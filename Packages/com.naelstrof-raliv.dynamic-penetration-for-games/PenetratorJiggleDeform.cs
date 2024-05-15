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
using JigglePhysics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PenetratorJiggleDeform))]
public class PenetratorJiggleDeformInspector : PenetratorInspector {
}

#endif

[ExecuteAlways]
public class PenetratorJiggleDeform : Penetrator {
    [SerializeField, Range(1, 5)] private int simulatedPointCount = 3;
    [SerializeField] private JiggleSettingsBase jiggleSettings;
    [SerializeField, Range(-90f, 90f)] private float leftRightCurvature = 0f;
    [SerializeField, Range(-90f, 90f)] private float upDownCurvature = 0f;
    [SerializeField, Range(-90f, 90f)] private float baseUpDownCurvatureOffset = 0f;
    [SerializeField, Range(-90f, 90f)] private float baseLeftRightCurvatureOffset = 0f;
    [SerializeField, Range(0f, 1f)] private float penetratorLengthFriction = 0.5f;
    [SerializeField, Range(0f, 0.95f)] private float penetratorLengthElasticity = 0.7f;
    [SerializeField, Range(0f, 2f)] private float knotForce = 1f;

    [SerializeField] protected Penetrable linkedPenetrable;

    private List<Transform> simulatedPoints;
    private List<Vector3> points = new();
    private JiggleRigBuilder builder;
    private JiggleRigBuilder.JiggleRig rig;
    private float lastInsertionAmount = 0f;
    private List<Collider> setupColliders = new();

    private float desiredLength;
    private float desiredLengthVelocity;
    private float? lastPenetrablePosition;

    protected override void OnEnable() {
        base.OnEnable();
        if (!Application.isPlaying) return;
        if (jiggleSettings is JiggleSettingsBlend) {
            jiggleSettings = Instantiate(jiggleSettings);
        }

        simulatedPoints = new List<Transform>();
        for (int i = 0; i < simulatedPointCount; i++) {
            var simulatedPointObj = new GameObject($"PenetratorJiggle{i}");
            simulatedPointObj.transform.SetParent(i == 0 ? GetRootTransform().parent : simulatedPoints[^1]);
            simulatedPoints.Add(simulatedPointObj.transform);
        }

        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
        builder = gameObject.AddComponent<JiggleRigBuilder>();
        rig = new JiggleRigBuilder.JiggleRig(simulatedPoints[0], jiggleSettings, new Transform[] { }, setupColliders);
        builder.jiggleRigs = new List<JiggleRigBuilder.JiggleRig> { rig };
        builder.enabled = false;
        desiredLength = penetratorData.GetWorldLength();
    }

    private bool GetSimulationAvailable() => simulatedPoints != null && simulatedPoints.Count != 0;

    private void FixedUpdate() {
        if (!IsValid()) {
            return;
        }

        GetFinalizedSpline(ref cachedSpline, out var distanceAlongSpline, out var insertionLerp, out var penetrationArgs);

        desiredLengthVelocity *= 1f - (penetratorLengthFriction * penetratorLengthFriction);

        if (insertionLerp >= 1f && Time.deltaTime > 0f && penetrationArgs.HasValue) {
            float newPenetrablePosition = cachedSpline.GetLengthFromSubsection(penetrationArgs.Value.penetrableStartIndex);
            float penetrableVelocity = (newPenetrablePosition - (lastPenetrablePosition ?? newPenetrablePosition)) / Time.deltaTime;
            lastPenetrablePosition = newPenetrablePosition;

            desiredLengthVelocity = Mathf.Lerp(desiredLengthVelocity, penetrableVelocity, penetrationResult?.penetrableFriction * penetrationResult?.penetrableFriction ?? 0f);
            desiredLengthVelocity += (penetrationResult?.knotForce ?? 0f) * Time.deltaTime * knotForce * 7f;
        } else {
            lastPenetrablePosition = null;
        }

        float elasticityCalc = penetratorLengthElasticity >= 1f ? 6000f : 10f / ((1f - penetratorLengthElasticity) * (1f - penetratorLengthElasticity));
        float elasticForce = (penetratorData.GetWorldLength() - GetSquashStretchedWorldLength()) * Time.deltaTime * elasticityCalc;

        desiredLengthVelocity += elasticForce;

        desiredLength += desiredLengthVelocity * Time.deltaTime;
        float desiredSquashAndStretch = desiredLength / penetratorData.GetWorldLength();

        squashAndStretch = Mathf.Clamp(desiredSquashAndStretch, 0f, 2f);
    }

    public override void GetFinalizedSpline(ref CatmullSpline finalizedSpline, out float distanceAlongSpline, out float insertionLerp, out PenetrationArgs? penetrationArgs) {
        var jigglePoints = GetPoints();

        if (linkedPenetrable != null) {
            penetrationArgs = GetPenetrableSplineInfo(out insertionLerp);
            cachedPenetrablePoints.Clear();
            cachedPenetrablePoints.Add(GetBasePointOne());
            cachedPenetrablePoints.Add(GetBasePointTwo());
            cachedPenetrablePoints.AddRange(linkedPenetrable.GetPoints());
            LerpPoints(jigglePoints, jigglePoints, cachedPenetrablePoints, insertionLerp);
        } else {
            penetrationArgs = null;
            insertionLerp = 0f;
        }

        penetratorData.GetSpline(jigglePoints, ref finalizedSpline, out distanceAlongSpline);
    }

