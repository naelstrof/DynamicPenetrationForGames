/* Copyright 2024 Naelstrof & Raliv
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 * documentation files (the “Software”), to deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
 * Software.
 *
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
 * OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DPG {

[System.Serializable]
public struct GirthFrame {
    public static Matrix4x4 GetDefaultPenetratorPose() => Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(Vector3.up,-Vector3.forward), Vector3.one);

    public float maxGirthRadius;
    public float maxLocalLength;
    
    public GirthSliceCurve girthCurve;

    public GirthSlice SampleGirth(float alpha) {
        return girthCurve.Sample(alpha);
    }
    public GirthFrame(int sampleCount, float fullLength, float maxGirth) {
        maxGirthRadius = maxGirth;
        maxLocalLength = fullLength;
        girthCurve = new GirthSliceCurve(sampleCount);
    }

    public void DrawGizmos(Matrix4x4 transform, int samples) {
        Gizmos.matrix = transform*GetDefaultPenetratorPose();
        for (int i = 0; i < samples; i++) {
            float alpha = (float)i / (float)(samples - 1);
            GirthSlice slice = SampleGirth(alpha);
            DrawCircle(slice.position, Vector3.forward, slice.radius, 16);
        }
    }
    
    private void DrawCircle(Vector3 position, Vector3 normal, float radius, int segments = 16) {
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.001f) {
            tangent = Vector3.Cross(normal, Vector3.right);
        }
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent);
        bitangent.Normalize();

        Vector3 prevPoint = position + tangent * radius;
        for (int i = 1; i <= segments; i++) {
            float angle = (float)i / (float)segments * Mathf.PI * 2f;
            Vector3 nextPoint = position + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    public GirthFrame Clone() {
        return new GirthFrame {
            maxGirthRadius = maxGirthRadius,
            maxLocalLength = maxLocalLength,
            girthCurve = girthCurve.Clone(),
        };
    }

    public void SampleBlendshapes(SkinnedMeshRenderer renderer, IList<GirthFrame> blendshapes) {
        var samples = girthCurve.GetSampleCount();
        var blendshapeCount = blendshapes.Count;

        for (int i = 0; i < samples; i++) {
            var girthSlice = girthCurve.GetSample(i);
            for (int j = 0; j < blendshapeCount; j++) {
                float weight = renderer.GetBlendShapeWeight(j) * 0.01f;
                if (weight == 0f) {
                    continue;
                }
                girthSlice += blendshapes[j].girthCurve.GetSample(i) * weight;
            }
            girthCurve.SetSample(i, girthSlice);
        }

        for (int j = 0; j < blendshapeCount; j++) {
            float weight = renderer.GetBlendShapeWeight(j) * 0.01f;
            if (weight == 0f) {
                continue;
            }
            maxGirthRadius += blendshapes[j].maxGirthRadius * weight;
            maxLocalLength += blendshapes[j].maxLocalLength * weight;
        }
    }

    public void SampleTransforms(SkinnedMeshRenderer renderer, Matrix4x4 penetratorBindPose, Transform rootBone) {
        var samples = girthCurve.GetSampleCount();
        for (int i = 0; i < samples; i++) {
            var slice = girthCurve.GetSample(i);
            var meshPos = penetratorBindPose.MultiplyPoint3x4(slice.position);
            var bindposes = renderer.sharedMesh.bindposes;
            var bones = renderer.bones;
            var worldPos =
                bones[slice.boneIndex0].TransformPoint(bindposes[slice.boneIndex0].MultiplyPoint3x4(meshPos)) * slice.boneWeights.x +
                bones[slice.boneIndex1].TransformPoint(bindposes[slice.boneIndex1].MultiplyPoint3x4(meshPos)) * slice.boneWeights.y +
                bones[slice.boneIndex2].TransformPoint(bindposes[slice.boneIndex2].MultiplyPoint3x4(meshPos)) * slice.boneWeights.z +
                bones[slice.boneIndex3].TransformPoint(bindposes[slice.boneIndex3].MultiplyPoint3x4(meshPos)) * slice.boneWeights.w;
            
            slice.position = GetDefaultPenetratorPose().inverse.MultiplyPoint3x4(rootBone.InverseTransformPoint(worldPos)); 
            girthCurve.SetSample(i, slice);
        }
    }
    
    public void ConvertToBlendshape(GirthFrame baseShape) {
        var samples = girthCurve.GetSampleCount();
        var blendshapeGirthCurve = girthCurve.Clone();
        for (int i = 0; i < samples; i++) {
            var baseGirth = baseShape.girthCurve.GetSample(i);
            var girthDelta = girthCurve.GetSample(i) - baseGirth;
            blendshapeGirthCurve.SetSample(i, girthDelta);
        }
        girthCurve = blendshapeGirthCurve;
        maxLocalLength -= baseShape.maxLocalLength;
        maxGirthRadius -= baseShape.maxGirthRadius;
    }

    private static List<Vector3> staticVertices = new List<Vector3>();
    private static List<int> staticTriangles = new List<int>();
    private static Material staticMaterialA;
    private static Material staticMaterialB;
    private static Material subtractBlit;
    public static async Task<GirthFrame> GenerateGirthFrame(RendererSubMeshMask rendererMask, Transform penetratorRoot, Matrix4x4 penetratorBindPose, int blendshapeIndex, float maxGirth, Shader girthUnwrapShader) {
        GirthFrame girthFrame = new GirthFrame(2,1, maxGirth);
        var girthMap = new RenderTexture(64, 64, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear) {
            useMipMap = true,
            autoGenerateMips = false,
            wrapModeU = TextureWrapMode.Clamp,
            wrapModeV = TextureWrapMode.Repeat
        };
        var mesh = rendererMask.GetMesh();
        staticVertices.Clear();
        mesh.GetVertices(staticVertices);
        if (blendshapeIndex != -1) {
            Vector3[] blendDeltaVertices = new Vector3[staticVertices.Count];
            mesh.GetBlendShapeFrameVertices(blendshapeIndex, 0, blendDeltaVertices, null, null);
            for (int i = 0; i < staticVertices.Count; i++) {
                staticVertices[i] += blendDeltaVertices[i];
            }
        }

        Mesh blitMesh;
        bool freemesh = false;

        // If we're a skinned mesh renderer, we mask by bone weights.
        var bones = rendererMask.renderer.bones;
        freemesh = true;
        blitMesh = new Mesh();
        blitMesh.SetVertices(staticVertices);
        blitMesh.subMeshCount = mesh.subMeshCount;
        for (int i = 0; i < mesh.subMeshCount; i++) {
            if (rendererMask.ShouldDrawSubmesh(i)) {
                staticTriangles.Clear();
                mesh.GetTriangles(staticTriangles, i);
                blitMesh.SetTriangles(staticTriangles, i);
            }
        }

        HashSet<int> validBones = new HashSet<int>();
        for (int i = 0; i < bones.Length; i++) {
            if (bones[i].IsChildOf(penetratorRoot)) {
                validBones.Add(i);
            }
        }

        var weights = mesh.GetAllBoneWeights();
        var bonesPerVertex = mesh.GetBonesPerVertex();
        int vt = 0;
        int wt = 0;
        for (int o = 0; o < bonesPerVertex.Length; o++) {
            for (int p = 0; p < bonesPerVertex[o]; p++) {
                BoneWeight1 weight = weights[wt];
                if (validBones.Contains(weight.boneIndex) && weight.weight > 0.1f) {
                    Vector3 pos = staticVertices[vt];
                    float length = Vector3.Dot(penetratorBindPose.GetColumn(2), pos - (Vector3)penetratorBindPose.GetColumn(3));
                    girthFrame.maxLocalLength = Mathf.Max(length, girthFrame.maxLocalLength);
                }

                wt++;
            }

            vt++;
        }

        if (staticMaterialA == null) {
            staticMaterialA = new Material(girthUnwrapShader);
        }

        if (staticMaterialB == null) {
            staticMaterialB = new Material(girthUnwrapShader);
        }

        staticMaterialA.SetFloat("_AngleOffset", Mathf.PI / 2f);
        staticMaterialA.SetVector("_PenetratorOrigin", (Vector3)penetratorBindPose.GetColumn(3));
        staticMaterialA.SetVector("_PenetratorForward", penetratorBindPose.GetColumn(2).normalized);
        staticMaterialA.SetVector("_PenetratorUp", penetratorBindPose.GetColumn(1).normalized);
        staticMaterialA.SetVector("_PenetratorRight", penetratorBindPose.GetColumn(0).normalized);
        staticMaterialA.SetFloat("_MaxLength", girthFrame.maxLocalLength);
        staticMaterialA.SetFloat("_MaxGirth", girthFrame.maxGirthRadius);
        staticMaterialB.SetFloat("_AngleOffset", -Mathf.PI / 2f);
        staticMaterialB.SetVector("_PenetratorOrigin", (Vector3)penetratorBindPose.GetColumn(3));
        staticMaterialB.SetVector("_PenetratorForward", penetratorBindPose.GetColumn(2).normalized);
        staticMaterialB.SetVector("_PenetratorUp", penetratorBindPose.GetColumn(1).normalized);
        staticMaterialB.SetVector("_PenetratorRight", penetratorBindPose.GetColumn(0).normalized);
        staticMaterialB.SetFloat("_MaxLength", girthFrame.maxLocalLength);
        staticMaterialB.SetFloat("_MaxGirth", girthFrame.maxGirthRadius);

        // Then use the GPU to rasterize
        CommandBuffer buffer = new CommandBuffer();
        buffer.SetRenderTarget(girthMap);
        buffer.ClearRenderTarget(false, true, Color.clear);
        for (int j = 0; j < mesh.subMeshCount; j++) {
            if (!rendererMask.ShouldDrawSubmesh(j)) {
                continue;
            }

            buffer.DrawMesh(blitMesh, Matrix4x4.identity, staticMaterialA, j, 0);
            buffer.DrawMesh(blitMesh, Matrix4x4.identity, staticMaterialB, j, 0);
        }

        Graphics.ExecuteCommandBuffer(buffer);
        buffer.Dispose();

        girthMap.GenerateMips();

        girthFrame = await Readback(girthFrame, girthMap);
        
        girthFrame = BindToNearestVertices(girthFrame, penetratorBindPose, mesh);

        if (freemesh) {
            if (Application.isPlaying) {
                Object.Destroy(blitMesh);
            }
            else {
                Object.DestroyImmediate(blitMesh);
            }
        }

        return girthFrame;
    }

    private static GirthFrame DoSlowReadback(GirthFrame girthFrame, RenderTexture girthMap) {
        Texture2D cpuTex = new Texture2D(girthMap.width, girthMap.height, TextureFormat.R8, false, true);
        var lastActive = RenderTexture.active;
        RenderTexture.active = girthMap;
        cpuTex.ReadPixels(new Rect(0, 0, girthMap.width, girthMap.height), 0, 0);
        cpuTex.Apply();
        RenderTexture.active = lastActive;
        var bytes = cpuTex.GetRawTextureData<byte>();
        girthFrame = PopulateGirthSlices(girthFrame, bytes, girthMap.width, girthMap.height);
        if (Application.isPlaying) {
            Object.Destroy(cpuTex);
        }
        else {
            Object.DestroyImmediate(cpuTex);
        }
        return girthFrame;
    }

    public static async Task<GirthFrame> Readback(GirthFrame girthFrame, RenderTexture girthMap) {
        if (SystemInfo.supportsAsyncGPUReadback && Application.isPlaying) {
            var nativeArray = new NativeArray<byte>(girthMap.width * girthMap.height * sizeof(byte), Allocator.Persistent);
            var request = AsyncGPUReadback.RequestIntoNativeArray(ref nativeArray, girthMap, 0, TextureFormat.R8);
            await Task.Run(() => request.WaitForCompletion());
            if (request.hasError || !request.done) {
                nativeArray.Dispose();
                return DoSlowReadback(girthFrame, girthMap);
            }
            // Otherwise our data is good.
            girthFrame = PopulateGirthSlices(girthFrame, nativeArray, request.width, request.height);
            nativeArray.Dispose();
            return girthFrame;
        } else {
            return DoSlowReadback(girthFrame, girthMap);
        }
    }

    private static GirthFrame BindToNearestVertices(GirthFrame input, Matrix4x4 penetratorBindPose, Mesh mesh) {
        var vertices = mesh.vertices;
        var vertexCount = vertices.Length;
        var boneWeights = mesh.boneWeights;
        var samples = input.girthCurve.GetSampleCount();
        for (int i = 0; i < samples; i++) {
            var girthSlice = input.girthCurve.GetSample(i);
            var localPos = penetratorBindPose.MultiplyPoint(girthSlice.position);
            float nearestDistance = float.MaxValue;
            int nearestVertexIndex = -1;
            for (int v = 0; v < vertexCount; v++) {
                var vertex = vertices[v];
                var distance = Vector3.Distance(localPos, vertex);
                if (distance < nearestDistance) {
                    nearestDistance = distance;
                    nearestVertexIndex = v;
                }
            }
            if (nearestVertexIndex != -1) {
                var weight = boneWeights[nearestVertexIndex];
                girthSlice.boneWeights = new Vector4(weight.weight0, weight.weight1, weight.weight2, weight.weight3);
                girthSlice.boneIndex0 = weight.boneIndex0;
                girthSlice.boneIndex1 = weight.boneIndex1;
                girthSlice.boneIndex2 = weight.boneIndex2;
                girthSlice.boneIndex3 = weight.boneIndex3;
                input.girthCurve.SetSample(i, girthSlice);
            }
        }

        return input;
    }

    private static GirthFrame PopulateGirthSlices(GirthFrame input, NativeArray<byte> bytes, int width, int height) {
        input.girthCurve = new GirthSliceCurve(width);
        for (int x = 0; x < width; x++) {
            GirthSlice slice = new GirthSlice();
            Vector2 positionSum = Vector2.zero;
            for (int y = 0; y < height / 2; y++) {
                float color = (float)bytes[x + y * width] / 255f; //cpuTex.GetPixel(x,y).r;
                float rad = ((float)y / (float)height) * Mathf.PI * 2f;
                float distFromCore = color * input.maxGirthRadius;
                float xPosition = Mathf.Sin(rad - Mathf.PI / 2f) * distFromCore;
                float yPosition = Mathf.Cos(rad - Mathf.PI / 2f) * distFromCore;
                Vector2 position = new Vector2(xPosition, yPosition);

                int oppositeY = (y + height / 2) % height;
                float oppositeColor = ((float)bytes[x + oppositeY * width] / 255f); //cpuTex.GetPixel(x,oppositeY).r;
                float oppositeRad = ((float)oppositeY / (float)height) * Mathf.PI * 2f;
                float oppositeDistFromCore = oppositeColor * input.maxGirthRadius;
                float oppositeXPosition = Mathf.Sin(oppositeRad - Mathf.PI / 2f) * oppositeDistFromCore;
                float oppositeYPosition = Mathf.Cos(oppositeRad - Mathf.PI / 2f) * oppositeDistFromCore;
                Vector2 oppositePosition = new Vector2(oppositeXPosition, oppositeYPosition);
                positionSum += (position + oppositePosition) * 0.5f;
            }

            Vector2 positionAverage = positionSum / (float)(height / 2);
            positionAverage *= 2;

            slice.position = new Vector3(positionAverage.x, positionAverage.y, input.maxLocalLength*(float)x/(float)width);

            float averageRadius = 0f;
            for (int y = 0; y < height; y++) {
                float color = (float)bytes[x + y * width] / 255f; //cpuTex.GetPixel(x,y).r;
                float rad = ((float)y / (float)height) * Mathf.PI * 2f;
                float distFromCore = color * input.maxGirthRadius;
                float xPosition = Mathf.Sin(rad - Mathf.PI / 2f) * distFromCore;
                float yPosition = Mathf.Cos(rad - Mathf.PI / 2f) * distFromCore;
                Vector2 position = new Vector2(xPosition, yPosition);
                averageRadius += Vector2.Distance(position, new Vector2(slice.position.x, slice.position.y));
            }
            
            averageRadius /= height;
            // 10% of the dick is tapered at the end, to make it continuous
            int taperEndCount = Mathf.Max(width / 10, 3);
            if (x > width - taperEndCount) {
                float multiplier = (float)(width - x) / (taperEndCount - 1);
                slice.radius = averageRadius * multiplier * multiplier;
            } else {
                slice.radius = averageRadius;
            }
            input.girthCurve.SetSample(x, slice);
        }
        var copy = input.girthCurve.GetSample(width - 2);
        copy.radius = 0f;
        input.girthCurve.SetSample(width - 1, copy);
        return input;
    }

    private static float GetEasing(float x) {
        return x >= 1f ? 1f : 1f - Mathf.Pow(2, -10f * x);
    }

}

}