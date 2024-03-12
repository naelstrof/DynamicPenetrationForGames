using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Penetrator))]
public class PenetratorInspector : Editor {
    
    private bool isEditingRoot;
    
    public override void OnInspectorGUI() {
        var script = target as Penetrator;
        if (GUILayout.Button("Edit position and orientation")) {
            isEditingRoot = true;
            SceneView.RepaintAll();
        }
        base.OnInspectorGUI();
    }

    protected void OnSceneGUI() {
        var script = (Penetrator)target;
        Undo.RecordObject(this, "Transforms Updated");
        script.DrawSceneGUI(isEditingRoot);
    }

}

#endif

[ExecuteAlways]
public abstract class Penetrator : MonoBehaviour {
    [FormerlySerializedAs("penetrator")]
    [SerializeField] private PenetratorData penetratorData;
    [SerializeField, Range(0.1f, 2f)] protected float squashAndStretch = 1f;
    
    [SerializeField] protected PenetratorRenderers penetratorRenderers;

    protected Penetrable.PenetrationData data = new Penetrable.PenetrationData() {
        truncationLength = 999f, // TODO: Make this a real constructor!
    };

    protected abstract IList<Vector3> GetPoints();
    
    protected virtual void OnEnable() {
        penetratorData.Initialize();
        penetratorRenderers.Initialize();
    }

    public void GetSpline(IList<Vector3> inputPoints, out CatmullSpline spline, out float baseDistanceAlongSpline) => penetratorData.GetSpline(inputPoints, out spline, out baseDistanceAlongSpline);
    public Transform GetRootTransform() => penetratorData.GetRootTransform();
    public Vector3 GetRootPositionOffset() => penetratorData.GetRootPositionOffset();
    public Vector3 GetRootForward() => penetratorData.GetRootForward();
    public Vector3 GetRootUp() => penetratorData.GetRootUp();
    public Vector3 GetRootRight() => penetratorData.GetRootRight();
    public virtual float GetWorldLength() {
        return penetratorData.GetPenetratorWorldLength() * squashAndStretch;
    }
    
    public virtual float GetWorldGirthRadius(float distanceAlongPenetrator) {
        return penetratorData.GetWorldGirthRadius(distanceAlongPenetrator/squashAndStretch);
    }

    public virtual void SetPenetrationData(Penetrable.PenetrationData data) {
        this.data = data;
    }
    protected virtual void LateUpdate() {
        penetratorData.GetSpline(GetPoints(), out var path, out float distanceAlongSpline);
        penetratorRenderers.Update(
            path,
            penetratorData.GetPenetratorWorldLength(),
            squashAndStretch,
            0f,
            distanceAlongSpline,
            penetratorData.GetRootTransform(),
            penetratorData.GetRootForward(),
            penetratorData.GetRootRight(),
            penetratorData.GetRootUp(),
            data.truncationLength,
            data.clippingRange.startDistance,
            data.clippingRange.endDistance,
            data.truncationGirth
        );
    }

    protected virtual void OnDisable() {
        penetratorData.Release();
        penetratorRenderers.OnDestroy();
    }

    protected virtual void OnValidate() {
        penetratorData.OnValidate();
    }

    public static IList<Vector3> LerpPoints(IList<Vector3> a, IList<Vector3> b, float t) {
        while (a.Count < b.Count) a.Add(a[^1]+(a[^1]-a[^2]));
        while (b.Count < a.Count) b.Add(b[^1]+(b[^1]-b[^2]));
        var aSpline = new CatmullSpline(a);
        var bSpline = new CatmullSpline(b);
        var lerpPoints = new List<Vector3>();
        for (var index = 0; index < a.Count; index++) {
            var sourceT = aSpline.GetDistanceFromTime((float)index / (a.Count - 1));
            var targetT = bSpline.GetDistanceFromTime((float)index / (b.Count - 1));
            var lerpT = bSpline.GetPositionFromDistance(Mathf.Lerp(sourceT, targetT, t));
            lerpPoints.Add(Vector3.Lerp(a[index], lerpT, t));
        }
        return lerpPoints;
    }

    protected virtual void OnDrawGizmos() {
        if (GetPoints().Count == 0) {
            return;
        }
        penetratorData.GetSpline(GetPoints(), out var path, out var distanceAlongSpline);
        CatmullSpline.GizmosDrawSpline(path, Color.red, Color.green);
    }
    
    
#if UNITY_EDITOR
    public void DrawSceneGUI(bool isEditingRoot) {
        if (!isEditingRoot) return;
        Undo.RecordObject(this, "Transforms Updated");
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.white;
        var globalPenetratorRootPositionRotation = Quaternion.LookRotation(penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootForward()), penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootUp()));
        var globalPenetratorRootPositionOffset = penetratorData.GetRootTransform().TransformPoint(penetratorData.GetRootPositionOffset());
        globalPenetratorRootPositionOffset = Handles.PositionHandle(globalPenetratorRootPositionOffset, globalPenetratorRootPositionRotation);
        globalPenetratorRootPositionRotation = Handles.RotationHandle(globalPenetratorRootPositionRotation, globalPenetratorRootPositionOffset);
        if (EditorGUI.EndChangeCheck()) {
            penetratorData.SetPenetratorPositionInfo(
                penetratorData.GetRootTransform().InverseTransformPoint(globalPenetratorRootPositionOffset),
                Quaternion.Inverse(penetratorData.GetRootTransform().rotation) * globalPenetratorRootPositionRotation
            );
        }
        Handles.DrawWireDisc(
            globalPenetratorRootPositionOffset+globalPenetratorRootPositionRotation*Vector3.forward * penetratorData.GetPenetratorWorldLength(),
            globalPenetratorRootPositionRotation*Vector3.forward,
            0.1f
            );
    }
#endif
    
}
