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

    private Spline3D _spline3D;

    private void OnDrawGizmos() {
        UpdateSpline();
        _spline3D.GizmoDraw(Color.green, Color.yellow, 256);
        var splineSpace = _spline3D.GetSplineSpace(Vector3.up);
        Gizmos.matrix = splineSpace.GetReferenceFrameFromT(ref _spline3D, preview, Vector3.up);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.1f);
    }

    private void UpdateSpline() {
        if (knots == null || knots.Length < 2) return;
        foreach(var knot in knots) {
            if (knot.knot == null) return;
            if (knot.frontHandle == null) return;
            if (knot.backHandle == null) return;
        }
        switch(splineType) {
            case SplineType.CatmullRom:
                _spline3D.SetFromCatmullPositions(Array.ConvertAll(knots, t => t.knot.position), looped);
                break;
            case SplineType.Bezier:
                _spline3D.SetFromBezierKnots(Array.ConvertAll(knots, t => new Spline3D.BezierKnot() { position = t.knot.position, frontHandle = t.frontHandle.position, backHandle = t.backHandle.position }));
                break;
            case SplineType.Hermite:
                _spline3D.SetFromHermiteKnots(Array.ConvertAll(knots, t => new Spline3D.HermiteKnot() { position = t.knot.position, velocity = t.frontHandle.position - t.knot.position }));
                break;
            case SplineType.BSpline:
                _spline3D.SetFromBSplinePositions(Array.ConvertAll(knots, t => t.knot.position), looped);
                break;
            case SplineType.Cardinal:
                _spline3D.SetFromCardinalPositions(Array.ConvertAll(knots, t => t.knot.position), tension, looped);
                break;
            case SplineType.CatmullRomWithExplicitHandles:
                _spline3D.SetFromCatmullPositions(Array.ConvertAll(knots, t => t.knot.position), knots[0].backHandle.position, knots[^1].frontHandle.position);
                break;
            case SplineType.BSplineWithExplicitHandles:
                _spline3D.SetFromBSplinePositions(Array.ConvertAll(knots, t => t.knot.position), knots[0].backHandle.position, knots[^1].frontHandle.position);
                break;
            case SplineType.CardinalWithExplicitHandles:
                _spline3D.SetFromCardinalPositions(Array.ConvertAll(knots, t => t.knot.position), knots[0].backHandle.position, knots[^1].frontHandle.position, tension);
                break;
        }
    }

    private void OnValidate() {
        UpdateSpline();
    }
}