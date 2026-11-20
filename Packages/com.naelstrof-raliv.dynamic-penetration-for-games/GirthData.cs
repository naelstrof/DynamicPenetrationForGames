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

using System;
using System.Threading.Tasks;
using GatorDragonGames;
using UnityEngine;

namespace DPG {

using System.Collections.Generic;

[Serializable]
public struct GirthLookupHandle {
    [SerializeField] private float[] boneWorldDistances;
    [SerializeField] private float maxLength;
    [SerializeField] private GirthFrame girthFrame;
    [SerializeField] private float[] boneTimes;
    [SerializeField] private Vector3 poseToWorldScale;
    
    private Spline poseSpaceTimeSpline;
    private Spline worldDistanceSpline;
    private Spline3D targetSpline;

    public GirthLookupHandle(GirthFrame girthFrame, IList<float> worldBoneDistancesIncludingTip, IList<float> boneTimes, Vector3 poseToWorldScale, Spline3D onSpline) {
        this.boneTimes = new float[boneTimes.Count];
        boneTimes.CopyTo(this.boneTimes, 0);
        targetSpline = onSpline;
        boneWorldDistances = new float[worldBoneDistancesIncludingTip.Count];
        worldBoneDistancesIncludingTip.CopyTo(boneWorldDistances, 0);
        maxLength = boneWorldDistances[^1];
        for (int i = 0; i < boneWorldDistances.Length; i++) {
            boneWorldDistances[i] = Mathf.Clamp01(boneWorldDistances[i]/maxLength);
        }
        worldDistanceSpline = new Spline().SetFromCardinalPositions(boneWorldDistances, 1f);
        poseSpaceTimeSpline = new Spline().SetFromCardinalPositions(boneTimes, 1f);
        this.poseToWorldScale = poseToWorldScale;
        this.girthFrame = girthFrame;
    }
    private float GetPoseSpaceTimeFromWorldTime(float alpha) {
        float sampleDistanceWorld = Mathf.Clamp01(targetSpline.GetDistanceFromTime(alpha)/maxLength/poseToWorldScale.z);
        float poseSampleTime = Mathf.Clamp01(poseSpaceTimeSpline.GetTimeFromDistance(sampleDistanceWorld));
        float sampleDistance = Mathf.Clamp01(worldDistanceSpline.GetTimeFromDistance(poseSampleTime));
        return Mathf.Clamp01(sampleDistance);
    }
    
    public float GetGirth(float alpha) {
        return girthFrame.SampleGirth(GetPoseSpaceTimeFromWorldTime(alpha))*poseToWorldScale.x;
    }
    
    public Vector2 GetOffset(float alpha) {
        return girthFrame.SampleOffset(GetPoseSpaceTimeFromWorldTime(alpha))*poseToWorldScale;
    }
    
