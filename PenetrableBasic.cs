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
    [SerializeField] private List<Transform> transforms;
    
    [SerializeField] private bool shouldTruncate = true;
    [SerializeField,Range(0f,1f)] private float truncateNormalizedDistance;
    [SerializeField,Range(0f,1f)] private float holeStartNormalizedDistance;
    [SerializeField] private bool shouldClip = true;
    [SerializeField] private ClippingRange clippingRange;
    [SerializeField,Range(0f,1f)] private float penetrableFriction = 0.5f;
    [SerializeField] private List<KnotForceSampleLocation> knotForceSampleLocations;
    private CatmullSpline cachedSpline;

    [Serializable]
    public struct KnotForceSampleLocation {
        [Range(0f,1f)] public float normalizedDistance;
    }

    [Serializable]
    public struct ClippingRange {
        [Range(0f,1f)]
        public float startNormalizedDistance;
        public bool allowAllTheWayThrough;
        [Range(0f,1f)]
        public float endNormalizedDistance;
    }

    private List<Vector3> points = new();

    public void GetTransforms(IList<Transform> output) {
        output.Clear();
        foreach (var t in transforms) {
            output.Add(t);
        }
    }
    public void SetTransforms(IList<Transform> newTransforms) {
        if (transforms == null) {
            transforms = new List<Transform>(newTransforms);
            return;
        }
        transforms.Clear();
        transforms.AddRange(newTransforms);
    }
    public void SetShouldTruncate(bool shouldTruncate) {
        this.shouldTruncate = shouldTruncate;
    }
    public bool GetShouldTruncate() {
        return shouldTruncate;
    }
    public void SetShouldClip(bool shouldClip) {
        this.shouldClip = shouldClip;
    }
    public bool GetShouldClip() {
        return shouldClip;
    }
    public ClippingRange GetClippingRange() {
        return clippingRange;
    }
    public void SetClippingRange(ClippingRange clippingRange) {
        this.clippingRange = clippingRange;
    }
    public void GetKnotForceSampleLocations(IList<KnotForceSampleLocation> knotForceSampleLocations) {
        knotForceSampleLocations.Clear();
        foreach (var location in this.knotForceSampleLocations) {
            knotForceSampleLocations.Add(location);
        }
    }
    public void SetKnotForceSampleLocations(IList<KnotForceSampleLocation> knotForceSampleLocations) {
        if (this.knotForceSampleLocations == null) {
            this.knotForceSampleLocations = new List<KnotForceSampleLocation>(knotForceSampleLocations);
            return;
        }
        this.knotForceSampleLocations.Clear();
        this.knotForceSampleLocations.AddRange(knotForceSampleLocations);
    }
    public float GetPenetrableFriction() => penetrableFriction;
    public void SetPenetrableFriction(float newFriction) {
        penetrableFriction = newFriction;
    }

    public void SetTruncateNormalizedDistance(float truncateNormalizedDistance) {
        this.truncateNormalizedDistance = truncateNormalizedDistance;
    }
    public float GetTruncateNormalizedDistance() => truncateNormalizedDistance;
    public void SetHoleStartNormalizedDistance(float holeStartNormalizedDistance) {
        this.holeStartNormalizedDistance = holeStartNormalizedDistance;
    }
    public float GetHoleStartNormalizedDistance() => holeStartNormalizedDistance;

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

    public override PenetrationResult GetPenetrationResult(Penetrator penetrator, Penetrator.PenetrationArgs penetrationArgs ) {
        base.GetPenetrationResult(penetrator, penetrationArgs);
        
        float holeStartDepth = PenetrableNormalizedDistanceSpaceToWorldDistance(holeStartNormalizedDistance, penetrationArgs);
        
        float knotForce = 0f;
        foreach (var knotForceSampleLocation in knotForceSampleLocations) {
            float worldKnotForceSampleLocationDistance = 
                PenetrableNormalizedDistanceSpaceToWorldDistance(knotForceSampleLocation.normalizedDistance, penetrationArgs);
            knotForce += penetrator.GetKnotForce(penetrationArgs.baseToPenetrationLength + worldKnotForceSampleLocationDistance);
        }

        bool tipIsInside = !(shouldClip && clippingRange.allowAllTheWayThrough && penetrationArgs.penetrationDepth > PenetrableNormalizedDistanceSpaceToWorldDistance( clippingRange.endNormalizedDistance, penetrationArgs));

        float worldClipStartDistance =
            PenetrableNormalizedDistanceSpaceToWorldDistance(clippingRange.startNormalizedDistance, penetrationArgs);
        float worldClipEndDistance =
            PenetrableNormalizedDistanceSpaceToWorldDistance(clippingRange.endNormalizedDistance, penetrationArgs);
        float worldTruncateDistance =
            PenetrableNormalizedDistanceSpaceToWorldDistance(truncateNormalizedDistance, penetrationArgs);
        float girthAtWorldTruncateDistance = penetrator.GetWorldGirthRadius(penetrationArgs.baseToPenetrationLength + worldTruncateDistance);
        return new PenetrationResult {
            penetrable = this,
            knotForce = knotForce,
            penetrableFriction = penetrableFriction,
            clippingRange = !shouldClip ? null : new ClippingRangeWorld {
                startDistance = penetrationArgs.baseToPenetrationLength + worldClipStartDistance,
                endDistance = clippingRange.allowAllTheWayThrough ? penetrationArgs.baseToPenetrationLength + worldClipEndDistance : null,
            },
            tipIsInside = tipIsInside,
            holeStartDepth = holeStartDepth,
            truncation = !shouldTruncate ? null : new Truncation {
                girth = girthAtWorldTruncateDistance,
                length = penetrationArgs.baseToPenetrationLength + worldTruncateDistance,
            },
        }; // TODO: MAKE THIS USE A CONSTRUCTOR ???
    }

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();
        if (transforms == null || transforms.Count <= 1) {
            return;
        }

        foreach (var t in transforms) {
            if (t == null) {
                return;
            }
        }
        cachedSpline ??= new CatmullSpline(GetPoints());
        cachedSpline.SetWeightsFromPoints(GetPoints());
        var lastColor = Gizmos.color;
        float arcLength = cachedSpline.arcLength;

        if (shouldClip) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(cachedSpline.GetPositionFromDistance(clippingRange.startNormalizedDistance * arcLength), 0.025f);
            if (clippingRange.allowAllTheWayThrough) {
                Gizmos.DrawWireSphere(cachedSpline.GetPositionFromDistance(clippingRange.endNormalizedDistance * arcLength), 0.025f);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cachedSpline.GetPositionFromDistance(truncateNormalizedDistance*arcLength), 0.025f);
        Gizmos.color = lastColor;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(cachedSpline.GetPositionFromDistance(holeStartNormalizedDistance*arcLength), 0.025f);
        Gizmos.color = Color.magenta;
        if (knotForceSampleLocations != null) {
            foreach (var knotLocation in knotForceSampleLocations) {
                Gizmos.DrawWireSphere(cachedSpline.GetPositionFromDistance(knotLocation.normalizedDistance * arcLength), 0.025f);
            }
        }
        Gizmos.color = lastColor;
    }

    public override void GetHole(out Vector3 holePosition, out Vector3 holeNormal) {
        cachedSpline ??= new CatmullSpline(GetPoints());
        cachedSpline.SetWeightsFromPoints(GetPoints());
        holePosition = cachedSpline.GetPositionFromDistance(holeStartNormalizedDistance * cachedSpline.arcLength);
        holeNormal = cachedSpline.GetVelocityFromDistance(holeStartNormalizedDistance * cachedSpline.arcLength).normalized;
    }
}

}