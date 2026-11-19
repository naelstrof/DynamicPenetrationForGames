using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames {
public class PenetrableBehavior : MonoBehaviour {

    [SerializeField] private Transform[] transforms;
    [SerializeField] private Renderer[] renderers;
    
    private Vector3[] points;
    
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

    public Renderer[] GetRenderers() => renderers;

#if UNITY_EDITOR
    private Spline3D _debugSpline3D = new Spline3D();
    private void OnDrawGizmos() {
        if (transforms == null || transforms.Length < 2) return;
        _debugSpline3D ??= new Spline3D();
        _debugSpline3D.SetFromCatmullPositions(GetPenetrable().points);
        _debugSpline3D.GizmoDraw(Color.green, Color.yellow);
    }
#endif

}

}