    public void DrawGizmos(int sampleCount, Vector3 dickWorldUp, Matrix4x4 dickBasis) {
        for (int i = 0; i < sampleCount; i++) {
            float samplePreAlpha = ((float)i / (float)sampleCount);
            float sampleDistance = samplePreAlpha * targetSpline.arcLength;
            float sampleAlpha = targetSpline.GetTimeFromDistance(sampleDistance);
            
            if (sampleAlpha > 1f) {
                break;
            }
            Gizmos.color = Color.Lerp(Color.green, Color.red, sampleAlpha);
            float girthRadius = GetGirth(sampleAlpha);
            Vector3 offset = GetOffset(sampleAlpha);
            var refFrame = targetSpline.GetReferenceFrameFromT(sampleAlpha, dickWorldUp);
            var position = refFrame.MultiplyPoint(dickBasis.MultiplyVector(offset));
            DrawCircle(position, refFrame.MultiplyVector(Vector3.forward), girthRadius);
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
}

[Serializable]
public struct SkinnedMeshGirth {
    [SerializeField] public float maxGirth;
    [SerializeField] public GirthFrame baseGirth;
    [SerializeField] public GirthFrame[] girthDeltas;
    [SerializeField] public float lengthPastTip;
    [SerializeField] public float[] boneTimes;

    public void DrawGizmos(Transform[] bones, SkinnedMeshRenderer renderer, Matrix4x4 dickBasis, int sampleCount = 128) {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.green;
        
        List<Vector3> staticVertices = new List<Vector3>(Array.ConvertAll(bones, t => t.position)) { bones[^1].position + (bones[^1].position-bones[^2].position).normalized*1f };
        Spline3D pretendSpline = new Spline3D().SetFromBSplinePositions(staticVertices);
        var lookupHandle = GetGirthLookupHandle(renderer, bones, GetWorldBoneTimes(bones), pretendSpline);
        lookupHandle.DrawGizmos(sampleCount, Vector3.up, dickBasis);
    }
    
    private float[] GetWorldBoneTimes(Transform[] bones) {
        float[] worldBoneTimes = new float[bones.Length+1];
        var sum = 0f;
        var prevPosition = bones[0].position;
        for(int i=0; i<bones.Length; i++) {
            sum += Vector3.Distance(prevPosition, bones[i].position);
            worldBoneTimes[i] = sum;
            prevPosition = bones[i].position;
        }

        worldBoneTimes[^1] = sum + lengthPastTip;
        return worldBoneTimes;
    }
    
    public GirthLookupHandle GetGirthLookupHandle(SkinnedMeshRenderer renderer, Transform[] bones, float[] worldBoneTimesIncludingTip, Spline3D onSpline) {
        var girthFrame = GetGirthFrame(renderer);
        var scale = bones[0].lossyScale;
        scale = new Vector3(scale.x, scale.z, scale.y*(girthFrame.maxLocalLength/baseGirth.maxLocalLength));
        return new GirthLookupHandle(girthFrame, worldBoneTimesIncludingTip, boneTimes, scale, onSpline);
    }

    public GirthFrame GetGirthFrame(SkinnedMeshRenderer renderer) {
        var b = baseGirth.Clone();
        b.SampleSkinnedMeshRenderer(renderer, girthDeltas);
        return b;
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

    private static float GetMaxGirth(RendererSubMeshMask rendererMask, Transform penetratorRoot, Vector3 rendererLocalPenetratorForward, Vector3 rendererLocalPenetratorRoot, List<Vector3> staticVertices) {
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
    private static bool TryGetBoneIndex(Transform[] bones, Transform bone, out int index) {
        for (int i = 0; i < bones.Length; i++) {
            if (bones[i] == bone) {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }
    
    public static async Task<SkinnedMeshGirth> GenerateGirthLookup(RendererSubMeshMask rendererWithMask, Shader girthUnwrapShader, Transform[] bones, Vector3 rootLocalPenetratorRoot, Vector3 rootLocalPenetratorForward, Vector3 rootLocalPenetratorUp, Vector3 rootLocalPenetratorRight) {
        Vector3 rendererLocalPenetratorForward = Vector3.zero;
        Vector3 rendererLocalPenetratorUp = Vector3.zero;
        Vector3 rendererLocalPenetratorRight = Vector3.zero;
        Vector3 rendererLocalPenetratorRoot = Vector3.zero;
        Matrix4x4 poseMatrix = Matrix4x4.identity;
        var penetratorRoot = bones[0];
        var skinnedMeshRenderer = rendererWithMask.renderer;
        if (!TryGetBoneIndex(skinnedMeshRenderer.bones, penetratorRoot, out int rootBoneID)) {
            throw new UnityException("You must choose a bone on the armature...");
        }

        Mesh skinnedMesh = skinnedMeshRenderer.sharedMesh;
        GetBindPoseBoneLocalPositionRotation(skinnedMesh.bindposes[rootBoneID], out Vector3 posePosition, out Quaternion poseRotation, out float scale);
        rendererLocalPenetratorForward = (poseRotation * rootLocalPenetratorForward).normalized;
        rendererLocalPenetratorUp = (poseRotation * rootLocalPenetratorUp).normalized;
        rendererLocalPenetratorRight = (poseRotation * rootLocalPenetratorRight).normalized;
        rendererLocalPenetratorRoot = posePosition+(poseRotation*rootLocalPenetratorRoot*scale);
        poseMatrix = skinnedMeshRenderer.sharedMesh.bindposes[rootBoneID];

        var staticVertices = new List<Vector3>();
        var maxGirth = GetMaxGirth(rendererWithMask, penetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorRoot, staticVertices);
        
        var baseGirth = await GirthFrame.GenerateGirthFrame(rendererWithMask, penetratorRoot, rendererLocalPenetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorUp, rendererLocalPenetratorRight, -1, maxGirth, girthUnwrapShader);
        var blendshapeCount = rendererWithMask.GetMesh().blendShapeCount;
        var blendshapeGirths = new GirthFrame[blendshapeCount];
        for (int i = 0; i < blendshapeCount; i++) {
            var blendshapeGirth = await GirthFrame.GenerateGirthFrame(rendererWithMask, penetratorRoot, rendererLocalPenetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorUp, rendererLocalPenetratorRight, i, maxGirth, girthUnwrapShader);
            blendshapeGirth.ConvertToBlendshape(baseGirth);
            blendshapeGirths[i] = blendshapeGirth;
        }
        var girthLookup = new SkinnedMeshGirth() {
            baseGirth = baseGirth,
            girthDeltas = blendshapeGirths,
            maxGirth = maxGirth,
        };
        var fullLength = baseGirth.maxLocalLength;
        var boneCount = bones.Length;
        var boneTimes = new float[boneCount+1];
        var distance = 0f;
        if (!TryGetBoneIndex(skinnedMeshRenderer.bones, bones[0], out int firstBone)) {
            throw new UnityException("You must choose a bones on the armature...");
        }
        GetBindPoseBoneLocalPositionRotation(skinnedMeshRenderer.sharedMesh.bindposes[firstBone], out Vector3 lastBonePosition, out Quaternion _, out float _);
        for (int i = 0; i < boneCount; i++) {
            var bone = bones[i];
            if (!TryGetBoneIndex(skinnedMeshRenderer.bones, bone, out int boneID)) {
                throw new UnityException("You must choose a bones on the armature...");
            }
            GetBindPoseBoneLocalPositionRotation(skinnedMeshRenderer.sharedMesh.bindposes[boneID], out Vector3 bonePosition, out Quaternion _, out float _);
            distance += Vector3.Distance(lastBonePosition, bonePosition);
            lastBonePosition = bonePosition;
            boneTimes[i] = distance/fullLength;
        }
        boneTimes[boneCount] = 1f;
        girthLookup.boneTimes = boneTimes;
        girthLookup.lengthPastTip = fullLength - (boneTimes[^2]*fullLength);
        return girthLookup;
    }
}

}