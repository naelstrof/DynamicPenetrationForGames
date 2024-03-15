using System.Collections.Generic;
using PenetrationTech;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(PenetratorData))]
public class PenetratorDataPropertyDrawer : PropertyDrawer {
    private static bool foldout;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);
        var rect = new Rect(position.x, position.y, position.width, 20);
        foldout = EditorGUI.BeginFoldoutHeaderGroup(rect, foldout, "Penetrator Data");
        if (foldout) {
            rect.y += 20;
            rect.x += 10;
            rect.width -= 10;
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
        EditorGUI.EndFoldoutHeaderGroup();
        EditorGUI.EndProperty();
        
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return foldout?80:20;
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

    public Transform GetRootTransform() => penetratorRootTransform;
    public Vector3 GetRootPositionOffset() => penetratorRootPositionOffset;
    public Vector3 GetRootForward() => penetratorRootForward;
    public Vector3 GetRootUp() => penetratorRootUp;
    public Texture2D GetDetailMap() => girthData.GetDetailMap();
    public RenderTexture GetGirthMap() => girthData.GetGirthMap();
    public Vector3 GetRootRight() => Vector3.Cross(penetratorRootUp, penetratorRootForward);
    // TODO: Girth Data World Length does not take root position into account
    public float GetPenetratorWorldLength() => girthData.GetWorldLength();
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
        girthData = new GirthData(mask, Shader.Find("Hidden/DPG/GirthUnwrapRaw"), penetratorRootTransform, penetratorRootPositionOffset, penetratorRootForward, penetratorRootUp, right);
    }
    
    public void GetSpline(IList<Vector3> inputPoints, out CatmullSpline spline, out float baseDistanceAlongSpline) {
        Initialize();
        points.Clear();

        points.AddRange(inputPoints);

        Vector3 dir = (points[^1] - points[^2]).normalized;
        points.Add(points[^1] + dir * (girthData.GetWorldLength()*1.25f));
        
        spline = new CatmullSpline(points);
        baseDistanceAlongSpline = spline.GetLengthFromSubsection(1);
    }
    
    public Vector3 GetBasePointOne() {
        Vector3 startPoint = penetratorRootTransform.TransformPoint(penetratorRootPositionOffset);
        return startPoint + penetratorRootTransform.TransformDirection(penetratorRootForward) * (girthData.GetWorldLength() * -0.25f);
    }

    public Vector3 GetBasePointTwo() {
        Vector3 startPoint = penetratorRootTransform.TransformPoint(penetratorRootPositionOffset);
        return startPoint;
    }

    public void OnValidate() {
        if (!Application.isPlaying) {
            Reinitialize();
        }
    }

}
