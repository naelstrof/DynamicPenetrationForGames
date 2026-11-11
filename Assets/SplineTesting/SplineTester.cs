using System;
using GatorDragonGames;
using UnityEngine;

public class SplineTester : MonoBehaviour {
    private enum SplineType {
        CatmullRomLoop,
        Bezier,
        Hermite,
        BSplineLoop,
        CardinalLoop,
        CatmullRomWithExplicitHandles,
        BSplineWithExplicitHandles,
        CardinalWithExplicitHandles
    }
    
    [SerializeField] private SplineType splineType = SplineType.CatmullRomLoop;
    [SerializeField, Range(-1f,1f)] private float tension = 0f;

    [System.Serializable]
    private struct SplineKnot {
        public Transform backHandle;
        public Transform knot;
        public Transform frontHandle;
    }
    [SerializeField] private SplineKnot[] knots;

    private Spline spline = new Spline();

    private void OnDrawGizmos() {
        UpdateSpline();
        spline.GizmoDraw(Color.green, Color.yellow, 256);
    }

    private void UpdateSpline() {
        if (knots == null || knots.Length < 2) return;
        spline ??= new Spline();
        foreach(var knot in knots) {
            if (knot.knot == null) return;
            if (knot.frontHandle == null) return;
            if (knot.backHandle == null) return;
        }
        switch(splineType) {
            case SplineType.CatmullRomLoop:
                spline.SetFromCatmullPositionsLooped(Array.ConvertAll(knots, t => t.knot.position));
                break;
            case SplineType.Bezier:
                spline.SetFromBezierKnots(Array.ConvertAll(knots, t => new Spline.BezierKnot() { position = t.knot.position, frontHandle = t.frontHandle.position, backHandle = t.backHandle.position }));
                break;
            case SplineType.Hermite:
                spline.SetFromHermiteKnots(Array.ConvertAll(knots, t => new Spline.HermiteKnot() { position = t.knot.position, velocity = t.frontHandle.position - t.knot.position }));
                break;
            case SplineType.BSplineLoop:
                spline.SetFromBSplinePositionsLooped(Array.ConvertAll(knots, t => t.knot.position));
                break;
            case SplineType.CardinalLoop:
                spline.SetFromCardinalPositionsLooped(Array.ConvertAll(knots, t => t.knot.position), tension);
                break;
            case SplineType.CatmullRomWithExplicitHandles:
                spline.SetFromCatmullPositions(Array.ConvertAll(knots, t => t.knot.position), knots[0].backHandle.position, knots[^1].frontHandle.position);
                break;
            case SplineType.BSplineWithExplicitHandles:
                spline.SetFromBSplinePositions(Array.ConvertAll(knots, t => t.knot.position), knots[0].backHandle.position, knots[^1].frontHandle.position);
                break;
            case SplineType.CardinalWithExplicitHandles:
                spline.SetFromCardinalPositions(Array.ConvertAll(knots, t => t.knot.position), knots[0].backHandle.position, knots[^1].frontHandle.position, tension);
                break;
        }
    }

    private void OnValidate() {
        UpdateSpline();
    }
}