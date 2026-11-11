using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames {
public class CurveOfTransforms : MonoBehaviour {

    [SerializeField] private Transform[] transforms;
    private Spline spline = new Spline();

    public Spline GetSpline() {
        Vector3[] points = new Vector3[transforms.Length];
        for (int i = 0; i < transforms.Length; i++) {
            points[i] = transforms[i].position;
        }
        return spline.SetFromCatmull(points, false);
    }

    public void GetSplineWithPrependedTransforms(Transform[] prependedTransforms, Spline spline) {
        var points = new Vector3[prependedTransforms.Length + transforms.Length];
        var index = 0;
        for (int i = 0; i < prependedTransforms.Length; i++) {
            points[index] = prependedTransforms[i].position;
            index++;
        }

        for (int i = 0; i < transforms.Length; i++) {
            points[index] = transforms[i].position;
            index++;
        }

        spline.SetFromCatmull(points, false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (transforms == null || transforms.Length < 2) return;
        GetSpline().GizmoDraw(Color.green, Color.yellow);
    }
#endif

}

}
