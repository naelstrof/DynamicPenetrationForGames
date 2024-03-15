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

    protected bool IsValid() {
#if UNITY_EDITOR
        return penetratorData.IsValid() && penetratorRenderers.IsValid();
#else
        return true;
#endif
    }

    public void GetSpline(IList<Vector3> inputPoints, out CatmullSpline spline, out float baseDistanceAlongSpline) => penetratorData.GetSpline(inputPoints, out spline, out baseDistanceAlongSpline);
    public Vector3 GetBasePointOne() => penetratorData.GetBasePointOne();
    public Vector3 GetBasePointTwo() => penetratorData.GetBasePointTwo();
    public Transform GetRootTransform() => penetratorData.GetRootTransform();
    public Vector3 GetRootPositionOffset() => penetratorData.GetRootPositionOffset();
    public Vector3 GetRootForward() => penetratorData.GetRootForward();
    public Vector3 GetRootUp() => penetratorData.GetRootUp();
    public Vector3 GetRootRight() => penetratorData.GetRootRight();
    public Texture2D GetDetailMap() => penetratorData.GetDetailMap();
    public RenderTexture GetGirthMap() => penetratorData.GetGirthMap();
    public float GetGirthScaleFactor() => penetratorData.GetGirthScaleFactor();
    public float GetPenetratorAngleOffset(CatmullSpline path) {
        Vector3 initialRight = path.GetBinormalFromT(0f);
        Vector3 initialForward = path.GetVelocityFromT(0f).normalized;
        Vector3 initialUp = Vector3.Cross(initialForward, initialRight).normalized;
        Vector3 worldDickUp = GetRootTransform().TransformDirection(GetRootUp()).normalized;
        Vector2 worldDickUpFlat = new Vector2(Vector3.Dot(worldDickUp,initialRight), Vector3.Dot(worldDickUp,initialUp));
        float angle = Mathf.Atan2(worldDickUpFlat.y, worldDickUpFlat.x)-Mathf.PI/2f;
        return angle;
    }
    public virtual float GetWorldLength() {
        return penetratorData.GetPenetratorWorldLength() * squashAndStretch;
    }
    
    public float GetUnperturbedWorldLength() {
        return penetratorData.GetPenetratorWorldLength();
    }
    
    public virtual float GetWorldGirthRadius(float distanceAlongPenetrator) {
        return penetratorData.GetWorldGirthRadius(distanceAlongPenetrator/squashAndStretch);
    }

    public virtual void SetPenetrationData(Penetrable.PenetrationData data) {
        this.data = data;
    }
    protected virtual void LateUpdate() {
        if (!IsValid()) {
            return;
        }
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
        if (t == 0) {
            return a;
        }

        if (Math.Abs(t - 1f) < Mathf.Epsilon) {
            return b;
        }
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

    protected virtual void OnDrawGizmosSelected() {
        if (GetPoints().Count == 0 || !IsValid()) {
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
