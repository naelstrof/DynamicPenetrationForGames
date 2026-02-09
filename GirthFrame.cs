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
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace DPG {

[System.Serializable]
public class GirthFrame {
    private static List<Vector3> staticVertices = new List<Vector3>();
    private static List<int> staticTriangles = new List<int>();
    private static Material staticMaterialA;
    private static Material staticMaterialB;

    private static Material subtractBlit;

    [SerializeField] public float maxLocalLength;

    //[SerializeField] private float maxLocalGirthRadius;
    public float maxGirthRadius;

    [SerializeField] public AnimationCurve localGirthRadiusCurve = new() {
        postWrapMode = WrapMode.ClampForever,
        preWrapMode = WrapMode.ClampForever
    };

    [SerializeField] public AnimationCurve localXOffsetCurve = new() {
        postWrapMode = WrapMode.ClampForever,
        preWrapMode = WrapMode.ClampForever
    };

    [SerializeField] public AnimationCurve localYOffsetCurve = new() {
        postWrapMode = WrapMode.ClampForever,
        preWrapMode = WrapMode.ClampForever
    };

    [SerializeField] public RenderTexture girthMap;
    [SerializeField] public Texture2D detailMap;

    private NativeArray<byte> nativeArray;

    public GirthFrame(RendererSubMeshMask rendererMask, Transform penetratorRoot, Vector3 rendererLocalPenetratorRoot,
        Vector3 rendererLocalPenetratorForward,
        Vector3 rendererLocalPenetratorUp,
        Vector3 rendererLocalPenetratorRight,
        int blendshapeIndex, float maxGirth, Shader girthUnwrapShader) {

        var mesh = rendererMask.GetMesh();
        girthMap = new RenderTexture(64, 64, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear) {
            useMipMap = true,
            autoGenerateMips = false,
            wrapModeU = TextureWrapMode.Clamp,
            wrapModeV = TextureWrapMode.Repeat
        };
        detailMap = new Texture2D(64, 64, TextureFormat.R8, Texture.GenerateAllMips, true) {
            wrapModeU = TextureWrapMode.Clamp,
            wrapModeV = TextureWrapMode.Repeat
        };
        maxGirthRadius = maxGirth;
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
                        maxLocalLength = Mathf.Max(length, maxLocalLength);
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
                maxLocalLength = Mathf.Max(length, maxLocalLength);
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
        staticMaterialA.SetFloat("_MaxLength", maxLocalLength);
        staticMaterialA.SetFloat("_MaxGirth", maxGirth);
        staticMaterialB.SetFloat("_AngleOffset", -Mathf.PI / 2f);
        staticMaterialB.SetVector("_PenetratorOrigin", rendererLocalPenetratorRoot);
        staticMaterialB.SetVector("_PenetratorForward", rendererLocalPenetratorForward);
        staticMaterialB.SetVector("_PenetratorUp", rendererLocalPenetratorUp);
        staticMaterialB.SetVector("_PenetratorRight", rendererLocalPenetratorRight);
        staticMaterialB.SetFloat("_MaxLength", maxLocalLength);
        staticMaterialB.SetFloat("_MaxGirth", maxGirth);

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

        Readback();

        if (freemesh) {
            if (Application.isPlaying) {
                Object.Destroy(blitMesh);
            }
            else {
                Object.DestroyImmediate(blitMesh);
            }
        }
    }

    private void DoSlowReadback() {
        Texture2D cpuTex = new Texture2D(girthMap.width, girthMap.height, TextureFormat.R8, false, true);
        var lastActive = RenderTexture.active;
        RenderTexture.active = girthMap;
        cpuTex.ReadPixels(new Rect(0, 0, girthMap.width, girthMap.height), 0, 0);
        cpuTex.Apply();
        RenderTexture.active = lastActive;
        var bytes = cpuTex.GetRawTextureData<byte>();
        PopulateOffsetCurves(bytes, girthMap.width, girthMap.height);
        PopulateGirthCurve(bytes, girthMap.width, girthMap.height);
        PopulateDetailMap(bytes, girthMap.width, girthMap.height);
        if (Application.isPlaying) {
            Object.Destroy(cpuTex);
        }
        else {
            Object.DestroyImmediate(cpuTex);
        }
    }

    private void OnCompleteReadBack(AsyncGPUReadbackRequest request) {
        // if girthmap is null, that means we were released before the readback could complete.
        if (girthMap == null) {
            nativeArray.Dispose();
            return;
        }

        // Failed request, I don't know how this happens, but it's possible. Fallback on a synchronous readback.
        if (request.hasError || !request.done) {
            nativeArray.Dispose();
            DoSlowReadback();
            return;
        }

        // Otherwise our data is good.
        PopulateOffsetCurves(nativeArray, request.width, request.height);
        PopulateGirthCurve(nativeArray, request.width, request.height);
        PopulateDetailMap(nativeArray, request.width, request.height);
        nativeArray.Dispose();
    }

    public void ConvertToBlendshape(GirthFrame baseShape, Shader subtractShader) {
        if (subtractBlit == null) {
            subtractBlit = new Material(subtractShader);
        }
        else {
            subtractBlit.shader = subtractShader;
        }

        Graphics.Blit(baseShape.girthMap, girthMap, subtractBlit);
    }

    private void PopulateDetailMap(NativeArray<byte> bytes, int width, int height) {
        NativeArray<byte> pixelData =
            new NativeArray<byte>(width * height, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (x == width - 1) {
                    pixelData[x + y * width] = byte.MaxValue / 2;
                    continue;
                }

                float color = (float)bytes[x + y * width] / 255f;
                ;
                float rad = ((float)y / (float)height) * Mathf.PI * 2f;
                float distFromCore = color * maxGirthRadius;
                float xPosition = Mathf.Sin(rad - Mathf.PI / 2f) * distFromCore;
                float yPosition = Mathf.Cos(rad - Mathf.PI / 2f) * distFromCore;
                Vector2 position = new Vector2(xPosition, yPosition);
                float distFromRoot = ((float)x / (float)width) * maxLocalLength;
                Vector2 offsetSample = new Vector2(localXOffsetCurve.Evaluate(distFromRoot),
                    localYOffsetCurve.Evaluate(distFromRoot));
                Vector2 newOffset = position - offsetSample;
                float newRadius = (newOffset.magnitude - localGirthRadiusCurve.Evaluate(distFromRoot)) / maxGirthRadius;
                pixelData[x + y * width] = (byte)Mathf.RoundToInt(Mathf.Clamp01(newRadius + 0.5f) * 255f);
            }
        }

        detailMap.SetPixelData(pixelData, 0);
        detailMap.Apply();
        pixelData.Dispose();
    }

    public void Readback() {
        if (SystemInfo.supportsAsyncGPUReadback && Application.isPlaying) {
            nativeArray = new NativeArray<byte>(girthMap.width * girthMap.height * sizeof(byte), Allocator.Persistent);
            AsyncGPUReadback.RequestIntoNativeArray(ref nativeArray, girthMap, 0, TextureFormat.R8, OnCompleteReadBack);
        }
        else {
            DoSlowReadback();
        }
    }

    private void PopulateOffsetCurves(NativeArray<byte> bytes, int width, int height) {
        for (int x = 0; x < width; x++) {
            Vector2 positionSum = Vector2.zero;
            for (int y = 0; y < height / 2; y++) {
                float color = (float)bytes[x + y * width] / 255f; //cpuTex.GetPixel(x,y).r;
                float rad = ((float)y / (float)height) * Mathf.PI * 2f;
                float distFromCore = color * maxGirthRadius;
                float xPosition = Mathf.Sin(rad - Mathf.PI / 2f) * distFromCore;
                float yPosition = Mathf.Cos(rad - Mathf.PI / 2f) * distFromCore;
                Vector2 position = new Vector2(xPosition, yPosition);

                int oppositeY = (y + height / 2) % height;
                float oppositeColor = ((float)bytes[x + oppositeY * width] / 255f); //cpuTex.GetPixel(x,oppositeY).r;
                float oppositeRad = ((float)oppositeY / (float)height) * Mathf.PI * 2f;
                float oppositeDistFromCore = oppositeColor * maxGirthRadius;
                float oppositeXPosition = Mathf.Sin(oppositeRad - Mathf.PI / 2f) * oppositeDistFromCore;
                float oppositeYPosition = Mathf.Cos(oppositeRad - Mathf.PI / 2f) * oppositeDistFromCore;
                Vector2 oppositePosition = new Vector2(oppositeXPosition, oppositeYPosition);
                positionSum += (position + oppositePosition) * 0.5f;

                //Vector3 point = rendererLocalDickForward * (((float)x/(float)width) * maxLocalLength);
                //Vector3 otherPoint = point+rendererLocalDickRight*xPosition + rendererLocalDickUp*yPosition;
                //Debug.DrawLine(objectToWorld.MultiplyPoint(point),objectToWorld.MultiplyPoint(otherPoint), Color.red, 10f);

                //Vector3 oppositeOtherPoint = point+rendererLocalDickRight*oppositeXPosition + rendererLocalDickUp*oppositeYPosition;
                //Debug.DrawLine(objectToWorld.MultiplyPoint(point),objectToWorld.MultiplyPoint(oppositeOtherPoint), Color.blue, 10f);
            }

            float distFromRoot = ((float)x / (float)width) * maxLocalLength;
            Vector2 positionAverage = positionSum / (float)(height / 2);
            positionAverage *= 2;

            localXOffsetCurve.AddKey(distFromRoot, positionAverage.x);
            localYOffsetCurve.AddKey(distFromRoot, positionAverage.y);
        }

        localXOffsetCurve.AddKey(maxLocalLength, 0f);
        localYOffsetCurve.AddKey(maxLocalLength, 0f);
    }

    private float GetEasing(float x) {
        return x >= 1f ? 1f : 1f - Mathf.Pow(2, -10f * x);
    }

    private void PopulateGirthCurve(NativeArray<byte> bytes, int width, int height) {
        for (int x = 0; x < width; x++) {
            float averageRadius = 0f;
            Vector2 offset = new Vector2(localXOffsetCurve.Evaluate((float)x / (float)width * maxLocalLength),
                localYOffsetCurve.Evaluate((float)x / (float)width * maxLocalLength));
            for (int y = 0; y < height; y++) {
                float color = (float)bytes[x + y * width] / 255f; //cpuTex.GetPixel(x,y).r;
                float rad = ((float)y / (float)height) * Mathf.PI * 2f;
                float distFromCore = color * maxGirthRadius;
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
                localGirthRadiusCurve.AddKey((float)x / (float)width * maxLocalLength,
                    averageRadius * multiplier * multiplier);
            }
            else {
                localGirthRadiusCurve.AddKey((float)x / (float)width * maxLocalLength, averageRadius);
            }
        }

        localGirthRadiusCurve.AddKey(maxLocalLength, 0f);
    }

    public void Release() {
        if (girthMap != null) {
            girthMap.Release();
        }

        if (detailMap != null) {
            if (Application.isPlaying) {
                Object.Destroy(detailMap);
            }
            else {
                Object.DestroyImmediate(detailMap);
            }
        }

        detailMap = null;
        girthMap = null;
    }
}

}