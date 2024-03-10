using System;
using System.Collections;
using System.Collections.Generic;
using JigglePhysics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class PenetratorJiggleDeformInspector : PenetratorInspector { }

#endif

public class PenetratorJiggleDeform : Penetrator {
    [SerializeField] private int simulatedPointCount = 3;
    [SerializeField] private JiggleSettings jiggleSettings;
    private List<Transform> simulatedPoints;
    private List<Vector3> points = new();
    private JiggleRigBuilder builder;
    protected override void OnEnable() {
        base.OnEnable();
        simulatedPoints = new List<Transform>();
        for (int i = 0; i < simulatedPointCount; i++) {
            var simulatedPointObj = new GameObject($"PenetratorJiggle{i}");
            simulatedPointObj.transform.SetParent(i == 0 ? penetratorData.GetRootTransform() : simulatedPoints[^1]);
            float progress = (float)i / (simulatedPointCount-1);
            float moveAmount = progress * penetratorData.GetPenetratorWorldLength();
            simulatedPointObj.transform.position = penetratorData.GetRootTransform().TransformPoint(penetratorData.GetRootPositionOffset()+penetratorData.GetRootForward()*moveAmount);
            simulatedPoints.Add(simulatedPointObj.transform);
        }
        builder = gameObject.AddComponent<JiggleRigBuilder>();
        var rig = new JiggleRigBuilder.JiggleRig(simulatedPoints[0], jiggleSettings, new Transform[]{}, new Collider[]{} );
        builder.jiggleRigs = new List<JiggleRigBuilder.JiggleRig>{rig};
    }

    protected override void OnDisable() {
        Destroy(builder);
        foreach (var t in simulatedPoints) {
            Destroy(t.gameObject);
        }
        simulatedPoints = null;
        base.OnDisable();
    }

    protected override IList<Vector3> GetPoints() {
        points.Clear();
        if (simulatedPoints == null) return points;
        foreach (var t in simulatedPoints) {
            points.Add(t.position);
        }
        return points;
    }
}
