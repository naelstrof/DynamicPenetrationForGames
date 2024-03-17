/* Copyright 2024 Naelstrof & Raliv
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 * documentation files (the “Software”), to deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
 * Software.
 * 
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
 * OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace DPG {

using System;
using System.Collections.Generic;
using UnityEngine;

public class PenetrableBasic : Penetrable {
    [SerializeField] private Transform[] transforms;
    [SerializeField] private Transform entranceTransform;
    
    [SerializeField] private bool shouldTruncate = true;
    [SerializeField,Range(0f,1f)] private float truncateNormalizedDistance;
    [SerializeField,Range(0f,1f)] private float holeStartNormalizedDistance;
    [SerializeField] private bool shouldClip = true;
    [SerializeField] private ClippingRange clippingRange;
    [SerializeField,Range(0f,1f)] private float penetrableFriction = 0.5f;

    [Serializable]
    public struct ClippingRange {
        [Range(0f,1f)]
        public float startNormalizedDistance;
        public bool allowAllTheWayThrough;
        [Range(0f,1f)]
        public float endNormalizedDistance;
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

    private float PenetrableNormalizedDistanceSpaceToWorldDistance(float penetrableNormalizedDistance, Penetrator.PenetrationArgs penetrationArgs) {
        float penetrableArcLength = penetrationArgs.alongSpline.GetLengthFromSubsection(GetPoints().Count-1, penetrationArgs.penetrableStartIndex);
        float penetrableDistance = penetrableNormalizedDistance * penetrableArcLength;
        return penetrableDistance;
    }

    public override PenetrationResult SetPenetrated(Penetrator penetrator, Penetrator.PenetrationArgs penetrationArgs ) {
        base.SetPenetrated(penetrator, penetrationArgs);
        startLocalRotation ??= entranceTransform.localRotation;
        float entranceSample = penetrationArgs.alongSpline.GetLengthFromSubsection(penetrationArgs.penetrableStartIndex);
        entranceTransform.up = -penetrationArgs.alongSpline.GetVelocityFromDistance(entranceSample).normalized;
        
        float distanceFromBaseOfPenetrator = -penetrationArgs.penetrationDepth + penetrationArgs.penetratorData.GetWorldLength();

        float holeStartDepth = PenetrableNormalizedDistanceSpaceToWorldDistance(holeStartNormalizedDistance, penetrationArgs);

        float knotForce = penetrator.GetKnotForce(distanceFromBaseOfPenetrator + holeStartDepth);
        
        return new PenetrationResult {
            knotForce = knotForce,
            penetrableFriction = penetrableFriction,
            clippingRange = !shouldClip ? null : new ClippingRangeWorld {
                startDistance = distanceFromBaseOfPenetrator + PenetrableNormalizedDistanceSpaceToWorldDistance(clippingRange.startNormalizedDistance, penetrationArgs),
                endDistance = clippingRange.allowAllTheWayThrough ? distanceFromBaseOfPenetrator + PenetrableNormalizedDistanceSpaceToWorldDistance(clippingRange.endNormalizedDistance, penetrationArgs) : null,
            },
            holeStartDepth = holeStartDepth,
            truncation = !shouldTruncate ? null : new Truncation {
                girth = penetrator.GetWorldGirthRadius(distanceFromBaseOfPenetrator + PenetrableNormalizedDistanceSpaceToWorldDistance(truncateNormalizedDistance, penetrationArgs)),
                length = distanceFromBaseOfPenetrator + PenetrableNormalizedDistanceSpaceToWorldDistance(truncateNormalizedDistance, penetrationArgs),
            },
        }; // TODO: MAKE THIS USE A CONSTRUCTOR ???
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

        if (shouldClip) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spline.GetPositionFromDistance(clippingRange.startNormalizedDistance * arcLength), 0.025f);
            if (!clippingRange.allowAllTheWayThrough) {
                Gizmos.DrawWireSphere(spline.GetPositionFromDistance(clippingRange.endNormalizedDistance * arcLength), 0.025f);
            }
        }

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

}