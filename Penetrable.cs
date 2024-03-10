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
        CatmullSpline.GizmosDrawSpline(spline, Color.red, Color.green);
    }
}
