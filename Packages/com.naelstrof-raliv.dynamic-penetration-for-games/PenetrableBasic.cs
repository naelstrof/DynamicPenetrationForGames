using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenetrableBasic : Penetrable {
    [SerializeField] private Transform[] transforms;
    [SerializeField] private Transform entranceTransform;
    
    private List<Vector3> points = new();

    private Quaternion startLocalRotation;

    private void Awake() {
        startLocalRotation = entranceTransform.localRotation;
    }

    public override IList<Vector3> GetPoints() {
        points.Clear();
        if (transforms == null) return points;
        foreach (var t in transforms) {
            if (t == null) {
                return points;
            }
            points.Add(t.position);
        }
        return points;
    }

    public override PenetrationData SetPenetrated(PenetratorData penetrator, float penetrationDepth, CatmullSpline alongSpline, int penetrableStartIndex) {
        base.SetPenetrated(penetrator, penetrationDepth, alongSpline, penetrableStartIndex);
        float entranceSample = alongSpline.GetDistanceFromSubT(0, penetrableStartIndex, 1f);
        if (penetrationDepth > 0) {
            entranceTransform.up = -alongSpline.GetVelocityFromDistance(entranceSample).normalized;
        } else {
            entranceTransform.localRotation = startLocalRotation;
        }
        entranceTransform.localScale = Vector3.one + Vector3.one*(penetrator.GetWorldGirthRadius(-penetrationDepth + penetrator.GetPenetratorWorldLength() )*10f);
        return new PenetrationData();
    }
}
