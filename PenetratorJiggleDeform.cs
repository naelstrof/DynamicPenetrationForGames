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

using System;
using UnityEngine.Assertions;

namespace DPG {
using System.Collections.Generic;
using GatorDragonGames.JigglePhysics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class PenetratorSquashStretch {
    
    const float TICKRATE = 0.02f;
    
    float lengthFrictionSquared;
    float lengthElasticity;
    private float desiredLength;
    private float desiredLengthVelocity;
    private float? lastPenetrablePosition;
    float squashStretch;
    float timeSinceLastTick;

    public float GetSquashStretch() => squashStretch;

    public PenetratorSquashStretch(float initialLength, float lengthElasticity, float lengthFriction) {
        desiredLength = initialLength;
        this.lengthElasticity = lengthElasticity;
        lengthFrictionSquared = lengthFriction * lengthFriction;
    }

    public void Tick(bool inserted, float penetratorLength, float newPenetrablePosition, float penetrableFriction, float knotForce, float deltaTime) {
        timeSinceLastTick += deltaTime;
        if (timeSinceLastTick < TICKRATE) {
            return;
        }
        desiredLengthVelocity *= 1f - lengthFrictionSquared;

        if (inserted) {
            float penetrableVelocity = (newPenetrablePosition - (lastPenetrablePosition ?? newPenetrablePosition)) / TICKRATE;
            lastPenetrablePosition = newPenetrablePosition;

            desiredLengthVelocity = Mathf.Lerp(desiredLengthVelocity, penetrableVelocity, penetrableFriction * TICKRATE);
            desiredLengthVelocity += knotForce;
        } else {
            lastPenetrablePosition = null;
        }

        float elasticityCalc = lengthElasticity >= 1f ? 6000f : 10f / ((1f - lengthElasticity) * (1f - lengthElasticity));
        float squashAndStretchedWorldLength = penetratorLength * squashStretch;
        float elasticForce = (penetratorLength - squashAndStretchedWorldLength) * TICKRATE * elasticityCalc;

        desiredLengthVelocity += elasticForce;

        desiredLength += desiredLengthVelocity * TICKRATE;
        float desiredSquashAndStretch = desiredLength / penetratorLength;

        squashStretch = Mathf.Clamp(desiredSquashAndStretch, 0f, 2f);
        while(timeSinceLastTick>TICKRATE) timeSinceLastTick -= TICKRATE;
    }

}

[CustomEditor(typeof(PenetratorJiggleDeform))]
public class PenetratorJiggleDeformInspector : PenetratorInspector {
}

#endif

[ExecuteAlways]
public class PenetratorJiggleDeform : Penetrator {
    [SerializeField, Range(-90f, 90f)] private float leftRightCurvature = 0f;
    [SerializeField, Range(-90f, 90f)] private float upDownCurvature = 0f;
    [SerializeField, Range(-90f, 90f)] private float baseUpDownCurvatureOffset = 0f;
    [SerializeField, Range(-90f, 90f)] private float baseLeftRightCurvatureOffset = 0f;
    [SerializeField, Range(0f, 1f)] private float penetratorLengthFriction = 0.5f;
    [SerializeField, Range(0f, 0.95f)] private float penetratorLengthElasticity = 0.7f;
    [SerializeField, Range(0f, 2f)] private float knotForce = 1f;

    [SerializeField] protected Penetrable linkedPenetrable;
    
    private JiggleRig jiggleRig;

    [SerializeField, HideInInspector] private bool hasSerializedJiggleData;
    [SerializeField] private JiggleTreeInputParameters jiggleRigData;
    [SerializeField] private bool isAnimatedJigglePhysics;
    
    [SerializeField] private GameObject jigglePrefab;
    
    private List<Transform> simulatedPoints;
    private List<Vector3> points = new();
    private float lastInsertionAmount = 0f;
    private List<Collider> setupColliders = new();
    
    private Transform jiggleRoot;
    private PenetratorSquashStretch squashStretch;

    protected override void OnEnable() {
        base.OnEnable();
        //PenetrationManager.SubscribeToPenetratorFixedUpdates(OnPenetratorFixedUpdate);
        if (!Application.isPlaying) return;


        if (jiggleRoot == null) {
            var obj = Instantiate(jigglePrefab, GetRootTransform());
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            //obj.hideFlags = HideFlags.HideAndDontSave;
            Assert.IsTrue(obj.transform.childCount == 1, $"Prefab is misconfigured, first transform must be a container for the actual jiggles! Got {obj.transform.childCount} children instead of 1");
            
            jiggleRig = obj.GetComponentInChildren<JiggleRig>();
            jiggleRig.SetInputParameters(jiggleRigData);
            
            jiggleRoot = obj.transform.GetChild(0);
            simulatedPoints = new List<Transform>();
            var transformCrawl = jiggleRoot;
            int iterations = 0;
            const int maxIterations = 100;
            while (transformCrawl.childCount != 0 && iterations++ < maxIterations) {
                simulatedPoints.Add(transformCrawl);
                transformCrawl = transformCrawl.GetChild(0);
            }
        }
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
        squashStretch = new PenetratorSquashStretch(penetratorData.GetWorldLength(), penetratorLengthElasticity, penetratorLengthFriction);
    }

