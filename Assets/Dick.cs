using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Dick))]
public class DickInspector : Editor {
    void OnSceneGUI() {
        var script = (Dick)target;
        Undo.RecordObject(this, "Transforms Updated");
        script.DrawSceneGUI();
    }
}

#endif

public class Dick : MonoBehaviour {
    [SerializeField] private Penetrator penetrator;
    [SerializeField] private Transform[] transforms;

    private List<Vector3> points;

    private void Start() {
        points = new List<Vector3>();
        penetrator.Initialize();
    }

    protected virtual void OnDrawGizmos() {
        points ??= new List<Vector3>();
        if (transforms == null || transforms.Length == 0) {
            return;
        }
        points.Clear();
        foreach (var t in transforms) {
            points.Add(t.position);
        }
        var path = penetrator.GetSpline(transforms[0].rotation, points);
        Gizmos.color = Color.red;
        Vector3 lastPoint = path.GetPositionFromT(0f);
        for (int i = 0; i < 64; i++) {
            Vector3 newPoint = path.GetPositionFromT((float)i / 64f);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }
    
#if UNITY_EDITOR
    public void DrawSceneGUI() {
        Undo.RecordObject(this, "Transforms Updated");
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.white;
        var rotation = penetrator.GetRootTransform().rotation * Quaternion.LookRotation(penetrator.GetRootForward(), penetrator.GetRootUp());
        var globalDickRootPositionOffset = penetrator.GetRootTransform().TransformPoint(penetrator.GetRootPositionOffset());
        var globalDickRootPositionRotation = rotation * penetrator.GetRootTransform().rotation;
        globalDickRootPositionOffset = Handles.PositionHandle(globalDickRootPositionOffset, globalDickRootPositionRotation);
        globalDickRootPositionRotation = Handles.RotationHandle(globalDickRootPositionRotation, globalDickRootPositionOffset);
        if (EditorGUI.EndChangeCheck()) {
            penetrator.SetDickPositionInfo(
                penetrator.GetRootTransform().InverseTransformPoint(globalDickRootPositionOffset),
                Quaternion.Inverse(penetrator.GetRootTransform().rotation) * globalDickRootPositionRotation
            );
        }
    }
#endif
    
}
