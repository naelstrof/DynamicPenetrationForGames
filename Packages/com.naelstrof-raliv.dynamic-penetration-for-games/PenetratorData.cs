using System.Collections.Generic;
using PenetrationTech;
using UnityEditor;
using UnityEngine;

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
            EditorGUI.PropertyField(subRect, property.FindPropertyRelative("dickRootTransform"), GUIContent.none);
            rect.y += 20;
            if (GUI.Button(rect, "Reset position and orientation")) {
                property.FindPropertyRelative("dickRootPositionOffset").vector3Value = Vector3.zero;
                property.FindPropertyRelative("dickRootForward").vector3Value = Vector3.forward;
                property.FindPropertyRelative("dickRootUp").vector3Value = Vector3.up;
            }
        }
        EditorGUI.EndFoldoutHeaderGroup();
        EditorGUI.EndProperty();
        
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return foldout?80:20;
    }
    
}

[System.Serializable]
public class PenetratorData {
    [SerializeField] private RendererSubMeshMask mask;
    
    [SerializeField] private Transform dickRootTransform;
    [SerializeField] private Vector3 dickRootPositionOffset;
    [SerializeField] private Vector3 dickRootForward;
    [SerializeField] private Vector3 dickRootUp;

    public Transform GetRootTransform() => dickRootTransform;
    public Vector3 GetRootPositionOffset() => dickRootPositionOffset;
    public Vector3 GetRootForward() => dickRootForward;
    public Vector3 GetRootUp() => dickRootUp;
    public Vector3 GetRootRight() => Vector3.Cross(dickRootUp, dickRootForward);
    // TODO: Girth Data World Length does not take root position into account
    public float GetPenetratorWorldLength() => girthData.GetWorldLength();

    public void ResetRoot() {
        dickRootPositionOffset = Vector3.zero;
        dickRootForward = Vector3.forward;
        dickRootUp = Vector3.up;
    }

    public void SetDickPositionInfo(Vector3 position, Quaternion rotation) {
        dickRootPositionOffset = position;
        dickRootForward = rotation * Vector3.forward;
        dickRootUp = rotation * Vector3.up;
        Reinitialize();
    }
    
    private GirthData girthData;
    private static List<Vector3> points = new List<Vector3>();

    private bool GetInitialized() => girthData != null;

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
        Vector3 up = dickRootUp;
        if (up == dickRootForward) {
            throw new UnityException("Non-orthogonal basis given!!!");
        }
        Vector3 right = Vector3.right;
        Vector3.OrthoNormalize(ref dickRootForward, ref up, ref right);
        girthData = new GirthData(mask, Shader.Find("Hidden/DPG/GirthUnwrapRaw"), dickRootTransform, dickRootPositionOffset, dickRootForward, dickRootUp, right);
    }
    
    public void GetSpline(IList<Vector3> inputPoints, out CatmullSpline spline, out float baseDistanceAlongSpline) {
        if (!GetInitialized()) {
            Initialize();
        }
        points.Clear();

        Vector3 startPoint = dickRootTransform.TransformPoint(dickRootPositionOffset);
        points.Add(startPoint + dickRootTransform.TransformDirection(dickRootForward) * (girthData.GetWorldLength() * -0.25f));
        points.Add(startPoint);
        points.AddRange(inputPoints);

        Vector3 dir = (points[^1] - points[^2]).normalized;
        points.Add(points[^1] + dir * girthData.GetWorldLength());
        
        spline = new CatmullSpline(points);
        baseDistanceAlongSpline = spline.GetDistanceFromSubT(0, 1, 1f);
    }

    public void OnValidate() {
        Reinitialize();
    }

}
