using System;
using System.Collections;
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
    
    private List<Vector3> points = new();
    
    protected override IList<Vector3> GetPoints() {
        points.Clear();
        foreach (var t in transforms) {
            points.Add(t.position);
        }
        return points;
    }
}
