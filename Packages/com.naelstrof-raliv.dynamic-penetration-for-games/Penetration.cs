using System.Collections.Generic;
using UnityEngine;

namespace GatorDragonGames {
public struct Penetration {
    public Spline spline;
    public float penetrableT;
    public float tipT;
    public Penetration(Penetrator penetrator, Penetrable penetrable, List<Vector3> cachedPoints) {
        cachedPoints.Clear();
        cachedPoints.Add(penetrator.position);
        cachedPoints.AddRange(penetrable.points);
        spline = new Spline();
        spline.SetFromCatmullPositions(cachedPoints, penetrator.position-penetrator.direction, cachedPoints[^1]-cachedPoints[^2]+cachedPoints[^2]);
        penetrableT = 1f/cachedPoints.Count;
        tipT = spline.GetTimeFromDistance(penetrator.penetratorLength);
    }

    public void Render(BoneChainOnCurve boneChain, float lengthMultiplier) {
        boneChain.AlignToSpline(spline, lengthMultiplier);
    }
}
}