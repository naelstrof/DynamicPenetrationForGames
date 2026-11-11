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
    }
    
    [SerializeField] private SplineType splineType = SplineType.CatmullRom;
    [SerializeField, Range(-1f,1f)] private float tension = 0f;
    [SerializeField] private bool loop = false;

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
            case SplineType.CatmullRom:
                spline.SetFromCatmull(Array.ConvertAll(knots, t => t.knot.position), loop);
                break;
            case SplineType.Bezier:
                spline.SetFromSplineKnotsBezier(Array.ConvertAll(knots, t => new Spline.BezierKnot() { position = t.knot.position, frontHandle = t.frontHandle.position, backHandle = t.backHandle.position }));
                break;
            case SplineType.Hermite:
                spline.SetFromSplineKnotsHermite(Array.ConvertAll(knots, t => new Spline.HermiteKnot() { position = t.knot.position, velocity = t.frontHandle.position - t.knot.position }));
                break;
            case SplineType.BSpline:
                spline.SetFromBSpline(Array.ConvertAll(knots, t => t.knot.position), loop);
                break;
            case SplineType.Cardinal:
                spline.SetFromCardinal(Array.ConvertAll(knots, t => t.knot.position), loop, tension);
                break;
        }
    }

    private void OnValidate() {
        UpdateSpline();
    }
}