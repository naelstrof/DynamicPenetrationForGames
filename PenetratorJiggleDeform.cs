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
    [SerializeField, Range(-90f, 90f)] private float leftRightCurvature = 0f;
    [SerializeField, Range(-90f, 90f)] private float upDownCurvature = 0f;
    
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
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
        builder = gameObject.AddComponent<JiggleRigBuilder>();
        rig = new JiggleRigBuilder.JiggleRig(simulatedPoints[0], jiggleSettings, new Transform[] { }, new Collider[] { });
        builder.jiggleRigs = new List<JiggleRigBuilder.JiggleRig> { rig };
    }

    protected void Update() {
        if (simulatedPoints == null || simulatedPoints.Count == 0) {
            return;
        }
        simulatedPoints[0].localScale = penetratorData.GetRootTransform().localScale;
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
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
        
        leftRightCurvature = curvature.x;
        upDownCurvature = curvature.y;

        Vector3 scaleMemory = penetratorData.GetRootTransform().localScale;
        simulatedPoints[0].localScale = penetratorData.GetRootTransform().localScale = Vector3.one;
        for (int i = 0; i < simulatedPointCount; i++) {
            if (i == 0) {
                simulatedPoints[i].transform.rotation = Quaternion.LookRotation(
                    penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootForward()),
                    penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootUp()));
                simulatedPoints[i].transform.position = penetratorData.GetRootTransform().TransformPoint(penetratorData.GetRootPositionOffset());
            } else {
                Vector2 segmentCurvature = curvature / Mathf.Max(simulatedPointCount - 2, 1f);
                simulatedPoints[i].transform.localRotation = Quaternion.Euler(segmentCurvature.y, segmentCurvature.x, 0f);
                float localLength = penetratorData.GetRootTransform().InverseTransformVector(penetratorData.GetPenetratorWorldLength() * penetratorData.GetRootForward()).magnitude;
                float moveAmount = 1f/(simulatedPointCount-1) * localLength;
                simulatedPoints[i].transform.localPosition = Vector3.forward * moveAmount;
            }
        }
        simulatedPoints[0].localScale = penetratorData.GetRootTransform().localScale = scaleMemory;
    }

    protected override void OnValidate() {
        base.OnValidate();
        if (!Application.isPlaying) return;
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return;
        if (rig == null) return;
    }
}
