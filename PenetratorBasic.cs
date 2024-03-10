using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PenetratorBasic))]
public class PenetratorBasicInspector : PenetratorInspector { }
#endif

[ExecuteAlways]
public class PenetratorBasic : Penetrator {
    [SerializeField]
    private Transform[] transforms;
    [SerializeField] private PenetratorRenderers penetratorRenderers;
    
    private List<Vector3> points;

    private void Awake() {
        penetratorRenderers.Initialize();
    }

    private void OnDestroy() {
        penetratorRenderers.OnDestroy();
    }
    
    protected override IList<Vector3> GetPoints() {
        points ??= new List<Vector3>();
        points.Clear();
        foreach (var t in transforms) {
            points.Add(t.position);
        }
        return points;
    }

    protected void Update() {
        if (transforms == null || transforms.Length == 0) {
            return;
        }
        penetratorData.GetSpline(GetPoints(), out var path, out float distanceAlongSpline);
        penetratorRenderers.Update(path, distanceAlongSpline, penetratorData.GetRootTransform(), penetratorData.GetRootForward(), penetratorData.GetRootRight(), penetratorData.GetRootUp());
    }

    protected override void OnDrawGizmos() {
        if (transforms == null || transforms.Length == 0) {
            return;
        }
        base.OnDrawGizmos();
    }
}
