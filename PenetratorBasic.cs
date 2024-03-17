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

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PenetratorBasic))]
public class PenetratorBasicInspector : PenetratorInspector { }
#endif

[ExecuteAlways]
public class PenetratorBasic : Penetrator {
    [SerializeField] private Transform[] transforms;
    [SerializeField] protected Penetrable linkedPenetrable;
    
    private List<Vector3> points = new();
    
    protected override IList<Vector3> GetPoints() {
        points.Clear();
        if (transforms == null) return points;
        foreach (var t in transforms) {
            if (t == null) {
                return points;
            }
            points.Add(t.position);
        }
        if (linkedPenetrable != null) {
            var linkedPoints = new List<Vector3>();
            linkedPoints.AddRange(linkedPenetrable.GetPoints());
            GetSpline(linkedPenetrable.GetPoints(), ref cachedSpline, out var baseDistanceAlongSpline);
            
            var proximity = cachedSpline.GetLengthFromSubsection(1, 1);
            var tipProximity = proximity - GetSquashStretchedWorldLength();
            linkedPenetrable.SetPenetrated(this, new PenetrationArgs(penetratorData, proximity, cachedSpline, 2));
            LerpPoints(points, points, linkedPoints, 1f-Mathf.Clamp01(tipProximity/0.2f));
        }
        return points;
    }
    
}

}