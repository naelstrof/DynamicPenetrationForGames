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
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Penetrator))]
public class PenetratorInspector : Editor {
    
    private bool isEditingRoot;
    
    public override void OnInspectorGUI() {
        var script = target as Penetrator;
        if (GUILayout.Button("Edit position and orientation")) {
            isEditingRoot = true;
            SceneView.RepaintAll();
        }
        base.OnInspectorGUI();
    }

    protected void OnSceneGUI() {
        var script = (Penetrator)target;
        Undo.RecordObject(this, "Transforms Updated");
        script.DrawSceneGUI(isEditingRoot);
    }

}

#endif

[ExecuteAlways]
public abstract class Penetrator : MonoBehaviour {
    [FormerlySerializedAs("penetrator")]
    [SerializeField] protected PenetratorData penetratorData;
    [SerializeField, Range(0.1f, 2f)] protected float squashAndStretch = 1f;
    
    [SerializeField] protected PenetratorRenderers penetratorRenderers;

    public struct PenetrationArgs {
        public PenetratorData penetratorData;
        public float penetrationDepth;
        public CatmullSpline alongSpline;
        public int penetrableStartIndex;

        public PenetrationArgs(PenetratorData penetratorData, float penetrationDepth, CatmullSpline alongSpline, int penetrableStartIndex) {
            this.penetratorData = penetratorData;
            this.penetrationDepth = penetrationDepth;
            this.alongSpline = alongSpline;
            this.penetrableStartIndex = penetrableStartIndex;
        }
    }
    
    public delegate void PenetrationAction(Penetrator penetrator, Penetrable penetrable, PenetrationArgs penetrationArgs, Penetrable.PenetrationResult result);
    public delegate void UnpenetrateAction(Penetrator penetrator, Penetrable penetrable);
    public event PenetrationAction penetrated;
    public event UnpenetrateAction unpenetrated;
    
    protected CatmullSpline cachedSpline;

    protected Penetrable.PenetrationResult? penetrationResult = null;

    protected abstract IList<Vector3> GetPoints();

    public virtual void GetOutputRenderers(IList<Renderer> output) => penetratorRenderers.GetRenderers(output);
    public virtual void AddOutputRenderer(Renderer renderer) => penetratorRenderers.AddRenderer(renderer);
    public virtual void RemoveOutputRenderer(Renderer renderer) => penetratorRenderers.RemoveRenderer(renderer);
    
    protected virtual void OnEnable() {
        cachedSpline = new CatmullSpline(new[] { Vector3.zero, Vector3.one });
        penetratorData.Initialize();
        penetratorRenderers.Initialize();
    }

    protected bool IsValid() {
#if UNITY_EDITOR
        return penetratorData.IsValid() && penetratorRenderers.IsValid();
#else
        return true;
#endif
    }

    protected Vector3 GetBasePointOne() => penetratorData.GetBasePointOne();
    protected Vector3 GetBasePointTwo() => penetratorData.GetBasePointTwo();
    public Transform GetRootTransform() => penetratorData.GetRootTransform();
    public Vector3 GetRootPositionOffset() => penetratorData.GetRootPositionOffset();
    public Vector3 GetRootForward() => penetratorData.GetRootForward();
    public Vector3 GetRootUp() => penetratorData.GetRootUp();
    public Vector3 GetRootRight() => penetratorData.GetRootRight();
    public Texture2D GetDetailMap() => penetratorData.GetDetailMap();
    public RenderTexture GetGirthMap() => penetratorData.GetGirthMap();
    public float GetGirthScaleFactor() => penetratorData.GetGirthScaleFactor();
    public float GetKnotForce(float alongLength) => penetratorData.GetKnotForce(alongLength);
    public float GetPenetratorAngleOffset(CatmullSpline path) {
        Vector3 initialRight = path.GetBinormalFromT(0f);
        Vector3 initialForward = path.GetVelocityFromT(0f).normalized;
        Vector3 initialUp = Vector3.Cross(initialForward, initialRight).normalized;
        Vector3 worldDickUp = GetRootTransform().TransformDirection(GetRootUp()).normalized;
        Vector2 worldDickUpFlat = new Vector2(Vector3.Dot(worldDickUp,initialRight), Vector3.Dot(worldDickUp,initialUp));
        float angle = Mathf.Atan2(worldDickUpFlat.y, worldDickUpFlat.x)-Mathf.PI/2f;
        return angle;
    }
    public virtual float GetSquashStretchedWorldLength() {
        return penetratorData.GetWorldLength() * squashAndStretch;
    }
    
    public virtual float GetWorldGirthRadius(float distanceAlongPenetrator) {
        return penetratorData.GetWorldGirthRadius(distanceAlongPenetrator/squashAndStretch);
    }

    protected virtual void SetPenetrationData(Penetrable.PenetrationResult? result) {
        penetrationResult = result;
    }

    public virtual Penetrable.PenetrationResult? GetPenetrationData() {
        return penetrationResult;
    }

