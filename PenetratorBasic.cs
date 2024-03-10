using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenetratorBasic : Penetrator {
    [SerializeField]
    private Transform[] transforms;
    private List<Vector3> points;
    protected override IList<Vector3> GetPoints() {
        points ??= new List<Vector3>();
        points.Clear();
        foreach (var t in transforms) {
            points.Add(t.position);
        }
        return points;
    }

    protected override Quaternion GetDickRotation() {
        return Quaternion.FromToRotation(Vector3.forward, penetratorData.GetRootForward());
    }

    protected override void OnDrawGizmos() {
        points ??= new List<Vector3>();
        if (transforms == null || transforms.Length == 0) {
            return;
        }
        base.OnDrawGizmos();
    }
}
