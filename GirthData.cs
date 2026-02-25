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

namespace DPG {

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class GirthData {
    private Shader additiveShader;
    private CommandBuffer renderGirthMapBuffer;
    private static List<Vector3> staticVertices = new List<Vector3>();
    private static Material additiveBlit;
    
    [SerializeField]
    private GirthFrame baseGirthFrame;
    [SerializeField]
    private List<GirthFrame> girthDeltaFrames;

    private RenderTexture girthMapCombined;
    
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
    private Matrix4x4 poseMatrix;
    private float maxGirth;

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
    
    public float GetWorldLength() {
        return LocalDickRootBoneToWorldLossy(rootLocalPenetratorForward*GetLocalRenderLength()).magnitude;
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

    public float GetKnotForce(float worldDistanceAlongPenetrator) {
        var worldDistanceAlongPenetratorFromMinVertex = worldDistanceAlongPenetrator;
        if (worldDistanceAlongPenetratorFromMinVertex < 0f || worldDistanceAlongPenetratorFromMinVertex > GetWorldLength()) {
            return 0f;
        }

        float localDistanceAlongPenetratorFromMinVertex = WorldToLocalDickRootBoneLossy(penetratorRoot.TransformDirection(rootLocalPenetratorForward) * worldDistanceAlongPenetrator).magnitude;
        float baseKnotForce = GetPiecewiseDerivative(baseGirthFrame.localGirthRadiusCurve, localDistanceAlongPenetratorFromMinVertex*(baseGirthFrame.maxLocalLength / GetLocalRenderLength()));
        float knotForce = baseKnotForce;
        if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                knotForce += (GetPiecewiseDerivative(girthDeltaFrames[i].localGirthRadiusCurve, localDistanceAlongPenetratorFromMinVertex*(girthDeltaFrames[i].maxLocalLength/GetLocalRenderLength()))-baseKnotForce) * (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
            }
        }

        float riseOverRunAdjustment = LocalDickRootBoneToWorldLossy(rootLocalPenetratorUp).magnitude / Mathf.Max(LocalDickRootBoneToWorldLossy(rootLocalPenetratorForward).magnitude,0.01f);
        return knotForce * riseOverRunAdjustment;
    }
    
    private Vector3 LocalDickRootBoneToWorldLossy(Vector3 vector) {
        Vector3 lossyScale = vector;
        
        lossyScale = poseMatrix.MultiplyVector(lossyScale);
        lossyScale = penetratorRoot.TransformVector(lossyScale);
        
        //return changeOfBasis.inverse * lossyScale;
        return lossyScale;
    }

    private Vector3 WorldToLocalDickRootBoneLossy(Vector3 vector) {
        Vector3 lossyScale = vector;
        
        lossyScale = penetratorRoot.InverseTransformVector(lossyScale);
        lossyScale = poseMatrix.inverse.MultiplyVector(lossyScale);
        
        return lossyScale;
    }

    public float GetGirthScaleFactor() {
        return LocalDickRootBoneToWorldLossy(rootLocalPenetratorUp * maxGirth).magnitude;
    }
    
    public Vector3 GetWorldOffset(float worldDistanceAlongPenetrator) {
        float localDistanceAlongPenetrator = WorldToLocalDickRootBoneLossy(penetratorRoot.TransformDirection(rootLocalPenetratorForward) * worldDistanceAlongPenetrator).magnitude;
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

        return LocalDickRootBoneToWorldLossy(rendererLocalPenetratorRight * localXOffsetSample + rendererLocalPenetratorUp * localYOffsetSample);
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
        if (additiveBlit == null) {
            additiveBlit = new Material(additiveShader);
        } else {
            additiveBlit.shader = additiveShader;
        }
        Graphics.Blit(baseGirthFrame.girthMap, girthMapCombined);
        
        if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                float amount = skinnedMeshRenderer.GetBlendShapeWeight(i);
                if (amount == 0) {
                    continue;
                }
                additiveBlit.SetFloat("_Amount", amount*0.01f);
                Graphics.Blit(girthDeltaFrames[i].girthMap, girthMapCombined, additiveBlit);
            }
        }
        girthMapCombined.GenerateMips();
        return girthMapCombined;
    }

    public float GetWorldGirthRadius(float worldDistanceAlongPenetrator) {
        float localDistanceAlongPenetrator = WorldToLocalDickRootBoneLossy(penetratorRoot.TransformDirection(rootLocalPenetratorForward) * worldDistanceAlongPenetrator).magnitude;
        // TODO: There's no real way to actually get the girth correctly, since we cannot interpret skewed scales. This is probably acceptable, though instead of just using localDickUp, maybe it should be a diagonal between up and right.
        // I currently just choose a single axis, though users shouldn't skew scale on the up/right axis anyway.
        float baseLocalGirthSample = baseGirthFrame.localGirthRadiusCurve.Evaluate(localDistanceAlongPenetrator*(baseGirthFrame.maxLocalLength / GetLocalRenderLength()));
        float localGirthSample = baseLocalGirthSample;
        if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                localGirthSample += (girthDeltaFrames[i].localGirthRadiusCurve.Evaluate(localDistanceAlongPenetrator*(girthDeltaFrames[i].maxLocalLength / GetLocalRenderLength()))-baseLocalGirthSample) * (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
            }
        }
        return LocalDickRootBoneToWorldLossy(rootLocalPenetratorUp * localGirthSample).magnitude;
    }
    private static void GetBindPoseBoneLocalPositionRotation(Matrix4x4 boneMatrix, out Vector3 position, out Quaternion rotation, out float scale) {
        // Get global matrix for bone
        Matrix4x4 bindMatrixGlobal = boneMatrix.inverse;

        // Get local X, Y, Z, and position of matrix
        Vector3 mX = new Vector3(bindMatrixGlobal.m00, bindMatrixGlobal.m10, bindMatrixGlobal.m20);
        Vector3 mY = new Vector3(bindMatrixGlobal.m01, bindMatrixGlobal.m11, bindMatrixGlobal.m21);
        Vector3 mZ = new Vector3(bindMatrixGlobal.m02, bindMatrixGlobal.m12, bindMatrixGlobal.m22);
        Vector3 mP = new Vector3(bindMatrixGlobal.m03, bindMatrixGlobal.m13, bindMatrixGlobal.m23);
        position = mP;

        scale = mX.magnitude;

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

    private static float GetMaxGirth(RendererSubMeshMask rendererMask, Transform penetratorRoot, Vector3 rendererLocalPenetratorForward, Vector3 rendererLocalPenetratorRoot) {
        float maxGirth = 0f;
        staticVertices.Clear();
        var mesh = rendererMask.GetMesh();
        mesh.GetVertices(staticVertices);
        Vector3[] blendDeltaVertices = new Vector3[staticVertices.Count];
        for (int blendshapeIndex = -1; blendshapeIndex < mesh.blendShapeCount; blendshapeIndex++) {
            if (blendshapeIndex != -1) {
                mesh.GetBlendShapeFrameVertices(blendshapeIndex, 0, blendDeltaVertices, null, null);
            }
            for (int i = 0; i < staticVertices.Count; i++) {
                blendDeltaVertices[i] += staticVertices[i];
            }

            // If we're a skinned mesh renderer, we mask by bone weights.
            if (rendererMask.renderer is SkinnedMeshRenderer meshRenderer) {
                var bones = meshRenderer.bones;
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
                            Vector3 pos = blendDeltaVertices[vt];
                            float length = Vector3.Dot(rendererLocalPenetratorForward, pos - rendererLocalPenetratorRoot);
                            float girth = Vector3.Distance(pos, (rendererLocalPenetratorRoot + rendererLocalPenetratorForward * length));
                            maxGirth = Mathf.Max(girth, maxGirth);
                        }
                        wt++;
                    }
                    vt++;
                }
            } else {
                // Otherwise we can just use every vert.
                foreach (Vector3 vertexPosition in blendDeltaVertices) {
                    float length = Vector3.Dot(rendererLocalPenetratorForward, vertexPosition - rendererLocalPenetratorRoot);
                    float girth = Vector3.Distance(vertexPosition, (rendererLocalPenetratorRoot + rendererLocalPenetratorForward * length));
                    maxGirth = Mathf.Max(girth, maxGirth);
                }
            }
        }
        return maxGirth;
    }

    public GirthData(RendererSubMeshMask rendererWithMask, Shader girthUnwrapShader, Shader subtractiveShader, Shader additiveShader, Transform root, Vector3 rootLocalPenetratorRoot, Vector3 rootPenetratorForward, Vector3 rootPenetratorUp, Vector3 rootPenetratorRight) {
        rendererMask = rendererWithMask;
        penetratorRoot = root;
        this.additiveShader = additiveShader;
        Transform t = penetratorRoot;
        this.rootLocalPenetratorUp = rootPenetratorUp;
        this.rootLocalPenetratorForward = rootPenetratorForward;
        this.rootLocalPenetratorRight = rootPenetratorRight;
        this.rootLocalPenetratorRoot = rootLocalPenetratorRoot;
        girthMapCombined = new RenderTexture(64, 64, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear) {
            useMipMap = true,
            autoGenerateMips = false,
            wrapModeU = TextureWrapMode.Clamp,
            wrapModeV = TextureWrapMode.Repeat
        };
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
            GetBindPoseBoneLocalPositionRotation(skinnedMesh.bindposes[rootBoneID], out Vector3 posePosition, out Quaternion poseRotation, out float scale);
            rendererLocalPenetratorForward = (poseRotation * rootPenetratorForward).normalized;
            rendererLocalPenetratorUp = (poseRotation * rootPenetratorUp).normalized;
            rendererLocalPenetratorRight = (poseRotation * rootPenetratorRight).normalized;
            rendererLocalPenetratorRoot = posePosition+(poseRotation*rootLocalPenetratorRoot*scale);
            poseMatrix = skinnedMeshRenderer.sharedMesh.bindposes[rootBoneID];
        } else {
            rendererLocalPenetratorForward = worldToRenderer.MultiplyVector(root.TransformDirection(rootPenetratorForward)).normalized;
            rendererLocalPenetratorUp = worldToRenderer.MultiplyVector(root.TransformDirection(rootPenetratorUp)).normalized;
            rendererLocalPenetratorRight = worldToRenderer.MultiplyVector(root.TransformDirection(rootPenetratorRight)).normalized;
            rendererLocalPenetratorRoot = worldToRenderer.MultiplyPoint(root.TransformPoint(rootLocalPenetratorRoot));
            poseMatrix = Matrix4x4.identity;
        }

        maxGirth = GetMaxGirth(rendererMask, penetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorRoot);
        baseGirthFrame = new GirthFrame(rendererMask, penetratorRoot, rendererLocalPenetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorUp, rendererLocalPenetratorRight,  -1, maxGirth, girthUnwrapShader);
        // Do a quick pass to figure out how girthy and lengthy we are
        girthDeltaFrames = new List<GirthFrame>();
        for (int i = 0; i < rendererMask.GetMesh().blendShapeCount; i++) {
            var girthDelta = new GirthFrame(rendererMask, penetratorRoot, rendererLocalPenetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorUp, rendererLocalPenetratorRight, i, maxGirth, girthUnwrapShader);
            girthDelta.ConvertToBlendshape(baseGirthFrame, subtractiveShader);
            girthDeltaFrames.Add(girthDelta);
        }
    }
}

}