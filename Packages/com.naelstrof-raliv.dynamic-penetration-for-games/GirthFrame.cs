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
    public float maxGirthRadius;
    public float maxLocalLength;
    public AnimationCurveFast localGirthRadiusCurve;
    public AnimationCurveFast localXOffsetCurve;
    public AnimationCurveFast localYOffsetCurve;

    public float SampleGirth(float dist) {
        return localGirthRadiusCurve.Sample(dist);
    }
    public Vector2 SampleOffset(float dist) {
        return new Vector2(localXOffsetCurve.Sample(dist), localYOffsetCurve.Sample(dist));
    }
    
    public GirthFrame(int sampleCount, float fullLength, float maxGirth) {
        maxGirthRadius = maxGirth;
        maxLocalLength = fullLength;
        localGirthRadiusCurve = new AnimationCurveFast(sampleCount);
        localXOffsetCurve = new AnimationCurveFast(sampleCount);
        localYOffsetCurve = new AnimationCurveFast(sampleCount);
    }
    
    public void ConvertToBlendshape(GirthFrame baseShape) {
        var samples = localGirthRadiusCurve.GetSampleCount();
        var blendshapeGirthRadius = new AnimationCurveFast(samples);
        var blendshapeXOffset = new AnimationCurveFast(samples);
        var blendshapeYOffset = new AnimationCurveFast(samples);
        for (int i = 0; i < samples-1; i++) {
            var sampleDistance = (float)i / (float)samples;
            float baseGirth = baseShape.localGirthRadiusCurve.Sample(sampleDistance);
            float baseXOffset = baseShape.localXOffsetCurve.Sample(sampleDistance);
            float baseYOffset = baseShape.localYOffsetCurve.Sample(sampleDistance);
            float girthDelta = localGirthRadiusCurve.Sample(sampleDistance) - baseGirth;
            float xOffsetDelta = localXOffsetCurve.Sample(sampleDistance) - baseXOffset;
            float yOffsetDelta = localYOffsetCurve.Sample(sampleDistance) - baseYOffset;
            blendshapeGirthRadius.SetSample(i, girthDelta);
            blendshapeXOffset.SetSample(i, xOffsetDelta);
            blendshapeYOffset.SetSample(i, yOffsetDelta);
        }
        blendshapeGirthRadius.SetSample(samples-1, 0f);
        blendshapeXOffset.SetSample(samples-1, 0f);
        blendshapeYOffset.SetSample(samples-1, 0f);
        localGirthRadiusCurve = blendshapeGirthRadius;
        localXOffsetCurve = blendshapeXOffset;
        localYOffsetCurve = blendshapeYOffset;
        maxLocalLength -= baseShape.maxLocalLength;
        maxGirthRadius -= baseShape.maxGirthRadius;
    }

    private static List<Vector3> staticVertices = new List<Vector3>();
    private static List<int> staticTriangles = new List<int>();
    private static Material staticMaterialA;
    private static Material staticMaterialB;
    private static Material subtractBlit;
    public static async Task<GirthFrame> GenerateGirthFrame(RendererSubMeshMask rendererMask, Transform penetratorRoot, Vector3 rendererLocalPenetratorRoot,
        Vector3 rendererLocalPenetratorForward,
        Vector3 rendererLocalPenetratorUp,
        Vector3 rendererLocalPenetratorRight,
        int blendshapeIndex, float maxGirth, Shader girthUnwrapShader) {
        
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
        if (rendererMask.renderer is SkinnedMeshRenderer meshRenderer) {
            var bones = meshRenderer.bones;
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
                        float length = Vector3.Dot(rendererLocalPenetratorForward, pos - rendererLocalPenetratorRoot);
                        girthFrame.maxLocalLength = Mathf.Max(length, girthFrame.maxLocalLength);
                    }

                    wt++;
                }

                vt++;
            }
        }
        else {
            // Otherwise we can just use every vert.
            foreach (Vector3 vertexPosition in staticVertices) {
                //Vector3 dickSpacePosition = changeOfBasis.MultiplyPoint(vertexPosition);
                float length = Vector3.Dot(rendererLocalPenetratorForward,
                    vertexPosition - rendererLocalPenetratorRoot);
                girthFrame.maxLocalLength = Mathf.Max(length, girthFrame.maxLocalLength);
            }

            blitMesh = mesh;
            freemesh = false;
        }

        if (staticMaterialA == null) {
            staticMaterialA = new Material(girthUnwrapShader);
        }

        if (staticMaterialB == null) {
            staticMaterialB = new Material(girthUnwrapShader);
        }

        staticMaterialA.SetFloat("_AngleOffset", Mathf.PI / 2f);
        staticMaterialA.SetVector("_PenetratorOrigin", rendererLocalPenetratorRoot);
        staticMaterialA.SetVector("_PenetratorForward", rendererLocalPenetratorForward);
        staticMaterialA.SetVector("_PenetratorUp", rendererLocalPenetratorUp);
        staticMaterialA.SetVector("_PenetratorRight", rendererLocalPenetratorRight);
        staticMaterialA.SetFloat("_MaxLength", girthFrame.maxLocalLength);
        staticMaterialA.SetFloat("_MaxGirth", girthFrame.maxGirthRadius);
        staticMaterialB.SetFloat("_AngleOffset", -Mathf.PI / 2f);
        staticMaterialB.SetVector("_PenetratorOrigin", rendererLocalPenetratorRoot);
        staticMaterialB.SetVector("_PenetratorForward", rendererLocalPenetratorForward);
        staticMaterialB.SetVector("_PenetratorUp", rendererLocalPenetratorUp);
        staticMaterialB.SetVector("_PenetratorRight", rendererLocalPenetratorRight);
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
        girthFrame = PopulateOffsetCurves(girthFrame, bytes, girthMap.width, girthMap.height);
        girthFrame = PopulateGirthCurve(girthFrame, bytes, girthMap.width, girthMap.height);
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
            girthFrame = PopulateOffsetCurves(girthFrame, nativeArray, request.width, request.height);
            girthFrame = PopulateGirthCurve(girthFrame, nativeArray, request.width, request.height);
            nativeArray.Dispose();
            return girthFrame;
        } else {
            return DoSlowReadback(girthFrame, girthMap);
        }
    }

    private static GirthFrame PopulateOffsetCurves(GirthFrame input, NativeArray<byte> bytes, int width, int height) {
        input.localXOffsetCurve = new AnimationCurveFast(width);
        input.localYOffsetCurve = new AnimationCurveFast(width);
        for (int x = 0; x < width; x++) {
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

            input.localXOffsetCurve.SetSample(x, positionAverage.x);
            input.localYOffsetCurve.SetSample(x, positionAverage.y);
        }

        input.localXOffsetCurve.SetSample(width-1, 0f);
        input.localYOffsetCurve.SetSample(width-1, 0f);
        return input;
    }

    private static float GetEasing(float x) {
        return x >= 1f ? 1f : 1f - Mathf.Pow(2, -10f * x);
    }

    private static GirthFrame PopulateGirthCurve(GirthFrame input, NativeArray<byte> bytes, int width, int height) {
        input.localGirthRadiusCurve = new AnimationCurveFast(width);
        for (int x = 0; x < width; x++) {
            float averageRadius = 0f;
            Vector2 offset = new Vector2(input.localXOffsetCurve.Sample((float)x / (float)width * input.maxLocalLength),
                input.localYOffsetCurve.Sample((float)x / (float)width * input.maxLocalLength));
            for (int y = 0; y < height; y++) {
                float color = (float)bytes[x + y * width] / 255f; //cpuTex.GetPixel(x,y).r;
                float rad = ((float)y / (float)height) * Mathf.PI * 2f;
                float distFromCore = color * input.maxGirthRadius;
                float xPosition = Mathf.Sin(rad - Mathf.PI / 2f) * distFromCore;
                float yPosition = Mathf.Cos(rad - Mathf.PI / 2f) * distFromCore;
                Vector2 position = new Vector2(xPosition, yPosition);
                averageRadius += Vector2.Distance(position, offset);
            }

            averageRadius /= height;
            // 10% of the dick is tapered at the end, to make it continuous
            int taperEndCount = Mathf.Max(width / 10, 3);
            if (x > width - taperEndCount) {
                float multiplier = (float)(width - x) / (taperEndCount - 1);
                input.localGirthRadiusCurve.SetSample(x, averageRadius * multiplier * multiplier);
            }
            else {
                input.localGirthRadiusCurve.SetSample(x, averageRadius);
            }
        }

        input.localGirthRadiusCurve.SetSample(width-1, 0f);
        return input;
    }
    
}

}