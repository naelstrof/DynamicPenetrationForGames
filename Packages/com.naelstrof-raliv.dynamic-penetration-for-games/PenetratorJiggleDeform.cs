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
    [SerializeField, Range(-20f, 20f)] private float leftRightCurvature = 0f;
    [SerializeField, Range(-20f, 20f)] private float upDownCurvature = 0f;
    
    private List<Transform> simulatedPoints;
    private List<Vector3> points = new();
    private JiggleRigBuilder builder;
    private JiggleRigBuilder.JiggleRig rig;
    protected override void OnEnable() {
        base.OnEnable();
        if (!Application.isPlaying) return;
        simulatedPoints = new List<Transform>();
        for (int i = 0; i < simulatedPointCount; i++) {
            var simulatedPointObj = new GameObject($"PenetratorJiggle{i}");
            simulatedPointObj.transform.SetParent(i == 0 ? penetratorData.GetRootTransform().parent : simulatedPoints[^1]);
            simulatedPoints.Add(simulatedPointObj.transform);
        }
        builder = gameObject.AddComponent<JiggleRigBuilder>();
        rig = new JiggleRigBuilder.JiggleRig(simulatedPoints[0], jiggleSettings, new Transform[] { }, new Collider[] { });
        builder.jiggleRigs = new List<JiggleRigBuilder.JiggleRig> { rig };
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
    }

    protected override void Update() {
        if (simulatedPoints == null || simulatedPoints.Count == 0) {
            base.Update();
            return;
        }
        simulatedPoints[0].localScale = penetratorData.GetRootTransform().localScale;
        base.Update();
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
            float insertionAmount = 1f - Mathf.Clamp01(tipProximity / 0.2f);
            if (insertionAmount >= 1f) {
                for (int i = 0; i < simulatedPointCount; i++) {
                    float progress = (float)i / (simulatedPointCount - 1);
                    float totalDistance = progress * penetratorData.GetPenetratorWorldLength();
                    simulatedPoints[i].position = linkedSpline.GetPositionFromDistance(totalDistance);
                }
                rig.SampleAndReset();
            }
            return LerpPoints(points, linkedPoints, insertionAmount);
        }
        return points;
    }

    public void SetCurvature(Vector2 curvature) {
        if (!Application.isPlaying) return;
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return;
        if (rig == null) return;
        
        leftRightCurvature = curvature.x;
        upDownCurvature = curvature.y;

        Vector3 scaleMemory = penetratorData.GetRootTransform().localScale;
        simulatedPoints[0].localScale = penetratorData.GetRootTransform().localScale = Vector3.one;
        Vector3 accumulatingForward = penetratorData.GetRootForward();
        for (int i = 0; i < simulatedPointCount; i++) {
            float progress = (float)i / (simulatedPointCount - 1);
            float moveAmount = progress * penetratorData.GetPenetratorWorldLength();
            simulatedPoints[i].transform.position = penetratorData.GetRootTransform().TransformPoint(penetratorData.GetRootPositionOffset() + accumulatingForward.normalized * moveAmount);
            accumulatingForward = Quaternion.AngleAxis(curvature.x, penetratorData.GetRootUp()) * Quaternion.AngleAxis(curvature.y, penetratorData.GetRootRight()) * accumulatingForward.normalized;
        }
        simulatedPoints[0].localScale = penetratorData.GetRootTransform().localScale = scaleMemory;
        rig.MatchAnimationInstantly();
    }

    protected override void OnValidate() {
        base.OnValidate();
        if (!Application.isPlaying) return;
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return;
        if (rig == null) return;
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
    }
}
