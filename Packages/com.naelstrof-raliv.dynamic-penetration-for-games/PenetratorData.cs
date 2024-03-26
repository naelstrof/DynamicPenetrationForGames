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

#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(PenetratorData))]
public class PenetratorDataPropertyDrawer : PropertyDrawer {
    private static bool foldout;
    private float height = 20;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);
        float startPos = position.y;
        var rect = new Rect(position.x, position.y, position.width, 20);
        foldout = EditorGUI.BeginFoldoutHeaderGroup(rect, foldout, "Penetrator Data");
        if (foldout) {
            rect.y += 20;
            rect.x += 10;
            rect.width -= 10;
            if (property.FindPropertyRelative("mask").FindPropertyRelative("renderer").objectReferenceValue is SkinnedMeshRenderer skinnedMeshRenderer) {
                if (!skinnedMeshRenderer.sharedMesh.isReadable) {
                    rect.height += 10;
                    EditorGUI.HelpBox(rect, $"Imported mesh {skinnedMeshRenderer.sharedMesh.name} must have Read/Write enabled in the import settings.", MessageType.Error);
                    rect.height -= 10;
                    rect.y += 30;
                    if (GUI.Button(rect, "Auto-Fix")) {
                        var path = AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh);
                        var importer = AssetImporter.GetAtPath(path);
                        if (importer is ModelImporter modelImporter) {
                            modelImporter.isReadable = true;
                            modelImporter.SaveAndReimport();
                        }
                    }
                    rect.y += 20;
                }
            }
            EditorGUIUtility.labelWidth = rect.width*0.4f;
            //EditorGUI.PropertyField(minRect, property.FindPropertyRelative("mask"));
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("mask"), GUIContent.none);
            rect.y += 20;
            var subRect = EditorGUI.PrefixLabel(rect, new GUIContent("Root Transform"));
            EditorGUI.PropertyField(subRect, property.FindPropertyRelative("penetratorRootTransform"), GUIContent.none);
            rect.y += 20;
            if (GUI.Button(rect, "Reset position and orientation")) {
                property.FindPropertyRelative("penetratorRootPositionOffset").vector3Value = Vector3.zero;
                property.FindPropertyRelative("penetratorRootForward").vector3Value = Vector3.forward;
                property.FindPropertyRelative("penetratorRootUp").vector3Value = Vector3.up;
            }
        }
        float endPos = rect.y+rect.height;
        EditorGUI.EndFoldoutHeaderGroup();
        EditorGUI.EndProperty();
        height = endPos - startPos;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return height;
    }
    
}
#endif

[System.Serializable]
public class PenetratorData {
    [SerializeField] private RendererSubMeshMask mask;
    
    [SerializeField] private Transform penetratorRootTransform;
    [SerializeField] private Vector3 penetratorRootPositionOffset;
    [SerializeField] private Vector3 penetratorRootForward = Vector3.up;
    [SerializeField] private Vector3 penetratorRootUp = -Vector3.back;
    [SerializeField] private Shader girthUnwrapShader;

    public Transform GetRootTransform() => penetratorRootTransform;
    public Vector3 GetRootPositionOffset() => penetratorRootPositionOffset;
    public Vector3 GetRootForward() => penetratorRootForward;
    public Vector3 GetRootUp() => penetratorRootUp;
    public Texture2D GetDetailMap() => girthData.GetDetailMap();
    public RenderTexture GetGirthMap() => girthData.GetGirthMap();
    public Vector3 GetRootRight() => Vector3.Cross(penetratorRootUp, penetratorRootForward);
    public Vector3 GetWorldOffset(float alongLength) => girthData.GetScaledSplineSpaceOffset(alongLength);
    
    public float GetWorldLength() => girthData.GetWorldLength();
    public float GetRendererLength() => girthData.GetWorldRenderLength();
    public float GetWorldGirthRadius(float alongLength) => girthData.GetWorldGirthRadius(alongLength);
    public float GetGirthScaleFactor() => girthData.GetGirthScaleFactor();
    public float GetKnotForce(float alongLength) => girthData.GetKnotForce(alongLength);
    

    public void ResetRoot() {
        penetratorRootPositionOffset = Vector3.zero;
        penetratorRootForward = Vector3.forward;
        penetratorRootUp = Vector3.up;
    }

    public void SetPenetratorPositionInfo(Vector3 position, Quaternion rotation) {
        penetratorRootPositionOffset = position;
        penetratorRootForward = rotation * Vector3.forward;
        penetratorRootUp = rotation * Vector3.up;
        Reinitialize();
    }
    
    //[SerializeField]
    private GirthData girthData;
    
    private static List<Vector3> points = new List<Vector3>();

    private bool GetInitialized() => girthData != null;

    public bool IsValid() {
        return mask.renderer != null && penetratorRootTransform != null && Vector3.Dot(penetratorRootForward, penetratorRootUp) <= Mathf.Epsilon;
    }

    public void Release() {
        girthData?.Release();
        girthData = null;
    }
    
    private void Reinitialize() {
        // TODO: There should be a way to update the girthdata
        Release();
        Initialize();
    }
    
    public void Initialize() {
        if (GetInitialized()) {
            return;
        }
        Vector3 up = penetratorRootUp;
        if (up == penetratorRootForward) {
            throw new UnityException("Non-orthogonal basis given!!!");
        }
        Vector3 right = Vector3.right;
        Vector3.OrthoNormalize(ref penetratorRootForward, ref up, ref right);
        if (penetratorRootTransform == null || mask.renderer == null) {
            return;
        }
        girthData = new GirthData(mask, girthUnwrapShader, penetratorRootTransform, penetratorRootPositionOffset, penetratorRootForward, penetratorRootUp, right);
    }
    
    public void GetSpline(IList<Vector3> inputPoints, ref CatmullSpline spline, out float baseDistanceAlongSpline) {
        Initialize();
        points.Clear();

        points.AddRange(inputPoints);

        Vector3 dir = (points[^1] - points[^2]).normalized;
        points.Add(points[^1] + dir * (girthData.GetWorldLength()*1.25f));

        spline.SetWeightsFromPoints(points);
        baseDistanceAlongSpline = spline.GetLengthFromSubsection(1);
    }
    
    public Vector3 GetBasePointOne() {
        Vector3 startPoint = penetratorRootTransform.TransformPoint(penetratorRootPositionOffset);
        return startPoint + penetratorRootTransform.TransformDirection(penetratorRootForward) * (girthData?.GetWorldLength() * -0.25f ?? 1f);
    }

    public Vector3 GetBasePointTwo() {
        Vector3 startPoint = penetratorRootTransform.TransformPoint(penetratorRootPositionOffset);
        return startPoint;
    }

    public void OnValidate() {
        if (!Application.isPlaying) {
            Reinitialize();
        }

#if UNITY_EDITOR
        if (girthUnwrapShader == null) {
            girthUnwrapShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("34d24fe4568f98c4cae4724e139cb644"));
        }
#endif
    }

}

}