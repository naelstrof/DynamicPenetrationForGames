using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenetrableBasic : Penetrable {
    [SerializeField] private Transform[] transforms;
    [SerializeField] private Transform entranceTransform;
    
    private List<Vector3> points = new();
    
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

    public override void SetPenetrated(PenetratorData penetrator, float distanceFromPenetrator, CatmullSpline alongSpline, int penetrableStartIndex) {
        base.SetPenetrated(penetrator, distanceFromPenetrator, alongSpline, penetrableStartIndex);
        float entranceSample = alongSpline.GetDistanceFromSubT(0, penetrableStartIndex, 1f);
        entranceTransform.up = -alongSpline.GetVelocityFromDistance(entranceSample).normalized;
        entranceTransform.localScale = Vector3.one + Vector3.one*penetrator.GetWorldGirthRadius(penetrator.GetPenetratorWorldLength() - distanceFromPenetrator);
    }
}
