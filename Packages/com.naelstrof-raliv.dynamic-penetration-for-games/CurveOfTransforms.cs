using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CurveOfTransforms : MonoBehaviour {
    
    [SerializeField] private Transform[] transforms;

    public CatmullSpline GetCatmullSpline() {
        Vector3[] points = new Vector3[transforms.Length];
        for (int i = 0; i < transforms.Length; i++) {
            points[i] = transforms[i].position;
        }
        return new CatmullSpline(points);
    }

    public CatmullSpline GetCatmullSplineWithPrependedTransforms(Transform[] prependedTransforms) {
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
        return new CatmullSpline(points);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (transforms == null || transforms.Length < 2) return;
        CatmullSpline spline = GetCatmullSpline();
        int resolution = 20;
        Vector3 previousPoint = spline.GetPositionFromT(0f);
        for (int i = 1; i <= resolution; i++) {
            float t = (float)i / resolution;
            Vector3 currentPoint = spline.GetPositionFromT(t);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
#endif

}
