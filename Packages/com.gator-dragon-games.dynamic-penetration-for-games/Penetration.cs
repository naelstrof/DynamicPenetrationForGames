using System.Collections.Generic;
using UnityEngine;

namespace GatorDragonGames.DPG {
public struct Penetration {
    public Spline3D spline3D;
    public float penetrableT;
    public float tipT;
    public Penetration(Penetrator penetrator, Penetrable penetrable, List<Vector3> cachedPoints, Spline3D spline3D) {
        cachedPoints.Clear();
        cachedPoints.Add(penetrator.position);
        cachedPoints.AddRange(penetrable.points);
        this.spline3D = spline3D;
        this.spline3D.SetFromCatmullPositions(cachedPoints, penetrator.position-penetrator.direction, cachedPoints[^1]-cachedPoints[^2]+cachedPoints[^2]);
        penetrableT = 1f/(cachedPoints.Count-1);
        tipT = spline3D.GetTimeFromDistance(penetrator.penetratorLength);
    }

    public void Render(BoneChainOnCurve boneChain, float lengthMultiplier) {
        boneChain.AlignToSpline(spline3D, lengthMultiplier);
    }
}
}