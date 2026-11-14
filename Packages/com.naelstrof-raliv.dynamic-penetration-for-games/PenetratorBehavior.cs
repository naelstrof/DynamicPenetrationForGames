using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames {

#if UNITY_EDITOR
[CustomEditor(typeof(PenetratorBehavior))]
class PenetratorBehaviorEditor : Editor {
    private void OnSceneGUI() {
        var targetScript = (PenetratorBehavior)target;
        targetScript.DrawSphereHandles();
    }
}
#endif

public class PenetratorBehavior : MonoBehaviour {
    [Serializable]
    private struct PenetratorCollisionSphere {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public Matrix4x4 GetWorldMatrix(float offsetScale) {
            var output =  transform.localToWorldMatrix * Matrix4x4.TRS(localPosition, localRotation, localScale);
            var pos = output.GetPosition();
            var rot = output.rotation;
            var scale = output.lossyScale;
            scale += new Vector3(offsetScale, offsetScale, offsetScale);
            return Matrix4x4.TRS(pos, rot, scale);
        }

        public bool RaycastIntersection(Ray ray) {
            var worldMatrix = GetWorldMatrix(0f);
            var sphereCenter = worldMatrix.GetPosition();
            var sphereRadius = worldMatrix.lossyScale.x * 0.5f;
            var oc = ray.origin - sphereCenter;
            var a = Vector3.Dot(ray.direction, ray.direction);
            var b = 2.0f * Vector3.Dot(oc, ray.direction);
            var c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
            var discriminant = b * b - 4f * a * c;
            return discriminant > 0;
        }

        #if UNITY_EDITOR
        public void DrawHandles(PenetratorBehavior penetratorBehavior) {
            var tool = Tools.current;
            if (tool == Tool.Move) {
                EditorGUI.BeginChangeCheck();
                var newPos = Handles.PositionHandle(transform.position + transform.rotation * localPosition, transform.rotation);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(penetratorBehavior, "Move Collision Sphere");
                    localPosition = Quaternion.Inverse(transform.rotation) * (newPos - transform.position);
                    EditorUtility.SetDirty(penetratorBehavior);
                }
            } else if (tool == Tool.Rotate) {
                EditorGUI.BeginChangeCheck();
                var newRot = Handles.RotationHandle(transform.rotation * localRotation, transform.position + transform.rotation * localPosition);
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(penetratorBehavior, "Rotate Collision Sphere");
                    localRotation = Quaternion.Inverse(transform.rotation) * newRot;
                    EditorUtility.SetDirty(penetratorBehavior);
                }
            } else if (tool == Tool.Scale) {
                EditorGUI.BeginChangeCheck();
                var newScale = Handles.ScaleHandle(localScale, transform.position + transform.rotation * localPosition,
                    transform.rotation * localRotation,
                    HandleUtility.GetHandleSize(transform.position + transform.rotation * localPosition));
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(penetratorBehavior, "Scale Collision Sphere");
                    localScale = newScale;
                    EditorUtility.SetDirty(penetratorBehavior);
                }
            }
        }
        #endif
    }

    [SerializeField] private Transform[] bones;
    [SerializeField] private PenetrableBehavior penetrableBehavior;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private PenetratorCollisionSphere[] _penetratorCollisionSpheres;
    private BoneChainOnCurve _boneChainOnCurve;
    private BoneChainCompression _boneChainCompression;
    private SphereCasterRenderer.SphereCast[] _sphereCasts;
    private const float sphereCastInflation = 0.1f;
    private List<Vector3> cachedPoints = new List<Vector3>();

    private void OnEnable() {
        _boneChainOnCurve = new BoneChainOnCurve(bones);
        _boneChainCompression = new BoneChainCompression(_boneChainOnCurve.GetTotalLength());
        _sphereCasts = new SphereCasterRenderer.SphereCast[_penetratorCollisionSpheres.Length];
        var dickDir = (bones[^1].position - bones[0].position).normalized;
        Array.Sort(_penetratorCollisionSpheres, (a, b) => {
            var aPos = a.GetWorldMatrix(0f).GetPosition();
            var bPos = b.GetWorldMatrix(0f).GetPosition();
            var aDist = Vector3.Dot(aPos - bones[0].position, dickDir);
            var bDist = Vector3.Dot(bPos - bones[0].position, dickDir);
            return aDist.CompareTo(bDist);
        });
    }
    

    private void LateUpdate() {
        Penetrator penetrator = new Penetrator() {
            position = bones[0].position,
            direction = (bones[1].position - bones[0].position).normalized,
            penetratorLength = _boneChainOnCurve.GetTotalLength()
        };
        var penetrable = penetrableBehavior.GetPenetrable();
        var penetration = new Penetration(penetrator, penetrable, cachedPoints);
            
        var orificeDist = penetration.spline.GetDistanceFromTime(penetration.penetrableT);
        _boneChainCompression.Tick(orificeDist);
        var aimDist = _boneChainCompression.GetTipPosition();
        var aimPoint = penetration.spline.GetPositionFromDistance(aimDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        aimPoint = penetration.spline.GetPositionFromDistance(orificeDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        var lengthMultiplier = _boneChainCompression.GetTipPosition() / _boneChainOnCurve.GetRestLength();
        penetration.Render(_boneChainOnCurve, lengthMultiplier);
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
        
        if (_sphereCasts.Length != _penetratorCollisionSpheres.Length) {
            _sphereCasts = new SphereCasterRenderer.SphereCast[_penetratorCollisionSpheres.Length];
        }
        for (int i=0; i < _penetratorCollisionSpheres.Length; i++) {
            var sphere = _penetratorCollisionSpheres[i];
            _sphereCasts[i] = new SphereCasterRenderer.SphereCast(sphere.GetWorldMatrix(sphereCastInflation), 0.5f, 0f, true, true, true);
        }
        penetrableBehavior.AddSphereCasts(_sphereCasts);
    }

    private void OnDrawGizmosSelected() {
        if (_penetratorCollisionSpheres == null) return;
        foreach (var sphere in _penetratorCollisionSpheres) {
            if (sphere.transform == null) continue;
            Gizmos.matrix = sphere.GetWorldMatrix(Application.isPlaying ? sphereCastInflation : 0f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, 1f);
        }
    }

    private void OnValidate() {
        if (_penetratorCollisionSpheres == null) return;
        for(int i=0; i < _penetratorCollisionSpheres.Length; i++) {
            if (_penetratorCollisionSpheres[i].localRotation is { x: 0, y: 0, z: 0, w: 0 }) {
                _penetratorCollisionSpheres[i].localRotation = Quaternion.identity;
            }
            if (_penetratorCollisionSpheres[i].localScale is { x: 0, y: 0, z: 0 }) {
                _penetratorCollisionSpheres[i].localScale = Vector3.one*0.01f;
            }

            if (_penetratorCollisionSpheres[i].localScale.x < 0f) {
                _penetratorCollisionSpheres[i].localScale.x = 0.01f;
            }
            if (_penetratorCollisionSpheres[i].localScale.y < 0f) {
                _penetratorCollisionSpheres[i].localScale.y = 0.01f;
            }
            if (_penetratorCollisionSpheres[i].localScale.z < 0f) {
                _penetratorCollisionSpheres[i].localScale.z = 0.01f;
            }
        }
    }

#if UNITY_EDITOR
    public void DrawSphereHandles() {
        if (Application.isPlaying) return;
        if (_penetratorCollisionSpheres == null) return;
        for (int i=0;i<_penetratorCollisionSpheres.Length;i++) {
            if (_penetratorCollisionSpheres[i].transform == null) continue;
            _penetratorCollisionSpheres[i].DrawHandles(this);
        }
    }
#endif
}


}