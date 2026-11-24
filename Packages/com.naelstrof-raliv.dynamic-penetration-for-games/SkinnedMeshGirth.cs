using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DPG {
[Serializable]
public struct SkinnedMeshGirth {
    [SerializeField] public float maxGirth;
    [SerializeField] public GirthFrame baseGirth;
    [SerializeField] public GirthFrame[] girthDeltas;
    [SerializeField] public Matrix4x4 penetratorBindPose;

    public struct SkinnedMeshGirthSampler {
        public Matrix4x4[] localToWorlds;
        public Matrix4x4[] bindPoses;
        public Transform[] bones;
        public float[] blendshapes;

        public SkinnedMeshGirthSampler(SkinnedMeshRenderer renderer) {
            var boneCount = renderer.bones.Length;
            localToWorlds = new Matrix4x4[boneCount];
            bones = renderer.bones;
            for(int i=0;i<boneCount;i++) {
                localToWorlds[i] = bones[i].localToWorldMatrix;
            }
            bindPoses = renderer.sharedMesh.bindposes;
            blendshapes = new float[renderer.sharedMesh.blendShapeCount];
        }

        public void Sample(SkinnedMeshRenderer renderer) {
            var boneCount = bones.Length;
            for(int i=0;i<boneCount;i++) {
                localToWorlds[i] = bones[i].localToWorldMatrix;
            }
            var blendshapeLength = blendshapes.Length;
            for(int i=0;i<blendshapeLength;i++) {
                blendshapes[i] = renderer.GetBlendShapeWeight(i);
            }
        }
    }

    public void DrawGizmos(ref GirthFrame girthFrameMemory, SkinnedMeshRenderer renderer, SkinnedMeshGirthSampler sampler, Transform rootBone, int sampleCount = 128) {
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.green;
        GetGirthFrame(ref girthFrameMemory, renderer, sampler, rootBone);
        girthFrameMemory.DrawGizmos(sampleCount);
    }

    public void GetGirthFrame(ref GirthFrame output, SkinnedMeshRenderer renderer, SkinnedMeshGirthSampler sampler, Transform rootBone) {
        baseGirth.Clone(ref output);
        sampler.Sample(renderer);
        output.SampleBlendshapes(girthDeltas, sampler);
        output.SampleTransforms(sampler.bindPoses, sampler.localToWorlds, penetratorBindPose, rootBone);
        output.TransformToWorld(rootBone.localToWorldMatrix);
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
        var penetratorRoot = bones[0];
        var skinnedMeshRenderer = rendererWithMask.renderer;
        if (!TryGetBoneIndex(skinnedMeshRenderer.bones, penetratorRoot, out int rootBoneID)) {
            throw new UnityException("You must choose a bone on the armature...");
        }

        Mesh skinnedMesh = skinnedMeshRenderer.sharedMesh;
        GetBindPoseBoneLocalPositionRotation(skinnedMesh.bindposes[rootBoneID], out Vector3 posePosition, out Quaternion poseRotation, out float scale);

        var rendererBindPose = Matrix4x4.TRS(posePosition, poseRotation, new Vector3(scale, scale, scale)) * penetratorLocalPose;

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