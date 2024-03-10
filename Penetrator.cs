using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using PenetrationTech;
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
    [SerializeField] private PenetratorRenderers penetratorRenderers;

    private List<Vector3> points;
    private bool isEditingRoot;

    private void Start() {
        points = new List<Vector3>();
        penetrator.Initialize();
        penetratorRenderers.Initialize();
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

        penetrator.Initialize();
        penetrator.GetSpline(transforms[0].rotation, points, out var path, out var distanceAlongSpline);
        Gizmos.color = Color.red;
        Vector3 lastPoint = path.GetPositionFromT(0f);
        for (int i = 0; i < 64; i++) {
            Vector3 newPoint = path.GetPositionFromT((float)i / 64f);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }

        var save = Gizmos.matrix;
        foreach(var weight in path.GetWeights()) {
            Vector3 pointA = CatmullSpline.GetPosition(weight, 0f);
            Vector3 normalA = CatmullSpline.GetVelocity(weight, 0f);
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(pointA, Quaternion.FromToRotation(Vector3.forward, normalA.normalized), Vector3.one - Vector3.forward * 0.8f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one*0.025f);
        }
        Gizmos.matrix = save;

        penetratorRenderers.Initialize();
        penetratorRenderers.Update(path, distanceAlongSpline, penetrator.GetRootTransform(), penetrator.GetRootForward(), penetrator.GetRootRight(), penetrator.GetRootUp());
    }
    
#if UNITY_EDITOR
    public void DrawSceneGUI() {
        Undo.RecordObject(this, "Transforms Updated");
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.white;
        var globalDickRootPositionRotation = Quaternion.LookRotation(penetrator.GetRootTransform().TransformDirection(penetrator.GetRootForward()), penetrator.GetRootTransform().TransformDirection(penetrator.GetRootUp()));
        var globalDickRootPositionOffset = penetrator.GetRootTransform().TransformPoint(penetrator.GetRootPositionOffset());
        globalDickRootPositionOffset = Handles.PositionHandle(globalDickRootPositionOffset, globalDickRootPositionRotation);
        globalDickRootPositionRotation = Handles.RotationHandle(globalDickRootPositionRotation, globalDickRootPositionOffset);
        if (EditorGUI.EndChangeCheck()) {
            penetrator.SetDickPositionInfo(
                penetrator.GetRootTransform().InverseTransformPoint(globalDickRootPositionOffset),
                Quaternion.Inverse(penetrator.GetRootTransform().rotation) * globalDickRootPositionRotation
            );
        }
        Handles.DrawWireDisc(
            globalDickRootPositionOffset+globalDickRootPositionRotation*Vector3.forward * penetrator.GetPenetratorWorldLength(),
            globalDickRootPositionRotation*Vector3.forward,
            0.1f
            );
    }
#endif
    
}
