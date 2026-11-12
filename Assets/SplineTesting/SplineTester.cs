using System;
using GatorDragonGames;
using UnityEngine;

public class SplineTester : MonoBehaviour {
    private enum SplineType {
        CatmullRom,
        Bezier,
        Hermite,
        BSpline,
        Cardinal,
        CatmullRomWithExplicitHandles,
        BSplineWithExplicitHandles,
        CardinalWithExplicitHandles
    }
    
    [SerializeField] private SplineType splineType = SplineType.CatmullRom;
    [SerializeField, Range(-1f,1f)] private float tension = 0f;
    [SerializeField, Range(-1f,2f)] private float preview = 0f;
    [SerializeField] private bool looped = false;
    
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
        Gizmos.matrix = spline.GetReferenceFrameFromT(preview);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.1f);
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
            case SplineType.CatmullRom:
                spline.SetFromCatmullPositions(Array.ConvertAll(knots, t => t.knot.position), looped);
                break;
            case SplineType.Bezier:
                spline.SetFromBezierKnots(Array.ConvertAll(knots, t => new Spline.BezierKnot() { position = t.knot.position, frontHandle = t.frontHandle.position, backHandle = t.backHandle.position }));
                break;
            case SplineType.Hermite:
                spline.SetFromHermiteKnots(Array.ConvertAll(knots, t => new Spline.HermiteKnot() { position = t.knot.position, velocity = t.frontHandle.position - t.knot.position }));
                break;
            case SplineType.BSpline:
                spline.SetFromBSplinePositions(Array.ConvertAll(knots, t => t.knot.position), looped);
                break;
            case SplineType.Cardinal:
                spline.SetFromCardinalPositions(Array.ConvertAll(knots, t => t.knot.position), tension, looped);
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