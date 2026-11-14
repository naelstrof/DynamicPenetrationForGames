using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames {
public struct Penetrable {
    public Vector3[] points;
}

public struct Penetrator {
    public Vector3 position;
    public Vector3 direction;
    public float penetratorLength;
}

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
public class PenetrableBehavior : MonoBehaviour {

    [SerializeField] private Transform[] transforms;
    [SerializeField] private Renderer[] renderers;
    
    private SphereCasterRenderer[] sphereCasterRenderers;

    private Vector3[] points;

    void OnEnable() {
        var rendererCount = renderers.Length;
        sphereCasterRenderers = new SphereCasterRenderer[rendererCount];
        for (int i = 0; i < rendererCount; i++) {
            if (renderers[i].TryGetComponent(out SphereCasterRenderer sphereCasterRenderer)) {
                sphereCasterRenderers[i] = sphereCasterRenderer;
                continue;
            }
            sphereCasterRenderers[i] = renderers[i].GetComponent<SphereCasterRenderer>();
        }
    }
    
    public Penetrable GetPenetrable() {
        if (points == null || points.Length != transforms.Length) {
            points = new Vector3[transforms.Length];
        }
        Penetrable penetrable = new Penetrable() {
            points = points
        };
        if (transforms == null || transforms.Length < 2) return penetrable;
        for (int i = 0; i < transforms.Length; i++) {
            penetrable.points[i] = transforms[i].position;
        }
        return penetrable;
    }

    public void AddSphereCasts(SphereCasterRenderer.SphereCast[] newSphereCasts) {
        foreach (var sphereCasterRenderer in sphereCasterRenderers) {
            sphereCasterRenderer.AddSphereCasts(newSphereCasts);
        }
    }

#if UNITY_EDITOR
    private Spline debugSpline = new Spline();
    private void OnDrawGizmos() {
        if (transforms == null || transforms.Length < 2) return;
        debugSpline ??= new Spline();
        debugSpline.SetFromCatmullPositions(GetPenetrable().points);
        debugSpline.GizmoDraw(Color.green, Color.yellow);
    }
#endif

}

}
