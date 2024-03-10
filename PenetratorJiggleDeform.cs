using System;
using System.Collections;
using System.Collections.Generic;
using JigglePhysics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PenetratorJiggleDeform))]
public class PenetratorJiggleDeformInspector : PenetratorInspector { }

#endif

[ExecuteAlways]
public class PenetratorJiggleDeform : Penetrator {
    [SerializeField, Range(1,5)] private int simulatedPointCount = 3;
    [SerializeField] private JiggleSettings jiggleSettings;
    [SerializeField] private Vector2 curvature;
    
    private List<Transform> simulatedPoints;
    private List<Vector3> points = new();
    private JiggleRigBuilder builder;
    protected override void OnEnable() {
        base.OnEnable();
        if (!Application.isPlaying) return;
        simulatedPoints = new List<Transform>();
        Vector3 accumulatingForward = penetratorData.GetRootForward();
        for (int i = 0; i < simulatedPointCount; i++) {
            var simulatedPointObj = new GameObject($"PenetratorJiggle{i}");
            simulatedPointObj.transform.SetParent(i == 0 ? penetratorData.GetRootTransform() : simulatedPoints[^1]);
            float progress = (float)i / (simulatedPointCount - 1);
            float moveAmount = progress * penetratorData.GetPenetratorWorldLength();
            simulatedPointObj.transform.position = penetratorData.GetRootTransform().TransformPoint(penetratorData.GetRootPositionOffset() + accumulatingForward * moveAmount);
            accumulatingForward = Quaternion.AngleAxis(curvature.x, penetratorData.GetRootUp()) *
                                  Quaternion.AngleAxis(curvature.y, penetratorData.GetRootRight()) *
                                  accumulatingForward;
            simulatedPoints.Add(simulatedPointObj.transform);
        }

        builder = gameObject.AddComponent<JiggleRigBuilder>();
        var rig = new JiggleRigBuilder.JiggleRig(simulatedPoints[0], jiggleSettings, new Transform[] { },
            new Collider[] { });
        builder.jiggleRigs = new List<JiggleRigBuilder.JiggleRig> { rig };
    }

    protected override void OnDisable() {
        if (Application.isPlaying) {
            Destroy(builder);
            foreach (var t in simulatedPoints) {
                Destroy(t.gameObject);
            }

            simulatedPoints = null;
        }
        base.OnDisable();
    }

    protected override IList<Vector3> GetPoints() {
        points.Clear();
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return points;
        for (int i=1;i<simulatedPoints.Count;i++) {
            points.Add(simulatedPoints[i].position);
        }
        if (linkedPenetrable != null) {
            var linkedPoints = new List<Vector3>();
            linkedPoints.AddRange(linkedPenetrable.GetPoints());
            penetratorData.GetSpline(linkedPenetrable.GetPoints(), out var linkedSpline, out var baseDistanceAlongSpline);
            var proximity = linkedSpline.GetDistanceFromSubT(1, 2, 1f);
            var tipProximity = proximity - penetratorData.GetPenetratorWorldLength();
            return LerpPoints(points, linkedPoints, 1f-Mathf.Clamp01(tipProximity/0.2f));
        }
        return points;
    }
}
