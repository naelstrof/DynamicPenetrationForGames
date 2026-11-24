using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace GatorDragonGames {

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(PenetratorCollisionSpheresAuthored.PenetratorCollisionSphere))]
public class PenetratorCollisionSphereDrawer : PropertyDrawer {
    private static SerializedProperty _activeProperty;
    public override VisualElement CreatePropertyGUI(SerializedProperty property) {
        var root = new VisualElement();
        root.Add(new PropertyField(property));
        var button = new Button {
            text = "Show Gizmo"
        };
        button.clicked += () => {
            _activeProperty = property.Copy();
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        };
        root.Add(button);
        return root;
    }


    private void OnSceneGUI(SceneView sceneView) {
        try {
            if (_activeProperty == null || _activeProperty.serializedObject == null) {
                SceneView.duringSceneGui -= OnSceneGUI;
                return;
            }

            PenetratorCollisionSpheresAuthored.PenetratorCollisionSphere.DrawSphereHandles(_activeProperty);
            _activeProperty.serializedObject.ApplyModifiedProperties();
        } catch {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }
}
#endif

[Serializable]
public struct PenetratorCollisionSpheresAuthored {
    private const float sphereCastInflation = 0.1f;
    [Serializable]
    public struct PenetratorCollisionSphere {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public Matrix4x4 GetWorldMatrix(float offsetScale) {
            var output =  transform.localToWorldMatrix * Matrix4x4.TRS(localPosition, localRotation, localScale);
            var pos = output.GetPosition();
            var rot = output.rotation;
            var scale = output.lossyScale;
            scale += new Vector3(offsetScale, offsetScale, offsetScale);
            return Matrix4x4.TRS(pos, rot, scale);
        }

        public bool RaycastIntersection(Ray ray) {
            var worldMatrix = GetWorldMatrix(0f);
            var sphereCenter = worldMatrix.GetPosition();
            var sphereRadius = worldMatrix.lossyScale.x * 0.5f;
            var oc = ray.origin - sphereCenter;
            var a = Vector3.Dot(ray.direction, ray.direction);
            var b = 2.0f * Vector3.Dot(oc, ray.direction);
            var c = Vector3.Dot(oc, oc) - sphereRadius * sphereRadius;
            var discriminant = b * b - 4f * a * c;
            return discriminant > 0;
        }

#if UNITY_EDITOR
        public static void DrawSphereHandles(SerializedProperty property) {
            var tool = Tools.current;
            var transform = property.FindPropertyRelative("transform").objectReferenceValue as Transform;
            if (transform == null) return;
            var localPosition = property.FindPropertyRelative("localPosition");
            var localRotation = property.FindPropertyRelative("localRotation");
            var localScale = property.FindPropertyRelative("localScale");
            
            if (tool == Tool.Move) {
                EditorGUI.BeginChangeCheck();
                var newPos = Handles.PositionHandle(transform.position + transform.rotation * localPosition.vector3Value, transform.rotation);
                if (EditorGUI.EndChangeCheck()) {
                    localPosition.vector3Value = Quaternion.Inverse(transform.rotation) * (newPos - transform.position);
                }
            } else if (tool == Tool.Rotate) {
                EditorGUI.BeginChangeCheck();
                var newRot = Handles.RotationHandle(transform.rotation * localRotation.quaternionValue, transform.position + transform.rotation * localPosition.vector3Value);
                if (EditorGUI.EndChangeCheck()) {
                    localRotation.quaternionValue = Quaternion.Inverse(transform.rotation) * newRot;
                }
            } else if (tool == Tool.Scale) {
                EditorGUI.BeginChangeCheck();
                var newScale = Handles.ScaleHandle(localScale.vector3Value, transform.position + transform.rotation * localPosition.vector3Value,
                transform.rotation * localRotation.quaternionValue,
                HandleUtility.GetHandleSize(transform.position + transform.rotation * localPosition.vector3Value));
                if (EditorGUI.EndChangeCheck()) {
                    localScale.vector3Value = newScale;
                }
            }
        }
#endif
    }

    public void OnValidate() {
        if (_penetratorCollisionSpheres == null) return;
        for(int i=0; i < _penetratorCollisionSpheres.Length; i++) {
            if (_penetratorCollisionSpheres[i].localRotation is { x: 0, y: 0, z: 0, w: 0 }) {
                _penetratorCollisionSpheres[i].localRotation = Quaternion.identity;
            }
            if (_penetratorCollisionSpheres[i].localScale is { x: 0, y: 0, z: 0 }) {
                _penetratorCollisionSpheres[i].localScale = Vector3.one*0.01f;
            }

            if (_penetratorCollisionSpheres[i].localScale.x < 0f) {
                _penetratorCollisionSpheres[i].localScale.x = 0.01f;
            }
            if (_penetratorCollisionSpheres[i].localScale.y < 0f) {
                _penetratorCollisionSpheres[i].localScale.y = 0.01f;
            }
            if (_penetratorCollisionSpheres[i].localScale.z < 0f) {
                _penetratorCollisionSpheres[i].localScale.z = 0.01f;
            }
        }
    }
    public void OnDrawGizmosSelected() {
        if (_penetratorCollisionSpheres == null) return;
        foreach (var sphere in _penetratorCollisionSpheres) {
            if (sphere.transform == null) continue;
            Gizmos.matrix = sphere.GetWorldMatrix(Application.isPlaying ? sphereCastInflation : 0f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, 1f);
        }
    }

#if UNITY_EDITOR
    public static void DrawHandles(SerializedProperty property) {
        if (Application.isPlaying) return;
        var penetratorCollisionSpheres = property.FindPropertyRelative("_penetratorCollisionSpheres");
        if (penetratorCollisionSpheres == null) return;
        for (int i=0;i<penetratorCollisionSpheres.arraySize;i++) {
            var sphere = penetratorCollisionSpheres.GetArrayElementAtIndex(i);
            PenetratorCollisionSphere.DrawSphereHandles(sphere);
        }
    }
#endif

    public void Render(Renderer[] renderers) {
        _sphereCasts ??= new SphereCasterRenderer.SphereCast[_penetratorCollisionSpheres.Length];
        Assert.IsTrue(_sphereCasts != null && _sphereCasts.Length == _penetratorCollisionSpheres.Length, "Please call Initialize() before calling Render() on PenetratorCollisionSpheres.");
        for (int i=0; i < _penetratorCollisionSpheres.Length; i++) {
            var sphere = _penetratorCollisionSpheres[i];
            _sphereCasts[i] = new SphereCasterRenderer.SphereCast(sphere.GetWorldMatrix(sphereCastInflation), 0.5f, 0f, true, true, true);
        }
        var rendererCount = renderers.Length;
        for (int i=0;i<rendererCount;i++) {
            SphereCasterRenderer.Render(renderers[i], _sphereCasts);
        }
    }
    [HideInInspector] private SphereCasterRenderer.SphereCast[] _sphereCasts;
    [SerializeField] private PenetratorCollisionSphere[] _penetratorCollisionSpheres;
}
}