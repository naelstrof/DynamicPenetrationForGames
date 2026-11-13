using System;
using Unity.Mathematics;
using UnityEngine;

namespace GatorDragonGames {

public class BoneChainOnCurveTest : MonoBehaviour {

    [SerializeField] private Transform[] bones;
    [SerializeField] private CurveOfTransforms curveOfTransforms;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    private Spline spline = new Spline();
    private BoneChainOnCurve _boneChainOnCurve;
    private BoneChainCompression _boneChainCompression;
    private PenetratorApproximationSphere[] _penetratorApproximationSpheres;
    private SphereCasterRenderer.SphereCast[] _sphereCasts;
    
    private struct PenetratorApproximationSphere {
        public Transform transform;
        public Matrix4x4 matrix;

        public Matrix4x4 GetWorldMatrix(float offset) {
            var output =  transform.localToWorldMatrix * matrix;
            var pos = output.GetPosition();
            var rot = output.rotation;
            var scale = output.lossyScale;
            scale += new Vector3(offset, offset, offset);
            return Matrix4x4.TRS(pos, rot, scale);
        }
    }

    private void OnEnable() {
        _boneChainOnCurve = new BoneChainOnCurve(bones);
        _boneChainCompression = new BoneChainCompression(_boneChainOnCurve.GetTotalLength());
        _penetratorApproximationSpheres = GeneratePenetratorApproximationSpheres(bones);
        _sphereCasts = new SphereCasterRenderer.SphereCast[_penetratorApproximationSpheres.Length];
    }
    
    private PenetratorApproximationSphere[] GeneratePenetratorApproximationSpheres(Transform[] bones) {
        var spheres = new PenetratorApproximationSphere[bones.Length];
        var mesh = skinnedMeshRenderer.sharedMesh;
        var vertices = mesh.vertices;
        var boneWeights = mesh.boneWeights;
        var skinnedMeshBones = skinnedMeshRenderer.bones;
        
        for (int b = 0; b < bones.Length; b++) {
            var targetBone = bones[b];
            var index = Array.IndexOf(skinnedMeshBones, targetBone);
            const float threshold = 0.05f;
            float3 min = new float3(float.MaxValue);
            float3 max = new float3(float.MinValue);
            bool found = false;
            for (int i=0;i<vertices.Length;i++) {
                var boneWeight = boneWeights[i];
                if (boneWeight.boneIndex0 == index || boneWeight.boneIndex1 == index || boneWeight.boneIndex2 == index || boneWeight.boneIndex3 == index) {
                    var weight = boneWeights[i];
                    if ((weight.boneIndex0 == index && weight.weight0 > threshold) ||
                        (weight.boneIndex1 == index && weight.weight1 > threshold) ||
                        (weight.boneIndex2 == index && weight.weight2 > threshold) ||
                        (weight.boneIndex3 == index && weight.weight3 > threshold)) {
                        var armatureVertex = vertices[i];
                        var worldVertex = skinnedMeshRenderer.transform.TransformPoint(armatureVertex);
                        var localVertex = targetBone.InverseTransformPoint(worldVertex);
                        min = math.min(min, localVertex);
                        max = math.max(max, localVertex);
                        found = true;
                    }
                }
            }

            if (found) {
                var extents = (max - min) * 0.5f;
                var center = (max + min) * 0.5f;
                var sphereMatrix = Matrix4x4.TRS(center, Quaternion.identity, extents);
                spheres[b] = new PenetratorApproximationSphere() { transform = targetBone, matrix = sphereMatrix };
            } else {
                spheres[b] = new PenetratorApproximationSphere() { transform = targetBone, matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.zero) };
            }
        }

        return spheres;
    }

    private void LateUpdate() {
        var backProjectedHandle = bones[0].position + (bones[0].position - bones[1].position) * 1f;
        curveOfTransforms.GetSplineWithPrependedTransforms(new[] { bones[0] }, backProjectedHandle, spline);
        var orificeDist = spline.GetLengthFromSubsection(1, 0);
        _boneChainCompression.Tick(orificeDist);
        var aimDist = _boneChainCompression.GetTipPosition();
        var aimPoint = spline.GetPositionFromDistance(aimDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        aimPoint = spline.GetPositionFromDistance(orificeDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        var lengthMultiplier = _boneChainCompression.GetTipPosition() / _boneChainOnCurve.GetRestLength();
        _boneChainOnCurve.AlignToSpline(spline, lengthMultiplier);
        // get blend shape index by name
        var blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.002");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.003");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.004");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.005");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        
        for (int i=0; i < _penetratorApproximationSpheres.Length; i++) {
            var sphere = _penetratorApproximationSpheres[i];
            _sphereCasts[i] = new SphereCasterRenderer.SphereCast(sphere.GetWorldMatrix(0.25f), 0.5f, 0f, true, true, true);
        }
        curveOfTransforms.AddSphereCasts(_sphereCasts);
    }

    private void OnDrawGizmosSelected() {
        if (_penetratorApproximationSpheres == null) return;
        foreach (var sphere in _penetratorApproximationSpheres) {
            if (sphere.transform == null) continue;
            Gizmos.matrix = sphere.GetWorldMatrix(0.25f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, 1f);
        }
    }

    private void OnValidate() {
        if (bones == null) return;
        _penetratorApproximationSpheres = GeneratePenetratorApproximationSpheres(bones);
        _sphereCasts = new SphereCasterRenderer.SphereCast[_penetratorApproximationSpheres.Length];
    }
}

}