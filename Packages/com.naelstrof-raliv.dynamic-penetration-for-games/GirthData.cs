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
using UnityEngine;

namespace DPG {

using System.Collections.Generic;

[Serializable]
public struct SkinnedMeshGirth {
    [SerializeField] public float maxGirth;
    [SerializeField] public GirthFrame baseGirth;
    [SerializeField] public GirthFrame[] girthDeltas;
    [SerializeField] public Matrix4x4 penetratorBindPose;

    public void DrawGizmos(SkinnedMeshRenderer renderer, Transform rootBone, int sampleCount = 128) {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.green;
        GetGirthFrame(renderer, rootBone).DrawGizmos(rootBone.localToWorldMatrix, sampleCount);
    }

    public GirthFrame GetGirthFrame(SkinnedMeshRenderer renderer, Transform rootBone) {
        var b = baseGirth.Clone();
        b.SampleBlendshapes(renderer, girthDeltas);
        b.SampleTransforms(renderer, penetratorBindPose, rootBone);
        return b;
    }
    

    public static void GetBindPoseBoneLocalPositionRotation(Matrix4x4 boneMatrix, out Vector3 position, out Quaternion rotation, out float scale) {
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
    public static bool TryGetBoneIndex(Transform[] bones, Transform bone, out int index) {
        for (int i = 0; i < bones.Length; i++) {
            if (bones[i] == bone) {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }
    
    public static async Task<SkinnedMeshGirth> GenerateGirthLookup(RendererSubMeshMask rendererWithMask, Shader girthUnwrapShader, Transform[] bones, Matrix4x4 penetratorLocalPose) {
        Matrix4x4 poseMatrix = Matrix4x4.identity;
        var penetratorRoot = bones[0];
        var skinnedMeshRenderer = rendererWithMask.renderer;
        if (!TryGetBoneIndex(skinnedMeshRenderer.bones, penetratorRoot, out int rootBoneID)) {
            throw new UnityException("You must choose a bone on the armature...");
        }

        Mesh skinnedMesh = skinnedMeshRenderer.sharedMesh;
        GetBindPoseBoneLocalPositionRotation(skinnedMesh.bindposes[rootBoneID], out Vector3 posePosition, out Quaternion poseRotation, out float scale);

        var rendererBindPose = Matrix4x4.TRS(posePosition, poseRotation, new Vector3(scale, scale, scale)) * penetratorLocalPose;
        
        poseMatrix = skinnedMeshRenderer.sharedMesh.bindposes[rootBoneID];

        var staticVertices = new List<Vector3>();
        var maxGirth = GetMaxGirth(rendererWithMask, penetratorRoot, rendererBindPose.MultiplyVector(Vector3.forward), rendererBindPose.MultiplyPoint(Vector3.zero), staticVertices);
        
        var baseGirth = await GirthFrame.GenerateGirthFrame(rendererWithMask, penetratorRoot, rendererBindPose, -1, maxGirth, girthUnwrapShader);
        var blendshapeCount = rendererWithMask.GetMesh().blendShapeCount;
        var blendshapeGirths = new GirthFrame[blendshapeCount];
        for (int i = 0; i < blendshapeCount; i++) {
            var blendshapeGirth = await GirthFrame.GenerateGirthFrame(rendererWithMask, penetratorRoot, rendererBindPose, i, maxGirth, girthUnwrapShader);
            blendshapeGirth.ConvertToBlendshape(baseGirth);
            blendshapeGirths[i] = blendshapeGirth;
        }
        return new SkinnedMeshGirth() {
            baseGirth = baseGirth,
            girthDeltas = blendshapeGirths,
            maxGirth = maxGirth,
            penetratorBindPose = rendererBindPose
        };
    }
}

}