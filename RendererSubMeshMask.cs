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

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(RendererSubMeshMask))]
public class RendererSubMeshMaskDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        SerializedProperty rendererProperty = property.FindPropertyRelative("renderer");
        float width = position.width;
        position.width = width*0.75f;
        EditorGUI.ObjectField(position, rendererProperty, new GUIContent("Skinned Mesh and masks") );
        position.x += width*0.75f + 10f;
        position.width = width*0.25f - 10f;
        Renderer renderer = (Renderer)rendererProperty.objectReferenceValue;
        Mesh mesh = null;
        switch (renderer) {
            case SkinnedMeshRenderer skinnedRenderer:
                mesh = skinnedRenderer.sharedMesh;
                break;
            case MeshRenderer:
                mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                break;
            default: {
                if (renderer != null) {
                    EditorGUI.HelpBox(position, "We only support SkinnedMeshRenderers and MeshRenderers", MessageType.Error);
                    throw new UnityException("We only support SkinnedMeshRenderers and MeshRenderers");
                }
                break;
            }
        }

        SerializedProperty maskProp = property.FindPropertyRelative("mask");
        if (EditorGUI.DropdownButton(position, new GUIContent("SubMeshMask"), FocusType.Passive) && renderer != null && mesh != null) {
            GenericMenu menu = new GenericMenu();
            for(int i=0;i<mesh.subMeshCount;i++) {
                string name = $"Sub-mesh {i}";
                if (renderer.sharedMaterials.Length > i && renderer.sharedMaterials[i] != null) {
                    name = $"Sub-mesh {i} [{renderer.sharedMaterials[i].name}]";
                }

                int alloc = i;
                menu.AddItem(new GUIContent(name), RendererSubMeshMask.GetMask(maskProp.intValue,i), ()=> {
                    maskProp.intValue = RendererSubMeshMask.ToggleMask(maskProp.intValue, alloc);
                    maskProp.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }
    }
}
#endif

[System.Serializable]
public class RendererSubMeshMask : ISerializationCallbackReceiver {
    [SerializeField]
    public Renderer renderer;
    [SerializeField]
    public int mask = ~0;

    public bool ShouldDrawSubmesh(int index) {
        return GetMask(mask, index);
    }

    public static int SetMask(int m, int index, bool set) {
        if (set) {
            return (m | (1 << index));
        }
        m &= ~(1 << index);
        return m & ~(1 << index);
    }

    public static int ToggleMask(int m, int index) {
        return SetMask(m, index, !GetMask(m, index));
    }

    public static bool GetMask(int m, int index) {
        return (m & (1 << index)) != 0;
    }

    public void OnBeforeSerialize() {
    }

    public void OnAfterDeserialize() {
        if (mask == 0) {
            mask = ~0;
        }
    }

    public Mesh GetMesh() {
        Mesh mesh;
        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer1) {
            mesh = skinnedMeshRenderer1.sharedMesh;
        } else if (renderer is MeshRenderer) {
            mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        } else {
            throw new UnityException("Girth data can only be generated on SkinnedMeshRenderers and MeshRenderers.");
        }
        return mesh;
    }
}

}