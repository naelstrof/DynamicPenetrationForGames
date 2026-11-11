using System;
using GatorDragonGames;
using UnityEngine;

public class SplineTester : MonoBehaviour {
    private enum SplineType {
        CentripetalCatmullRom,
        CentripetalCatmullRomCustomHandles,
        Bezier,
    }
    
    [SerializeField] private SplineType splineType = SplineType.CentripetalCatmullRom;
    [SerializeField, Range(0f,1f)] private float alpha = 0.5f;
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
        foreach(var knot in knots) {
            if (knot.knot == null) return;
            if (knot.frontHandle == null) return;
            if (knot.backHandle == null) return;
        }
        switch(splineType) {
            case SplineType.CentripetalCatmullRom:
                if (loop) {
                    spline.SetFromCentripetalCatmullLooped(Array.ConvertAll(knots, t => t.knot.position), alpha);
                } else {
                    spline.SetFromCentripetalCatmull(Array.ConvertAll(knots, t => t.knot.position), alpha);
                }
                break;
            case SplineType.CentripetalCatmullRomCustomHandles:
                spline.SetFromCentripetalCatmullWithCustomHandles(Array.ConvertAll(knots, t => t.knot.position), alpha);
                break;
            case SplineType.Bezier:
                spline.SetFromSplineKnotsBezier(Array.ConvertAll(knots, t => new Spline.SplineKnot() { position = t.knot.position, frontHandle = t.frontHandle.position, backHandle = t.backHandle.position }));
                break;
        }
    }

    private void OnValidate() {
        UpdateSpline();
    }
}