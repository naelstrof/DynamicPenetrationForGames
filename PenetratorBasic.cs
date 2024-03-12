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
    [SerializeField] private Transform[] transforms;
    [SerializeField] protected Penetrable linkedPenetrable;
    
    private List<Vector3> points = new();
    
    protected override IList<Vector3> GetPoints() {
        points.Clear();
        if (transforms == null) return points;
        foreach (var t in transforms) {
            if (t == null) {
                return points;
            }
            points.Add(t.position);
        }
        if (linkedPenetrable != null) {
            var linkedPoints = new List<Vector3>();
            linkedPoints.AddRange(linkedPenetrable.GetPoints());
            GetSpline(linkedPenetrable.GetPoints(), out var linkedSpline, out var baseDistanceAlongSpline);
            var proximity = linkedSpline.GetDistanceFromSubT(1, 2, 1f);
            var tipProximity = proximity - GetWorldLength();
            linkedPenetrable.SetPenetrated(this, proximity, linkedSpline, 2);
            return LerpPoints(points, linkedPoints, 1f-Mathf.Clamp01(tipProximity/0.2f));
        }
        return points;
    }
    
}