    private bool GetSimulationAvailable() => simulatedPoints != null && simulatedPoints.Count != 0;

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

    //private void OnPenetratorFixedUpdate() {
    //    if (!IsValid()) {
    //        return;
    //    }

    //    GetFinalizedSpline(ref cachedSpline, out var distanceAlongSpline, out var insertionLerp, out var penetrationArgs);

    //}

    protected override void OnPenetratorUpdate() {
        if (!IsValid()) {
            return;
        }

        if (isAnimatedJigglePhysics) {
            jiggleRig.SetInputParameters(jiggleRigData);
            jiggleRig.UpdateParameters();
        }

        if (GetSimulationAvailable()) {
            simulatedPoints[0].localScale = Vector3.one * simulatedPoints[0].parent.InverseTransformVector(GetRootTransform().TransformDirection(GetRootForward()) * GetSquashStretchedWorldLength()).magnitude;
        }

        GetFinalizedSpline(ref cachedSpline, out var distanceAlongSpline, out var insertionLerp, out var penetrationArgs);

        if (Time.deltaTime > 0f) {
            squashStretch.Tick(
                insertionLerp >= 1f,
                penetratorData.GetWorldLength(),
                penetrationArgs.HasValue
                    ? cachedSpline.GetLengthFromSubsection(penetrationArgs.Value.penetrableStartIndex)
                    : 0f,
                (penetrationResult?.knotForce ?? 0f) * knotForce * 7f,
                penetrationResult?.penetrableFriction * penetrationResult?.penetrableFriction ?? 0f,
                Time.deltaTime
            );
            squashAndStretch = squashStretch.GetSquashStretch();
        }

        if (linkedPenetrable) {
            if (insertionLerp >= 1f && penetrationArgs.HasValue) {
                if (GetSimulationAvailable()) {
                    for (int i = 0; i < simulatedPoints.Count; i++) {
                        float progress = (float)i / (simulatedPoints.Count - 1);
                        float totalDistance = progress * GetSquashStretchedWorldLength();
                        simulatedPoints[i].position = cachedSpline.GetPositionFromDistance(totalDistance);
                    }
                }
                
                var newResult = linkedPenetrable.SetPenetrated(this, penetrationArgs.Value);
                SetPenetrationData(newResult);
                OnPenetrated(linkedPenetrable, penetrationArgs.Value, newResult);
            } else if (lastInsertionAmount >= 1f) {
                linkedPenetrable.SetUnpenetrated(this);
                SetPenetrationData(null);
                OnUnpenetrated(linkedPenetrable);
                SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
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

    public void SetJiggleSettingsBlend(JiggleTreeInputParameters data) {
        
    }
    
    public JiggleTreeInputParameters GetJiggleSettingsBlend() {
        return jiggleRigData;
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

        Vector2 segmentCurvature = curvature / Mathf.Max(simulatedPoints.Count - 1, 1f);
        for (int i = 0; i < simulatedPoints.Count; i++) {
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
                float moveAmount = 1f / (simulatedPoints.Count - 1);
                simulatedPoints[i].transform.localPosition = Vector3.forward * moveAmount;
            }
        }

        simulatedPoints[0].localScale = Vector3.one * simulatedPoints[0].parent.InverseTransformVector(GetRootTransform().TransformDirection(GetRootForward()) * GetSquashStretchedWorldLength()).magnitude;
    }

    protected override void OnValidate() {
        base.OnValidate();
#if UNITY_EDITOR
        if (jigglePrefab == null) {
            jigglePrefab = AssetDatabase.LoadAssetByGUID<GameObject>(new GUID("bf0f13679933bbb57b506ceb38f2cb75"));
            Debug.Log(jigglePrefab);
        }
#endif
        if (!hasSerializedJiggleData) {
            jiggleRigData = JiggleTreeInputParameters.Default();
            hasSerializedJiggleData = true;
        }
        jiggleRigData.OnValidate();
        if (!Application.isPlaying) return;
        if (simulatedPoints == null || simulatedPoints.Count <= 1) return;
        SetCurvature(new Vector2(leftRightCurvature, upDownCurvature));
        jiggleRig.SetInputParameters(jiggleRigData);
        jiggleRig.UpdateParameters();
    }
}

}