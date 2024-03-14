using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenetrationTech {
    [System.Serializable]
    public class GirthData {
        private static List<int> staticTriangles = new List<int>();
        private static List<Vector3> staticVertices = new List<Vector3>();
        private static Material staticMaterialA;
        private static Material staticMaterialB;
        [System.Serializable]
        public class GirthFrame {
            [SerializeField]
            public float maxLocalLength;
            [SerializeField]
            public float maxLocalGirthRadius;
            [SerializeField]
            public AnimationCurve localGirthRadiusCurve;
            [SerializeField]
            public AnimationCurve localXOffsetCurve;
            [SerializeField]
            public AnimationCurve localYOffsetCurve;
            [SerializeField]
            public RenderTexture girthMap;
            [SerializeField]
            public Texture2D detailMap;
            [HideInInspector]
            private Vector3 rendererLocalPenetratorForward;
            [HideInInspector]
            private Vector3 rendererLocalPenetratorRight;
            [HideInInspector]
            private Vector3 rendererLocalPenetratorUp;

            private NativeArray<byte> nativeArray;

            public GirthFrame() {
                localXOffsetCurve = new AnimationCurve {
                    postWrapMode = WrapMode.ClampForever,
                    preWrapMode = WrapMode.ClampForever
                };
                localYOffsetCurve = new AnimationCurve {
                    postWrapMode = WrapMode.ClampForever,
                    preWrapMode = WrapMode.ClampForever
                };
                localGirthRadiusCurve = new AnimationCurve {
                    postWrapMode = WrapMode.ClampForever,
                    preWrapMode = WrapMode.ClampForever
                };
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
                } else {
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

            private void PopulateDetailMap(NativeArray<byte> bytes, int width, int height) {
                NativeArray<byte> pixelData = new NativeArray<byte>(width * height, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        if (x == width - 1) {
                            pixelData[x + y * width] = byte.MaxValue/2;
                            continue;
                        }

                        float color = (float)bytes[x + y * width]/255f;;
                        float rad = ((float)y/(float)height)*Mathf.PI*2f;
                        float distFromCore = color*maxLocalGirthRadius;
                        float xPosition = Mathf.Sin(rad-Mathf.PI/2f)*distFromCore;
                        float yPosition = Mathf.Cos(rad-Mathf.PI/2f)*distFromCore;
                        Vector2 position = new Vector2(xPosition, yPosition);
                        float distFromRoot = ((float)x/(float)width)*maxLocalLength;
                        Vector2 offsetSample = new Vector2(localXOffsetCurve.Evaluate(distFromRoot), localYOffsetCurve.Evaluate(distFromRoot));
                        Vector2 newOffset = position-offsetSample;
                        float newRadius = (newOffset.magnitude - localGirthRadiusCurve.Evaluate(distFromRoot))/maxLocalGirthRadius;
                        pixelData[x+y*width] = (byte)Mathf.RoundToInt(Mathf.Clamp01(newRadius+0.5f)*255f);
                    }
                }
                detailMap.SetPixelData(pixelData,0);
                detailMap.Apply();
                pixelData.Dispose();
            }

            public void Readback(Vector3 rendererLocalPenetratorForward, Vector3 rendererLocalPenetratorRight, Vector3 rendererLocalPenetratorUp) {
                this.rendererLocalPenetratorForward = rendererLocalPenetratorForward;
                this.rendererLocalPenetratorRight = rendererLocalPenetratorRight;
                this.rendererLocalPenetratorUp = rendererLocalPenetratorUp;
                if (SystemInfo.supportsAsyncGPUReadback && Application.isPlaying) {
                    nativeArray = new NativeArray<byte>(girthMap.width * girthMap.height * sizeof(byte), Allocator.Persistent);
                    AsyncGPUReadback.RequestIntoNativeArray(ref nativeArray, girthMap, 0, TextureFormat.R8, OnCompleteReadBack);
                } else {
                    DoSlowReadback();
                }
            }

            private void PopulateOffsetCurves(NativeArray<byte> bytes, int width, int height) {
                for (int x = 0;x<width;x++) {
                    Vector2 positionSum = Vector2.zero;
                    for (int y = 0;y<height/2;y++) {
                        float color = (float)bytes[x + y * width]/255f;//cpuTex.GetPixel(x,y).r;
                        float rad = ((float)y/(float)height)*Mathf.PI*2f;
                        float distFromCore = color*maxLocalGirthRadius;
                        float xPosition = Mathf.Sin(rad-Mathf.PI/2f)*distFromCore;
                        float yPosition = Mathf.Cos(rad-Mathf.PI/2f)*distFromCore;
                        Vector2 position = new Vector2(xPosition, yPosition);

                        int oppositeY = (y+height/2)%height;
                        float oppositeColor = ((float)bytes[x + oppositeY * width]/255f);//cpuTex.GetPixel(x,oppositeY).r;
                        float oppositeRad = ((float)oppositeY/(float)height)*Mathf.PI*2f;
                        float oppositeDistFromCore = oppositeColor*maxLocalGirthRadius;
                        float oppositeXPosition = Mathf.Sin(oppositeRad-Mathf.PI/2f)*oppositeDistFromCore;
                        float oppositeYPosition = Mathf.Cos(oppositeRad-Mathf.PI/2f)*oppositeDistFromCore;
                        Vector2 oppositePosition = new Vector2(oppositeXPosition, oppositeYPosition);
                        positionSum += (position+oppositePosition)*0.5f;

                        //Vector3 point = rendererLocalDickForward * (((float)x/(float)width) * maxLocalLength);
                        //Vector3 otherPoint = point+rendererLocalDickRight*xPosition + rendererLocalDickUp*yPosition;
                        //Debug.DrawLine(objectToWorld.MultiplyPoint(point),objectToWorld.MultiplyPoint(otherPoint), Color.red, 10f);

                        //Vector3 oppositeOtherPoint = point+rendererLocalDickRight*oppositeXPosition + rendererLocalDickUp*oppositeYPosition;
                        //Debug.DrawLine(objectToWorld.MultiplyPoint(point),objectToWorld.MultiplyPoint(oppositeOtherPoint), Color.blue, 10f);
                    }
                    float distFromRoot = ((float)x/(float)width)*maxLocalLength;
                    Vector2 positionAverage = positionSum/(float)(height/2);
                    positionAverage *= 2;
                    localXOffsetCurve.AddKey(distFromRoot, positionAverage.x);
                    localYOffsetCurve.AddKey(distFromRoot, positionAverage.y);
                }
                localXOffsetCurve.AddKey(maxLocalLength, 0f);
                localYOffsetCurve.AddKey(maxLocalLength, 0f);
            }
            private void PopulateGirthCurve(NativeArray<byte> bytes, int width, int height) {
                for (int x=0;x<width;x++) {
                    float averageRadius = 0f;
                    Vector2 offset = new Vector2(localXOffsetCurve.Evaluate((float)x / (float)width * maxLocalLength), localYOffsetCurve.Evaluate((float)x / (float)width * maxLocalLength));
                    for (int y=0;y<height;y++) {
                        float color = (float)bytes[x + y * width] / 255f; //cpuTex.GetPixel(x,y).r;
                        float rad = ((float)y / (float)height) * Mathf.PI * 2f;
                        float distFromCore = color * maxLocalGirthRadius;
                        float xPosition = Mathf.Sin(rad - Mathf.PI / 2f) * distFromCore;
                        float yPosition = Mathf.Cos(rad - Mathf.PI / 2f) * distFromCore;
                        Vector2 position = new Vector2(xPosition, yPosition);
                        averageRadius += Vector2.Distance(position, offset);
                    }
                    averageRadius/=height;
                    localGirthRadiusCurve.AddKey((float)x/(float)width*maxLocalLength,averageRadius);
                }
                localGirthRadiusCurve.AddKey(maxLocalLength,0f);
            }
            public void Release() {
                if (girthMap != null) {
                    girthMap.Release();
                }
                if (detailMap != null) {
                    if (Application.isPlaying) {
                        Object.Destroy(detailMap);
                    } else {
                        Object.DestroyImmediate(detailMap);
                    }
                }
                detailMap = null;
                girthMap = null;
            }
        }

        [SerializeField]
        private GirthFrame baseGirthFrame;
        [SerializeField]
        private List<GirthFrame> girthDeltaFrames;
        
        private RendererSubMeshMask rendererMask;
        private Transform penetratorRoot;
        private Vector3 rendererLocalPenetratorForward;
        private Vector3 rendererLocalPenetratorUp;
        private Vector3 rendererLocalPenetratorRight;
        private Vector3 rendererLocalPenetratorRoot;
        private Vector3 rootLocalPenetratorForward;
        private Vector3 rootLocalPenetratorUp;
        private Vector3 rootLocalPenetratorRight;
        private Vector3 rootLocalPenetratorRoot;

        private static float GetPiecewiseDerivative(AnimationCurve curve, float t) {
            float epsilon = 0.00001f;
            float a = curve.Evaluate(t - epsilon);
            float b = curve.Evaluate(t + epsilon);
            return (b - a) / (epsilon * 2f);
        }

        private Matrix4x4 rendererToWorld => rendererMask.renderer.localToWorldMatrix;
        private Matrix4x4 worldToRenderer => rendererMask.renderer.worldToLocalMatrix;

        public static bool IsValid(GirthData data, Vector3 forward, Vector3 right, Vector3 up) {
            return data != null && data.rendererMask != null && data.girthDeltaFrames != null && data.girthDeltaFrames.Count != 0 && data.rootLocalPenetratorForward == forward && data.rootLocalPenetratorRight == right && data.rootLocalPenetratorUp == up;
        }
        
        private float localPenetratorRootForwardness => Vector3.Dot(rootLocalPenetratorRoot, rootLocalPenetratorForward);

        public float GetWorldLength() {
            Vector3 localPenetratorRootForward = penetratorRoot.TransformVector(rootLocalPenetratorForward * localPenetratorRootForwardness);
            Vector3 renderLength = GetLocalRenderLength() * rendererLocalPenetratorForward - worldToRenderer.MultiplyVector(localPenetratorRootForward);
            Vector3 length = renderLength.magnitude * rendererLocalPenetratorForward;
            return rendererToWorld.MultiplyVector(length).magnitude * GetPenetratorScaleFactor(rootLocalPenetratorForward);
        }
        
        private float GetWorldRenderLength() {
            // This handles skewed forwards, and even non-proportional scales of the dick (making it stubbier or longer)
            Vector3 length = GetLocalRenderLength() * rendererLocalPenetratorForward;
            return rendererToWorld.MultiplyVector(length).magnitude * GetPenetratorScaleFactor(rootLocalPenetratorForward);
        }

        private float GetLocalRenderLength() {
            float baseLength = baseGirthFrame.maxLocalLength;
            float length = baseLength;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    length += (girthDeltaFrames[i].maxLocalLength-baseLength) * (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }
            return length;
        }

        private float GetKnotForce(float worldDistanceAlongPenetrator) {
            var worldDistanceAlongPenetratorFromMinVertex = worldDistanceAlongPenetrator + localPenetratorRootForwardness;
            if (worldDistanceAlongPenetratorFromMinVertex < 0f || worldDistanceAlongPenetratorFromMinVertex > GetWorldRenderLength()) {
                return 0f;
            }

            float localDistanceAlongPenetratorFromMinVertex = worldToRenderer.MultiplyVector(worldDistanceAlongPenetratorFromMinVertex*rendererToWorld.MultiplyVector((rendererLocalPenetratorForward)).normalized).magnitude / GetPenetratorScaleFactor(rootLocalPenetratorForward);
            float baseKnotForce = GetPiecewiseDerivative(baseGirthFrame.localGirthRadiusCurve, localDistanceAlongPenetratorFromMinVertex*(baseGirthFrame.maxLocalLength / GetLocalRenderLength()));
            float knotForce = baseKnotForce;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    knotForce += (GetPiecewiseDerivative(girthDeltaFrames[i].localGirthRadiusCurve, localDistanceAlongPenetratorFromMinVertex*(girthDeltaFrames[i].maxLocalLength/GetLocalRenderLength()))-baseKnotForce) * (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }
            return knotForce;
        }

        private float GetPenetratorScaleFactor(Vector3 axis) {
            if (rendererMask.renderer is SkinnedMeshRenderer) {
                Matrix4x4 rootMatrix = Matrix4x4.TRS(Vector3.zero, penetratorRoot.localRotation, penetratorRoot.localScale);
                float scale = rootMatrix.MultiplyVector(axis).magnitude;
                return scale;
            }
            // TODO: Fails on non skinned renderers
            return 1f;
        }

        public float GetGirthScaleFactor() {
            float baseGirthRadius = baseGirthFrame.maxLocalGirthRadius;
            float girthRadius = baseGirthRadius;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    girthRadius += (girthDeltaFrames[i].maxLocalGirthRadius-baseGirthRadius) *
                             (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }
            Vector3 localGirth = rendererLocalPenetratorUp*girthRadius;
            float scaleFactor = rendererToWorld.MultiplyVector(localGirth).magnitude;
            return scaleFactor * GetPenetratorScaleFactor(rootLocalPenetratorUp);
        }
        
        // Dick space is arbitrary, "Spline space" refers to Z forward, Y Up, and X right space. 
        // This is to make it easier to place onto a spline.
        public Vector3 GetScaledSplineSpaceOffset(float worldDistanceAlongPenetrator) {
            float localDistanceAlongPenetrator = worldToRenderer.MultiplyVector(worldDistanceAlongPenetrator*rendererToWorld.MultiplyVector((rendererLocalPenetratorForward)).normalized).magnitude / GetPenetratorScaleFactor(rootLocalPenetratorForward);
            float lengthScaleFactor = baseGirthFrame.maxLocalLength / GetLocalRenderLength();
            float baseLocalXOffsetSample = baseGirthFrame.localXOffsetCurve.Evaluate(localDistanceAlongPenetrator*lengthScaleFactor);
            float baseLocalYOffsetSample = baseGirthFrame.localYOffsetCurve.Evaluate(localDistanceAlongPenetrator*lengthScaleFactor);
            float localXOffsetSample = baseLocalXOffsetSample;
            float localYOffsetSample = baseLocalYOffsetSample;
            
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    float scaleFactor = girthDeltaFrames[i].maxLocalLength / GetLocalRenderLength();
                    localXOffsetSample += (girthDeltaFrames[i].localXOffsetCurve.Evaluate(localDistanceAlongPenetrator*scaleFactor)-baseLocalXOffsetSample) *
                             (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                    localYOffsetSample += (girthDeltaFrames[i].localYOffsetCurve.Evaluate(localDistanceAlongPenetrator*scaleFactor)-baseLocalYOffsetSample) *
                             (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }

            Vector3 worldOffset = rendererToWorld.MultiplyVector(rendererLocalPenetratorRight*localXOffsetSample+rendererLocalPenetratorUp*localYOffsetSample)*GetPenetratorScaleFactor(rootLocalPenetratorRight);
            Matrix4x4 changeOfBasis = Matrix4x4.identity;
            changeOfBasis.SetRow(0,rendererToWorld.MultiplyVector(rendererLocalPenetratorRight).normalized);
            changeOfBasis.SetRow(1,rendererToWorld.MultiplyVector(rendererLocalPenetratorUp).normalized);
            changeOfBasis.SetRow(2,rendererToWorld.MultiplyVector(rendererLocalPenetratorForward).normalized);
            changeOfBasis[3,3] = 1f;
            return changeOfBasis.MultiplyVector(worldOffset);
        }
        
        public Texture2D GetDetailMap() {
            Texture2D bestMatch = baseGirthFrame.detailMap;
            float bestMatchAmount = 50f;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    float amount = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    if (amount > bestMatchAmount) {
                        bestMatch = girthDeltaFrames[i].detailMap;
                        bestMatchAmount = amount;
                    }
                }
            }
            return bestMatch;
        }

        public RenderTexture GetGirthMap() {
            RenderTexture bestMatch = baseGirthFrame.girthMap;
            float bestMatchAmount = 50f;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    float amount = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    if (amount > bestMatchAmount) {
                        bestMatch = girthDeltaFrames[i].girthMap;
                        bestMatchAmount = amount;
                    }
                }
            }

            return bestMatch;
        }

        public float GetWorldGirthRadius(float worldDistanceAlongPenetrator) {
            float localDistanceAlongPenetrator = worldToRenderer.MultiplyVector(worldDistanceAlongPenetrator*rendererToWorld.MultiplyVector((rendererLocalPenetratorForward)).normalized).magnitude / GetPenetratorScaleFactor(rootLocalPenetratorForward);
            // TODO: There's no real way to actually get the girth correctly, since we cannot interpret skewed scales. This is probably acceptable, though instead of just using localDickUp, maybe it should be a diagonal between up and right.
            // I currently just choose a single axis, though users shouldn't skew scale on the up/right axis anyway.
            float baseLocalGirthSample = baseGirthFrame.localGirthRadiusCurve.Evaluate(localDistanceAlongPenetrator*(baseGirthFrame.maxLocalLength / GetLocalRenderLength()));
            float localGirthSample = baseLocalGirthSample;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    localGirthSample += (girthDeltaFrames[i].localGirthRadiusCurve.Evaluate(localDistanceAlongPenetrator*(girthDeltaFrames[i].maxLocalLength / GetLocalRenderLength()))-baseLocalGirthSample) * (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }

            Vector3 localGirth = rendererLocalPenetratorUp*localGirthSample;
            return rendererToWorld.MultiplyVector(localGirth).magnitude * GetPenetratorScaleFactor(rootLocalPenetratorUp);
        }
        private static void GetBindPoseBoneLocalPositionRotation(Matrix4x4 boneMatrix, out Vector3 position, out Quaternion rotation) {
            // Get global matrix for bone
            Matrix4x4 bindMatrixGlobal = boneMatrix.inverse;
 
            // Get local X, Y, Z, and position of matrix
            Vector3 mX = new Vector3(bindMatrixGlobal.m00, bindMatrixGlobal.m10, bindMatrixGlobal.m20);
            Vector3 mY = new Vector3(bindMatrixGlobal.m01, bindMatrixGlobal.m11, bindMatrixGlobal.m21);
            Vector3 mZ = new Vector3(bindMatrixGlobal.m02, bindMatrixGlobal.m12, bindMatrixGlobal.m22);
            Vector3 mP = new Vector3(bindMatrixGlobal.m03, bindMatrixGlobal.m13, bindMatrixGlobal.m23);
            position = mP;
            
            // Set rotation
            // Check if scaling is negative and handle accordingly
            if (Vector3.Dot(Vector3.Cross(mX, mY), mZ) >= 0) {
                rotation = Quaternion.LookRotation(mZ, mY);
            } else {
                rotation = Quaternion.LookRotation(-mZ, -mY);
            }
        }
        public void Release() {
            if (baseGirthFrame != null) {
                baseGirthFrame.Release();
                baseGirthFrame = null;
            }

            if (girthDeltaFrames != null && girthDeltaFrames.Count > 0) {
                foreach (var girthFrame in girthDeltaFrames) {
                    girthFrame.Release();
                }

                girthDeltaFrames.Clear();
            }
        }
        private GirthFrame GenerateFrame(Mesh mesh, int blendshapeIndex, Shader girthUnwrapShader) {
            GirthFrame frame = new GirthFrame {
                girthMap = new RenderTexture(64, 64, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
                {
                    useMipMap = true,
                    autoGenerateMips = false,
                    wrapModeU = TextureWrapMode.Clamp,
                    wrapModeV = TextureWrapMode.Repeat
                },
                detailMap = new Texture2D(64, 64, TextureFormat.R8, Texture.GenerateAllMips, true) {
                    wrapModeU = TextureWrapMode.Clamp,
                    wrapModeV = TextureWrapMode.Repeat
                }
            };

            staticVertices.Clear();
            mesh.GetVertices(staticVertices);
            if (blendshapeIndex != -1) {
                Vector3[] blendDeltaVertices = new Vector3[staticVertices.Count];
                mesh.GetBlendShapeFrameVertices(blendshapeIndex,0, blendDeltaVertices, null, null);
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
                for(int i=0;i<bones.Length;i++) {
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
                            float girth = Vector3.Distance(pos,
                                (rendererLocalPenetratorRoot + rendererLocalPenetratorForward * length));
                            frame.maxLocalGirthRadius = Mathf.Max(girth, frame.maxLocalGirthRadius);
                            frame.maxLocalLength = Mathf.Max(length, frame.maxLocalLength);
                        }

                        wt++;
                    }

                    vt++;
                }
            } else {
                // Otherwise we can just use every vert.
                foreach (Vector3 vertexPosition in staticVertices) {
                    //Vector3 dickSpacePosition = changeOfBasis.MultiplyPoint(vertexPosition);
                    float length = Vector3.Dot(rendererLocalPenetratorForward, vertexPosition - rendererLocalPenetratorRoot);
                    float girth = Vector3.Distance(vertexPosition,
                        (rendererLocalPenetratorRoot + rendererLocalPenetratorForward * length));
                    frame.maxLocalGirthRadius = Mathf.Max(girth, frame.maxLocalGirthRadius);
                    frame.maxLocalLength = Mathf.Max(length, frame.maxLocalLength);
                }

                blitMesh = mesh;
            }

            if (staticMaterialA == null) {
                staticMaterialA = new Material(girthUnwrapShader);
                staticMaterialA.SetFloat("_AngleOffset", Mathf.PI / 2f);
            }

            if (staticMaterialB == null) {
                staticMaterialB = new Material(girthUnwrapShader);
                staticMaterialB.SetFloat("_AngleOffset", -Mathf.PI / 2f);
            }

            staticMaterialA.SetVector("_PenetratorOrigin", rendererLocalPenetratorRoot);
            staticMaterialA.SetVector("_PenetratorForward", rendererLocalPenetratorForward);
            staticMaterialA.SetVector("_PenetratorUp", rendererLocalPenetratorUp);
            staticMaterialA.SetVector("_PenetratorRight", rendererLocalPenetratorRight);
            staticMaterialA.SetFloat("_MaxLength", frame.maxLocalLength);
            staticMaterialA.SetFloat("_MaxGirth", frame.maxLocalGirthRadius);
            staticMaterialB.SetVector("_PenetratorOrigin", rendererLocalPenetratorRoot);
            staticMaterialB.SetVector("_PenetratorForward", rendererLocalPenetratorForward);
            staticMaterialB.SetVector("_PenetratorUp", rendererLocalPenetratorUp);
            staticMaterialB.SetVector("_PenetratorRight", rendererLocalPenetratorRight);
            staticMaterialB.SetFloat("_MaxLength", frame.maxLocalLength);
            staticMaterialB.SetFloat("_MaxGirth", frame.maxLocalGirthRadius);

            // Then use the GPU to rasterize
            CommandBuffer buffer = new CommandBuffer();
            buffer.SetRenderTarget(frame.girthMap);
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
            
            frame.girthMap.GenerateMips();

            frame.Readback(rendererLocalPenetratorForward, rendererLocalPenetratorRight, rendererLocalPenetratorUp);

            if (freemesh) {
                if (Application.isPlaying) {
                    Object.Destroy(blitMesh);
                } else {
                    Object.DestroyImmediate(blitMesh);
                }
            }
            return frame;
        }

        public GirthData(RendererSubMeshMask rendererWithMask, Shader girthUnwrapShader, Transform root, Vector3 rootLocalPenetratorRoot, Vector3 rootPenetratorForward, Vector3 rootPenetratorUp, Vector3 rootPenetratorRight) {
            rendererMask = rendererWithMask;
            penetratorRoot = root;
            this.rootLocalPenetratorUp = rootPenetratorUp;
            this.rootLocalPenetratorForward = rootPenetratorForward;
            this.rootLocalPenetratorRight = rootPenetratorRight;
            this.rootLocalPenetratorRoot = rootLocalPenetratorRoot;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                int rootBoneID = -1;
                for (int i = 0; i < skinnedMeshRenderer.bones.Length; i++) {
                    if (skinnedMeshRenderer.bones[i] == root) {
                        rootBoneID = i;
                    }
                }

                if (rootBoneID == -1) {
                    throw new UnityException("You must choose a bone on the armature...");
                }

                Mesh skinnedMesh = skinnedMeshRenderer.sharedMesh;
                GetBindPoseBoneLocalPositionRotation(skinnedMesh.bindposes[rootBoneID], out Vector3 posePosition, out Quaternion poseRotation);
                rendererLocalPenetratorForward = (poseRotation * rootPenetratorForward).normalized;
                rendererLocalPenetratorUp = (poseRotation * rootPenetratorUp).normalized;
                rendererLocalPenetratorRight = (poseRotation * rootPenetratorRight).normalized;
                rendererLocalPenetratorRoot = posePosition;
            } else {
                rendererLocalPenetratorForward = worldToRenderer.MultiplyVector(root.TransformDirection(rootPenetratorForward)).normalized;
                rendererLocalPenetratorUp = worldToRenderer.MultiplyVector(root.TransformDirection(rootPenetratorUp)).normalized;
                rendererLocalPenetratorRight = worldToRenderer.MultiplyVector(root.TransformDirection(rootPenetratorRight)).normalized;
                rendererLocalPenetratorRoot = worldToRenderer.MultiplyPoint(root.TransformPoint(rootLocalPenetratorRoot));
            }


            Mesh mesh;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer1) {
                mesh = skinnedMeshRenderer1.sharedMesh;
            } else if (rendererMask.renderer is MeshRenderer) {
                mesh = rendererMask.renderer.GetComponent<MeshFilter>().sharedMesh;
            } else {
                throw new UnityException("Girth data can only be generated on SkinnedMeshRenderers and MeshRenderers.");
            }

            baseGirthFrame = GenerateFrame(mesh, -1, girthUnwrapShader);
            // Do a quick pass to figure out how girthy and lengthy we are
            girthDeltaFrames = new List<GirthFrame>();
            for (int i = 0; i < mesh.blendShapeCount; i++) {
                girthDeltaFrames.Add(GenerateFrame(mesh, i, girthUnwrapShader));
            }
        }
    }
}