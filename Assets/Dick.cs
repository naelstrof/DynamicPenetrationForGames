using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
