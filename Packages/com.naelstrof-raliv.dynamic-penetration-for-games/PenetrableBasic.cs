using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenetrableBasic : Penetrable {
    [SerializeField]
    private Transform[] transforms;
    private List<Vector3> points = new();
    
    public override IList<Vector3> GetPoints() {
        points.Clear();
        if (transforms == null) return points;
        foreach (var t in transforms) {
            if (t == null) {
                return points;
            }
            points.Add(t.position);
        }
        return points;
    }
}
