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

    public struct PenetrationData {
        public float knotForce;
        public bool tipIsInside;
        public float stimulation;
        public float truncationGirth;
        public float truncationLength;
        public PenetrableBasic.ClippingRangeWorld clippingRange;
    }
    
    public virtual PenetrationData SetPenetrated(Penetrator penetrator, float penetrationDepth, CatmullSpline alongSpline, int penetrableStartIndex) {
        return new PenetrationData();
    }

    public virtual void SetUnpenetrated(Penetrator penetrator) {
    }
}
