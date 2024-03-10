using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Penetrable : MonoBehaviour {
    public abstract IList<Vector3> GetPoints();
    protected virtual void OnDrawGizmos() {
        if (GetPoints().Count <= 1) {
            return;
        }
        var spline = new CatmullSpline(GetPoints());
        Gizmos.color = Color.red;
        Vector3 lastPoint = spline.GetPositionFromT(0f);
        for (int i = 0; i <= 64; i++) {
            Vector3 newPoint = spline.GetPositionFromT((float)i / 64f);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }

        var save = Gizmos.matrix;
        foreach(var weight in spline.GetWeights()) {
            Vector3 pointA = CatmullSpline.GetPosition(weight, 0f);
            Vector3 normalA = CatmullSpline.GetVelocity(weight, 0f);
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(pointA, Quaternion.FromToRotation(Vector3.forward, normalA.normalized), Vector3.one - Vector3.forward * 0.8f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one*0.025f);
            if (spline.GetWeights().IndexOf(weight) == spline.GetWeights().Count - 1) {
                Vector3 pointB = CatmullSpline.GetPosition(weight, 1f);
                Vector3 normalB = CatmullSpline.GetVelocity(weight, 1f);
                Gizmos.matrix = Matrix4x4.TRS(pointB, Quaternion.FromToRotation(Vector3.forward, normalB.normalized), Vector3.one - Vector3.forward * 0.8f);
                Gizmos.DrawCube(Vector3.zero, Vector3.one*0.025f);
            }
        }
        Gizmos.matrix = save;
    }
}