    protected virtual void LateUpdate() {
        if (!IsValid()) {
            return;
        }
        penetratorData.GetSpline(GetPoints(), ref cachedSpline, out float distanceAlongSpline);
        penetratorRenderers.Update(
            cachedSpline,
            penetratorData.GetWorldLength(),
            squashAndStretch,
            0f,
            distanceAlongSpline,
            penetratorData.GetRootTransform(),
            penetratorData.GetRootForward(),
            penetratorData.GetRootRight(),
            penetratorData.GetRootUp(),
            penetrationResult?.clippingRange,
            penetrationResult?.truncation
        );
    }

    protected virtual void OnDisable() {
        penetratorData.Release();
        penetratorRenderers.OnDestroy();
    }

    protected virtual void OnValidate() {
        penetratorData.OnValidate();
    }

    private static CatmullSpline lerpSplineA;
    private static CatmullSpline lerpSplineB;
    protected static void LerpPoints(IList<Vector3> output, IList<Vector3> a, IList<Vector3> b, float t) {
        if (t <= 0) {
            while (output.Count < a.Count) output.Add(Vector3.zero);
            while (output.Count > a.Count) output.RemoveAt(output.Count-1);
            for (int i = 0; i < a.Count; i++) {
                output[i] = a[i];
            }
            return;
        }

        if (t>=1f) {
            while (output.Count < b.Count) output.Add(Vector3.zero);
            while (output.Count > b.Count) output.RemoveAt(output.Count-1);
            for (int i = 0; i < b.Count; i++) {
                output[i] = b[i];
            }
            return;
        }
        
        while (a.Count < b.Count) a.Add(a[^1]+(a[^1]-a[^2]));
        while (b.Count < a.Count) b.Add(b[^1]+(b[^1]-b[^2]));
        if (output.Count > a.Count) {
            output.Clear();
        }
        while (output.Count < a.Count) output.Add(Vector3.zero);
        lerpSplineA ??= new CatmullSpline(a);
        lerpSplineA.SetWeightsFromPoints(a);
        lerpSplineB ??= new CatmullSpline(b);
        lerpSplineB.SetWeightsFromPoints(b);
        for (var index = 0; index < a.Count; index++) {
            var sourceT = lerpSplineA.GetDistanceFromTime((float)index / (a.Count - 1));
            var targetT = lerpSplineB.GetDistanceFromTime((float)index / (b.Count - 1));
            var lerpT = lerpSplineB.GetPositionFromDistance(Mathf.Lerp(sourceT, targetT, t));
            output[index] = Vector3.Lerp(a[index], lerpT, t);
        }
    }

    public virtual void GetFinalizedSpline(ref CatmullSpline finalizedSpline, out float distanceAlongSpline, out float insertionLerp, out PenetrationArgs? penetrationArgs) {
        penetratorData.GetSpline(GetPoints(), ref finalizedSpline, out distanceAlongSpline);
        penetrationArgs = null;
        insertionLerp = 0f;
    }

    protected virtual void OnDrawGizmosSelected() {
        if (GetPoints().Count == 0 || !IsValid()) {
            return;
        }

        GetFinalizedSpline(ref cachedSpline, out var distanceAlongSpline, out var insertionLerp, out var penetrationArgs);
        CatmullSpline.GizmosDrawSpline(cachedSpline, Color.red, Color.green);
    }
    
    protected virtual void OnPenetrated(Penetrable penetrable, PenetrationArgs penetrationArgs, Penetrable.PenetrationResult result) {
        penetrated?.Invoke(this, penetrable, penetrationArgs, result);
    }
    protected virtual void OnUnpenetrated(Penetrable penetrable) {
        unpenetrated?.Invoke(this, penetrable);
    }
    
#if UNITY_EDITOR
    public void DrawSceneGUI(bool isEditingRoot) {
        if (!isEditingRoot) return;
        Undo.RecordObject(this, "Transforms Updated");
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.white;
        var globalPenetratorRootPositionRotation = Quaternion.LookRotation(penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootForward()), penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootUp()));
        var globalPenetratorRootPositionOffset = penetratorData.GetRootTransform().TransformPoint(penetratorData.GetRootPositionOffset());
        globalPenetratorRootPositionOffset = Handles.PositionHandle(globalPenetratorRootPositionOffset, globalPenetratorRootPositionRotation);
        globalPenetratorRootPositionRotation = Handles.RotationHandle(globalPenetratorRootPositionRotation, globalPenetratorRootPositionOffset);
        if (EditorGUI.EndChangeCheck()) {
            penetratorData.SetPenetratorPositionInfo(
                penetratorData.GetRootTransform().InverseTransformPoint(globalPenetratorRootPositionOffset),
                Quaternion.Inverse(penetratorData.GetRootTransform().rotation) * globalPenetratorRootPositionRotation
            );
        }
        Handles.DrawWireDisc(
            globalPenetratorRootPositionOffset+globalPenetratorRootPositionRotation*Vector3.forward * penetratorData.GetWorldLength(),
            globalPenetratorRootPositionRotation*Vector3.forward,
            0.1f
            );
    }
#endif
}

}