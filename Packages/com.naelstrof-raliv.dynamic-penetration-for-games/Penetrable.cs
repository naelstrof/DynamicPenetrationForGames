/* Copyright 2024 Naelstrof & Raliv
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 * documentation files (the “Software”), to deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
 * Software.
 * 
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
 * OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace DPG {

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

    public abstract void GetHole(out Vector3 holePosition, out Vector3 holeNormal);

    public delegate void PenetrationAction(Penetrable penetrable, Penetrator penetrator, Penetrator.PenetrationArgs penetrationArgs);
    public delegate void UnpenetrateAction(Penetrable penetrable, Penetrator penetrator);
    public event PenetrationAction penetrated;
    public event UnpenetrateAction unpenetrated;
    
    public struct ClippingRangeWorld {
        public float startDistance;
        public float? endDistance;
    }
    
    public struct Truncation {
        public float girth;
        public float length;
    }

    public struct PenetrationResult {
        public Penetrable penetrable;
        public float knotForce;
        public float penetrableFriction;
        public float holeStartDepth;
        public bool tipIsInside;
        public Truncation? truncation;
        public ClippingRangeWorld? clippingRange;
    }
    
    public virtual PenetrationResult SetPenetrated(Penetrator penetrator, Penetrator.PenetrationArgs penetrationArgs) {
        penetrated?.Invoke(this, penetrator, penetrationArgs);
        return new PenetrationResult();
    }

    public virtual void SetUnpenetrated(Penetrator penetrator) {
        unpenetrated?.Invoke(this, penetrator);
    }
}

}