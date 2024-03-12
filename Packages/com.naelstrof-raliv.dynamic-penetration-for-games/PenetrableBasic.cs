using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PenetrableBasic : Penetrable {
    [SerializeField] private Transform[] transforms;
    [SerializeField] private Transform entranceTransform;
    [SerializeField,Range(0f,1f)] private float truncateT;
    [SerializeField] private ClippingRange clippingRange;

    [Serializable]
    public struct ClippingRange {
        [Range(0f,1f)]
        public float startT;
        [Range(0f,1f)]
        public float endT;
    }
    
    public struct ClippingRangeWorld {
        public float startDistance;
        public float endDistance;
    }

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

    private float PenetrableTSpaceToWorldDistance(float t, CatmullSpline spline, int penetrableStartIndex) {
        return spline.GetDistanceFromSubT(penetrableStartIndex, penetrableStartIndex+GetPoints().Count-1, t);
    }

    public override PenetrationData SetPenetrated(Penetrator penetrator, float penetrationDepth, CatmullSpline alongSpline, int penetrableStartIndex) {
        base.SetPenetrated(penetrator, penetrationDepth, alongSpline, penetrableStartIndex);
        float entranceSample = alongSpline.GetDistanceFromSubT(0, penetrableStartIndex, 1f);
        entranceTransform.up = -alongSpline.GetVelocityFromDistance(entranceSample).normalized;
        //float distanceFromBaseOfPenetrator = -penetrationDepth + penetrator.GetWorldLength();
        float distanceFromBaseOfPenetrator = alongSpline.GetDistanceFromSubT(1, penetrableStartIndex, 1f);
        
        entranceTransform.localScale = Vector3.one + Vector3.one*(penetrator.GetWorldGirthRadius(distanceFromBaseOfPenetrator)*10f);
        PenetrationData data = new PenetrationData() {
            clippingRange = new ClippingRangeWorld() {
                startDistance = distanceFromBaseOfPenetrator + PenetrableTSpaceToWorldDistance(clippingRange.startT, alongSpline, penetrableStartIndex),
                endDistance = distanceFromBaseOfPenetrator + PenetrableTSpaceToWorldDistance(clippingRange.endT, alongSpline, penetrableStartIndex),
            },
            knotForce = 0f,
            stimulation = 0f,
            tipIsInside = true,
            truncationGirth = penetrator.GetWorldGirthRadius(distanceFromBaseOfPenetrator + PenetrableTSpaceToWorldDistance(truncateT, alongSpline, penetrableStartIndex)),
            truncationLength = distanceFromBaseOfPenetrator + PenetrableTSpaceToWorldDistance(truncateT, alongSpline, penetrableStartIndex),
        }; // TODO: MAKE THIS USE A CONSTRUCTOR
        return data;
    }

    public override void SetUnpenetrated(Penetrator penetrator) {
        entranceTransform.localRotation = startLocalRotation;
        entranceTransform.localScale = Vector3.one;
    }

    private void OnDrawGizmosSelected() {
        if (transforms == null || transforms.Length <= 1) {
            return;
        }
        CatmullSpline spline = new CatmullSpline(GetPoints());
        var lastColor = Gizmos.color;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spline.GetPositionFromT(clippingRange.startT), 0.025f);
        Gizmos.DrawWireSphere(spline.GetPositionFromT(clippingRange.endT), 0.025f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spline.GetPositionFromT(truncateT), 0.025f);
        Gizmos.color = lastColor;
    }
}
