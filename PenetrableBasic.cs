using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PenetrableBasic : Penetrable {
    [SerializeField] private Transform[] transforms;
    [SerializeField] private Transform entranceTransform;
    [SerializeField,Range(0f,1f)] private float truncateNormalizedDistance;
    [SerializeField,Range(0f,1f)] private float holeStartNormalizedDistance;
    [SerializeField] private ClippingRange clippingRange;
    [SerializeField,Range(0f,1f)] private float penetrableFriction = 0.5f;

    [Serializable]
    public struct ClippingRange {
        [Range(0f,1f)]
        public float startNormalizedDistance;
        [Range(0f,1f)]
        public float endNormalizedDistance;
    }
    
    public struct ClippingRangeWorld {
        public float startDistance;
        public float endDistance;
    }

    private List<Vector3> points = new();

    private Quaternion? startLocalRotation;

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

    private float PenetrableNormalizedDistanceSpaceToWorldDistance(float penetrableNormalizedDistance, CatmullSpline spline, int penetrableStartIndex) {
        float penetrableArcLength = spline.GetLengthFromSubsection(GetPoints().Count-1, penetrableStartIndex);
        float penetrableDistance = penetrableNormalizedDistance * penetrableArcLength;
        return penetrableDistance;
    }

    public override PenetrationData SetPenetrated(Penetrator penetrator, float penetrationDepth, CatmullSpline alongSpline, int penetrableStartIndex) {
        base.SetPenetrated(penetrator, penetrationDepth, alongSpline, penetrableStartIndex);
        startLocalRotation ??= entranceTransform.localRotation;
        float entranceSample = alongSpline.GetLengthFromSubsection(penetrableStartIndex);
        entranceTransform.up = -alongSpline.GetVelocityFromDistance(entranceSample).normalized;
        
        float distanceFromBaseOfPenetrator = -penetrationDepth + penetrator.GetUnperturbedWorldLength(); // FIXME: We use unperturbed here instead of the real world length. Penetrables probably shouldn't be aware of squash/stretch. Pre-calc on penetrator?

        float holeStartDepth = PenetrableNormalizedDistanceSpaceToWorldDistance(holeStartNormalizedDistance, alongSpline, penetrableStartIndex);

        float knotForce = penetrator.GetKnotForce(distanceFromBaseOfPenetrator + holeStartDepth);
        
        //entranceTransform.localScale = Vector3.one + Vector3.one*(penetrator.GetWorldGirthRadius(distanceFromBaseOfPenetrator)*10f);
        PenetrationData data = new PenetrationData() {
            knotForce = knotForce,
            penetrableFriction = penetrableFriction,
            clippingRange = new ClippingRangeWorld() {
                startDistance = distanceFromBaseOfPenetrator + PenetrableNormalizedDistanceSpaceToWorldDistance(clippingRange.startNormalizedDistance, alongSpline, penetrableStartIndex),
                endDistance = distanceFromBaseOfPenetrator + PenetrableNormalizedDistanceSpaceToWorldDistance(clippingRange.endNormalizedDistance, alongSpline, penetrableStartIndex),
            },
            holeStartDepth = holeStartDepth,
            truncationGirth = penetrator.GetWorldGirthRadius(distanceFromBaseOfPenetrator + PenetrableNormalizedDistanceSpaceToWorldDistance(truncateNormalizedDistance, alongSpline, penetrableStartIndex)),
            truncationLength = distanceFromBaseOfPenetrator + PenetrableNormalizedDistanceSpaceToWorldDistance(truncateNormalizedDistance, alongSpline, penetrableStartIndex),
        }; // TODO: MAKE THIS USE A CONSTRUCTOR
        return data;
    }

    public override void SetUnpenetrated(Penetrator penetrator) {
        base.SetUnpenetrated(penetrator);
        if (entranceTransform == null) {
            return;
        }
        if (startLocalRotation.HasValue) {
            entranceTransform.localRotation = startLocalRotation.Value;
        }
        entranceTransform.localScale = Vector3.one;
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();
        if (transforms == null || transforms.Length <= 1) {
            return;
        }
        CatmullSpline spline = new CatmullSpline(GetPoints());
        var lastColor = Gizmos.color;
        float arcLength = spline.arcLength;
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spline.GetPositionFromDistance(clippingRange.startNormalizedDistance*arcLength), 0.025f);
        Gizmos.DrawWireSphere(spline.GetPositionFromDistance(clippingRange.endNormalizedDistance*arcLength), 0.025f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(spline.GetPositionFromDistance(truncateNormalizedDistance*arcLength), 0.025f);
        Gizmos.color = lastColor;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spline.GetPositionFromDistance(holeStartNormalizedDistance*arcLength), 0.025f);
        Gizmos.color = lastColor;
    }

    private void OnValidate() {
        startLocalRotation = null;
    }
}
