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

using UnityEngine.UIElements;

namespace DPG {

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;

[CustomEditor(typeof(Penetrator))]
public class PenetratorInspector : Editor {
    
    private bool isEditingRoot;

    public override VisualElement CreateInspectorGUI() {
        var visualElement = new VisualElement();
        var button = new Button {
            text = "Edit Position and Rotation"
        };
        button.clicked += () => {
            isEditingRoot = true;
            SceneView.RepaintAll();
        };
        visualElement.Add(button);
        InspectorElement.FillDefaultInspector(visualElement, serializedObject, this);
        return visualElement;
    }

    protected void OnSceneGUI() {
        var script = (Penetrator)target;
        script.DrawSceneGUI(isEditingRoot, serializedObject.FindProperty("penetratorData"));
        serializedObject.ApplyModifiedProperties();
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
        penetratorData?.Initialize();
        penetratorRenderers?.OnEnable();
        PenetrationManager.SubscribeToPenetratorUpdates(OnPenetratorUpdate);
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
    public Vector3 GetWorldOffset(float alongLength, CatmullSpline path, float distanceAlongSpline) {
        var trans = path.GetReferenceFrameFromT(cachedSpline.GetTimeFromDistance(alongLength + distanceAlongSpline));
        return penetratorData.GetWorldOffset(alongLength/squashAndStretch);
    }
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

    protected virtual void OnPenetratorUpdate() {
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
        penetratorRenderers?.OnDisable();
    }

    protected virtual void OnValidate() {
        penetratorData?.OnValidate();
        penetratorRenderers?.OnValidate();
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
        if (!IsValid() || GetPoints().Count == 0) {
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
    public void DrawSceneGUI(bool isEditingRoot, SerializedProperty penetratorDataProp) {
        if (!isEditingRoot) return;
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.white;
        var globalPenetratorRootPositionRotation = Quaternion.LookRotation(penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootForward()), penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootUp()));
        var globalPenetratorRootPositionOffset = penetratorData.GetRootTransform().TransformPoint(penetratorData.GetRootPositionOffset());
        globalPenetratorRootPositionOffset = Handles.PositionHandle(globalPenetratorRootPositionOffset, globalPenetratorRootPositionRotation);
        globalPenetratorRootPositionRotation = Handles.RotationHandle(globalPenetratorRootPositionRotation, globalPenetratorRootPositionOffset);
        if (EditorGUI.EndChangeCheck()) {
            Vector3 position = penetratorData.GetRootTransform().InverseTransformPoint(globalPenetratorRootPositionOffset);
            Quaternion rotation = Quaternion.Inverse(penetratorData.GetRootTransform().rotation) * globalPenetratorRootPositionRotation;
            penetratorDataProp.FindPropertyRelative("penetratorRootPositionOffset").vector3Value = position;
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;
            Vector3.OrthoNormalize(ref forward, ref up, ref right);
            
            penetratorDataProp.FindPropertyRelative("penetratorRootForward").vector3Value = forward;
            penetratorDataProp.FindPropertyRelative("penetratorRootUp").vector3Value = up;
        }

        Handles.color = Color.blue;
        Handles.DrawWireDisc(
            globalPenetratorRootPositionOffset+globalPenetratorRootPositionRotation*Vector3.forward * penetratorData.GetWorldLength(),
            globalPenetratorRootPositionRotation*Vector3.forward,
            0.1f
            );
        
        GetFinalizedSpline(ref cachedSpline, out var distanceAlongSpline, out var insertionLerp, out var penetrationArgs);
        float length = GetSquashStretchedWorldLength();
        for (int i = 0; i < 32; i++) {
            float dist = (float)i / 31 * length;
            Vector3 pos = cachedSpline.GetPositionFromDistance(dist+distanceAlongSpline);
            float girth = penetratorData.GetWorldGirthRadius(dist/squashAndStretch);
            Vector3 offset = GetWorldOffset(dist/squashAndStretch, cachedSpline, distanceAlongSpline);
            Vector3 normal = cachedSpline.GetVelocityFromDistance(dist + distanceAlongSpline);
            if (girth <= 0) {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(pos + offset, normal, 0.01f);
            } else {
                Handles.color = Color.white;
                Handles.DrawWireDisc(pos + offset, normal, girth);
            }
        }
    }
#endif
}

}