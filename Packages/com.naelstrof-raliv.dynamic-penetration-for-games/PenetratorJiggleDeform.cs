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
    
    [SerializeField] protected Penetrable linkedPenetrable;
    
    private List<Transform> simulatedPoints;
    private List<Vector3> points = new();
    private JiggleRigBuilder builder;
    private JiggleRigBuilder.JiggleRig rig;
    private float lastInsertionAmount = 0f;
    protected override void OnEnable() {
        base.OnEnable();
        if (!Application.isPlaying) return;
        simulatedPoints = new List<Transform>();
        for (int i = 0; i < simulatedPointCount; i++) {
            var simulatedPointObj = new GameObject($"PenetratorJiggle{i}");
            simulatedPointObj.transform.SetParent(i == 0 ? GetRootTransform().parent : simulatedPoints[^1]);
            simulatedPoints.Add(simulatedPointObj.transform);
        }
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
        builder = gameObject.AddComponent<JiggleRigBuilder>();
        rig = new JiggleRigBuilder.JiggleRig(simulatedPoints[0], jiggleSettings, new Transform[] { }, new Collider[] { });
        builder.jiggleRigs = new List<JiggleRigBuilder.JiggleRig> { rig };
        builder.enabled = false;
    }

    protected override void LateUpdate() {
        if (simulatedPoints == null || simulatedPoints.Count == 0) {
            GetSpline(new Vector3[] {}, out var fakeSpline, out float fakeDistanceAlongSpline);
            penetratorRenderers.Update(
                fakeSpline,
                GetWorldLength(),
                squashAndStretch,
                GetWorldLength() * 1.1f,
                fakeDistanceAlongSpline,
                GetRootTransform(),
                GetRootForward(),
                GetRootRight(),
                GetRootUp(),
                data.truncationLength,
                data.clippingRange.startDistance,
                data.clippingRange.endDistance,
                data.truncationGirth
            );
            return;
        }
        if (Application.isPlaying) {
            builder.Advance(Time.deltaTime);
        }
        var jigglePoints = GetPoints();

        float penetrationDepth = 0;
        float insertionAmount = 0;
        int penetrableStartIndex = 0;
        
        if (linkedPenetrable != null) {
            GetPenetrableSplineInfo(out penetrationDepth, out penetrableStartIndex, out insertionAmount);
            jigglePoints = LerpPoints(jigglePoints, linkedPenetrable.GetPoints(), insertionAmount);
        }
        
        GetSpline(jigglePoints, out var finalizedSpline, out float distanceAlongSpline);
        
        simulatedPoints[0].localScale = GetRootTransform().localScale;
        if (linkedPenetrable != null) {
            if (insertionAmount >= 1f) {
                for (int i = 0; i < simulatedPointCount; i++) {
                    float progress = (float)i / (simulatedPointCount - 1);
                    float totalDistance = progress * GetWorldLength();
                    simulatedPoints[i].position = finalizedSpline.GetPositionFromDistance(totalDistance);
                }
                rig.SampleAndReset();
                var newData = linkedPenetrable.SetPenetrated(this, penetrationDepth, finalizedSpline, penetrableStartIndex);
                SetPenetrationData(newData);
            } else if (lastInsertionAmount >= 1f) {
                linkedPenetrable.SetUnpenetrated(this);
                SetPenetrationData(new Penetrable.PenetrationData() {
                    truncationLength = 999f // TODO: THIS SHOULD BE A CONSTRUCTOR
                });
            }
        }

        float penetrableDistance = insertionAmount < 1f ? GetWorldLength() + 0.1f : finalizedSpline.GetDistanceFromSubT(1, penetrableStartIndex, 1f);
        penetratorRenderers.Update(
            finalizedSpline,
            GetWorldLength(),
            squashAndStretch,
            penetrableDistance,
            distanceAlongSpline,
            GetRootTransform(),
            GetRootForward(),
            GetRootRight(),
            GetRootUp(),
            data.truncationLength,
            data.clippingRange.startDistance,
            data.clippingRange.endDistance,
            data.truncationGirth
        );
        lastInsertionAmount = insertionAmount;
    }
    
    protected virtual void GetPenetrableSplineInfo(out float penetrationDepth, out int penetrableStartIndex, out float insertionAmount) {
        GetSpline(linkedPenetrable.GetPoints(), out var linkedSpline, out var baseDistanceAlongSpline);
        var proximity = linkedSpline.GetDistanceFromSubT(1, 2, 1f);
        var tipProximity = proximity - GetWorldLength();
        penetrationDepth = -tipProximity;
        penetrableStartIndex = 2;
        insertionAmount = 1f - Mathf.Clamp01(tipProximity / 0.2f);
    }

    public void SetLinkedPenetrable(Penetrable penetrable) {
        if (penetrable == null) {
            penetrable.SetUnpenetrated(this);
        }
        linkedPenetrable = penetrable;
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
        return points;
    }

    public void SetCurvature(Vector2 curvature) {
        if (!Application.isPlaying) return;
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return;
        
        leftRightCurvature = curvature.x;
        upDownCurvature = curvature.y;

        Vector3 scaleMemory = GetRootTransform().localScale;
        simulatedPoints[0].localScale = GetRootTransform().localScale = Vector3.one;
        Vector2 segmentCurvature = curvature / Mathf.Max(simulatedPointCount - 1, 1f);
        for (int i = 0; i < simulatedPointCount; i++) {
            if (i == 0) {
                simulatedPoints[i].transform.rotation = Quaternion.AngleAxis(segmentCurvature.x, GetRootUp()) * Quaternion.AngleAxis(segmentCurvature.y, GetRootRight()) *
                    Quaternion.LookRotation( GetRootTransform().TransformDirection(GetRootForward()), GetRootTransform().TransformDirection(GetRootUp()));
                simulatedPoints[i].transform.position = GetRootTransform().TransformPoint(GetRootPositionOffset());
            } else {
                simulatedPoints[i].transform.localRotation = Quaternion.Euler(segmentCurvature.y, segmentCurvature.x, 0f);
                float localLength = GetRootTransform().InverseTransformVector(GetWorldLength() * GetRootForward()).magnitude;
                float moveAmount = 1f/(simulatedPointCount-1) * localLength;
                simulatedPoints[i].transform.localPosition = Vector3.forward * moveAmount;
            }
        }
        simulatedPoints[0].localScale = GetRootTransform().localScale = scaleMemory;
    }

    protected override void OnValidate() {
        base.OnValidate();
        if (!Application.isPlaying) return;
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return;
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
    }
}