    protected override void LateUpdate() {
        if (!IsValid()) {
            return;
        }

        if (Application.isPlaying) {
            builder.Advance(Time.deltaTime);
        }

        if (GetSimulationAvailable()) {
            simulatedPoints[0].localScale = simulatedPoints[0].parent.InverseTransformVector(Vector3.one * GetSquashStretchedWorldLength());
        }


        GetFinalizedSpline(ref cachedSpline, out var distanceAlongSpline, out var insertionLerp, out var penetrationArgs);

        if (linkedPenetrable != null) {
            if (insertionLerp >= 1f && penetrationArgs.HasValue) {
                if (GetSimulationAvailable()) {
                    for (int i = 0; i < simulatedPointCount; i++) {
                        float progress = (float)i / (simulatedPointCount - 1);
                        float totalDistance = progress * GetSquashStretchedWorldLength();
                        simulatedPoints[i].position = cachedSpline.GetPositionFromDistance(totalDistance);
                    }

                    rig.SampleAndReset();
                }
                
                var newResult = linkedPenetrable.SetPenetrated(this, penetrationArgs.Value);
                SetPenetrationData(newResult);
                OnPenetrated(linkedPenetrable, penetrationArgs.Value, newResult);
            } else if (lastInsertionAmount >= 1f) {
                linkedPenetrable.SetUnpenetrated(this);
                SetPenetrationData(null);
                OnUnpenetrated(linkedPenetrable);
            }
        }

        float penetrableDistance = insertionLerp < 1f ? GetSquashStretchedWorldLength() + 0.1f : cachedSpline.GetLengthFromSubsection(penetrationArgs?.penetrableStartIndex - 1 ?? 1, 1);
        penetratorRenderers.Update(
            cachedSpline,
            penetratorData.GetWorldLength(),
            squashAndStretch,
            penetrableDistance + (penetrationResult?.holeStartDepth ?? 0f),
            distanceAlongSpline,
            GetRootTransform(),
            GetRootForward(),
            GetRootRight(),
            GetRootUp(),
            penetrationResult?.clippingRange,
            penetrationResult?.truncation
        );
        lastInsertionAmount = insertionLerp;
    }

    public void SetJiggleSettingsBlend(float blend) {
        if (jiggleSettings is not JiggleSettingsBlend jiggleSettingsBlend) return;
        jiggleSettingsBlend.normalizedBlend = blend;
    }

    public void AddJiggleSettingsCollider(Collider collider) {
        if (rig == null) {
            if (!setupColliders.Contains(collider)) setupColliders.Add(collider);
            return;
        }

        if (!rig.colliders.Contains(collider)) rig.colliders.Add(collider);
    }


    private List<Vector3> cachedPenetrablePoints;
    protected virtual PenetrationArgs GetPenetrableSplineInfo(out float insertionAmount) {
        cachedPenetrablePoints ??= new List<Vector3>();
        cachedPenetrablePoints.Clear();
        cachedPenetrablePoints.Add(GetBasePointOne());
        cachedPenetrablePoints.Add(GetBasePointTwo());
        cachedPenetrablePoints.AddRange(linkedPenetrable.GetPoints());
        penetratorData.GetSpline(cachedPenetrablePoints, ref cachedSpline, out var baseDistanceAlongSpline);
        var proximity = cachedSpline.GetLengthFromSubsection(1, 1);
        var tipProximity = proximity - GetSquashStretchedWorldLength();

        insertionAmount = 1f - Mathf.Clamp01(tipProximity / 0.2f);
        return new PenetrationArgs(penetratorData, -tipProximity, cachedSpline, 2);
    }

    public Penetrable GetLinkedPenetrable() => linkedPenetrable;

    public void SetLinkedPenetrable(Penetrable penetrable) {
        if (linkedPenetrable != null && penetrable != linkedPenetrable) {
            linkedPenetrable.SetUnpenetrated(this);
        }
        linkedPenetrable = penetrable;
        if (penetrable == null) {
            SetPenetrationData(null);
        }
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
        points.Add(GetBasePointOne());
        points.Add(GetBasePointTwo());
        if (!GetSimulationAvailable()) {
            points.Add(GetRootTransform().TransformPoint(GetRootPositionOffset()) + GetRootTransform().TransformDirection(GetRootForward())*GetSquashStretchedWorldLength());
            return points;
        }
        for (int i = 1; i < simulatedPoints.Count; i++) {
            points.Add(simulatedPoints[i].position);
        }

        return points;
    }

    public void SetCurvature(Vector2 curvature) {
        if (!Application.isPlaying) return;
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return;

        leftRightCurvature = curvature.x;
        upDownCurvature = curvature.y;

        Vector2 segmentCurvature = curvature / Mathf.Max(simulatedPointCount - 1, 1f);
        for (int i = 0; i < simulatedPointCount; i++) {
            if (i == 0) {
                simulatedPoints[i].localScale = Vector3.one;
                simulatedPoints[i].transform.rotation =
                    Quaternion.LookRotation(GetRootTransform().TransformDirection(GetRootForward()),
                        GetRootTransform().TransformDirection(GetRootUp())) *
                    Quaternion.Euler(segmentCurvature.y+baseUpDownCurvatureOffset, segmentCurvature.x+baseLeftRightCurvatureOffset, 0f);
                simulatedPoints[i].transform.position = GetRootTransform().TransformPoint(GetRootPositionOffset());
            } else {
                simulatedPoints[i].transform.localRotation =
                    Quaternion.Euler(segmentCurvature.y, segmentCurvature.x, 0f);
                float moveAmount = 1f / (simulatedPointCount - 1);
                simulatedPoints[i].transform.localPosition = Vector3.forward * moveAmount;
            }
        }

        simulatedPoints[0].localScale = simulatedPoints[0].parent.InverseTransformVector(Vector3.one * GetSquashStretchedWorldLength());
    }

    protected override void OnValidate() {
        base.OnValidate();
        if (!Application.isPlaying) return;
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return;
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
    }
}

}