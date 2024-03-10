using System;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] protected PenetratorData penetratorData;
    
    [SerializeField] private PenetratorRenderers penetratorRenderers;
    
    protected abstract IList<Vector3> GetPoints();
    
    protected virtual void OnEnable() {
        penetratorData.Initialize();
        penetratorRenderers.Initialize();
    }
    protected virtual void OnDisable() {
        penetratorData.Release();
        penetratorRenderers.OnDestroy();
    }
    
    protected virtual void Update() {
        penetratorData.GetSpline(GetPoints(), out var path, out float distanceAlongSpline);
        penetratorRenderers.Update(path, distanceAlongSpline, penetratorData.GetRootTransform(), penetratorData.GetRootForward(), penetratorData.GetRootRight(), penetratorData.GetRootUp());
    }

    protected void OnValidate() {
        penetratorData.OnValidate();
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
