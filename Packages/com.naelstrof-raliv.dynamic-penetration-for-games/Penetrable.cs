using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Penetrable : MonoBehaviour {
    public abstract IList<Vector3> GetPoints();
    protected virtual void OnDrawGizmosSelected() {
        if (GetPoints().Count <= 1) {
            return;
        }
        var spline = new CatmullSpline(GetPoints());
        CatmullSpline.GizmosDrawSpline(spline, Color.red, Color.green);
    }

    public delegate void PenetrationAction(Penetrable self, Penetrator penetrator, float penetrationDepth, CatmullSpline alongSpline, int penetrableStartIndex);
    public delegate void UnpenetrateAction(Penetrable self, Penetrator penetrator);
    public event PenetrationAction penetrated;
    public event UnpenetrateAction unpenetrated;

    public struct PenetrationData {
        public float knotForce;
        public bool tipIsInside;
        public float stimulation;
        public float truncationGirth;
        public float truncationLength;
        public PenetrableBasic.ClippingRangeWorld clippingRange;
    }
    
    public virtual PenetrationData SetPenetrated(Penetrator penetrator, float penetrationDepth, CatmullSpline alongSpline, int penetrableStartIndex) {
        penetrated?.Invoke(this, penetrator, penetrationDepth, alongSpline, penetrableStartIndex);
        return new PenetrationData();
    }

    public virtual void SetUnpenetrated(Penetrator penetrator) {
        unpenetrated?.Invoke(this, penetrator);
    }
}
