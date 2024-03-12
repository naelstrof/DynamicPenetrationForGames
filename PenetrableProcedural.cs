using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PenetrableProcedural))]
public class PenetrableProceduralEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (!GUILayout.Button("Bake All...")) return;
        SerializedProperty renderTargetList = serializedObject.FindProperty("renderTargets");
        List<UnityEngine.Object> renderersToUndo = new List<UnityEngine.Object>();
        for (int j = 0; j < renderTargetList.arraySize; j++) {
            SerializedProperty skinnedMeshRendererProp = renderTargetList.GetArrayElementAtIndex(j);
            SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRendererProp.objectReferenceValue as SkinnedMeshRenderer;
            if (skinnedMeshRenderer == null) {
                continue;
            }

            if (skinnedMeshRenderer.sharedMesh.name.Contains("Clone")) {
                throw new UnityException("Possibly trying to bake data to a baked mesh. Reset your mesh if you want to bake from original data.");
            }

            renderersToUndo.Add(skinnedMeshRenderer);
        }
        string path = EditorUtility.OpenFolderPanel("Output mesh location","Assets","");
        // Catch user pressing the cancel button or closing the window
        if (string.IsNullOrEmpty(path)) {
            return;
        }
        int startIndex = path.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) {
            throw new UnityException("Must save assets to the Unity project");
        }
        path = path.Substring(startIndex);
            
        Undo.RecordObjects(renderersToUndo.ToArray(), "Swapped existing meshes with baked mesh.");
            
        for (int j = 0; j < renderTargetList.arraySize; j++) {
            SerializedProperty skinnedMeshRendererProp = renderTargetList.GetArrayElementAtIndex(j);
            SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRendererProp.objectReferenceValue as SkinnedMeshRenderer;
            if (skinnedMeshRenderer == null) {
                continue;
            }

            Mesh newMesh = Instantiate(skinnedMeshRenderer.sharedMesh);
            // Generate second mesh to insure properties are in bakespace before bake
            Mesh bakeMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(bakeMesh);
                
            List<Vector3> vertices = new List<Vector3>();
            List<Vector4> uvs = new List<Vector4>();
            bakeMesh.GetVertices(vertices);
            bakeMesh.GetUVs(2, uvs);
            // If we have no uvs, the array is empty. so we correct that by adding a bunch of zeros.
            for (int i=uvs.Count;i<vertices.Count;i++) {
                uvs.Add(Vector4.zero);
            }

            SerializedProperty penetrableTargetsProp = serializedObject.FindProperty("penetrableTargets");
            for (int i=0;i<uvs.Count;i++) {
                EditorUtility.DisplayProgressBar("Baking meshes...", $"Baking for mesh {newMesh.name}", (float)i / (float)uvs.Count);
                for(int o=0;o<penetrableTargetsProp.arraySize;o++) {
                    Penetrable p = penetrableTargetsProp.GetArrayElementAtIndex(o).objectReferenceValue as Penetrable;
                    if (p == null) {
                        throw new UnityException("Please make sure the Penetrables array doesn't have any nulls...");
                    }
                    Vector3 worldPosition = skinnedMeshRenderer.localToWorldMatrix.MultiplyPoint(vertices[i]);
                    CatmullSpline penPath = new CatmullSpline(p.GetPoints());
                    float nearestT = penPath.GetClosestTimeFromPosition(worldPosition, 256);
                    // Debug.DrawLine(worldPosition, penPath.GetPositionFromT(nearestT), Color.red, 10f);
                    switch(o) {
                        case 0: uvs[i] = new Vector4(nearestT,uvs[i].y,uvs[i].z,uvs[i].w);break;
                        case 1: uvs[i] = new Vector4(uvs[i].x,nearestT,uvs[i].z,uvs[i].w);break;
                        case 2: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,nearestT,uvs[i].w);break;
                        case 3: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,uvs[i].z,nearestT);break;
                        default: throw new UnityException("We only support up to 4 penetrables per procedural deformation...");
                    }
                }
            }
            newMesh.SetUVs(2, uvs);
            string meshPath = $"{path}/{newMesh.name}.mesh";
            AssetDatabase.CreateAsset(newMesh, meshPath);
            skinnedMeshRenderer.sharedMesh = newMesh;
            EditorUtility.SetDirty(skinnedMeshRenderer.sharedMesh);
        }
        serializedObject.ApplyModifiedProperties();
        EditorUtility.ClearProgressBar();
    }
}
#endif


public class PenetrableProcedural : MonoBehaviour {
    [SerializeField] private Penetrable[] penetrables;
    
    /*private ComputeBuffer penetratorBuffer;
    private ComputeBuffer splineBuffer;
    private NativeArray<PenetratorData> data;
    private NativeArray<PenetratorRenderers.CatmullSplineData> splineData;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int penetratorDataArrayID = Shader.PropertyToID("_PenetratorData");
    private static readonly int splineDataArrayID = Shader.PropertyToID("_CatmullSplines");
    private static readonly int dickGirthMapXID = Shader.PropertyToID("_DickGirthMapX");
    private static readonly int dickGirthMapYID = Shader.PropertyToID("_DickGirthMapY");
    private static readonly int dickGirthMapZID = Shader.PropertyToID("_DickGirthMapZ");
    private static readonly int dickGirthMapWID = Shader.PropertyToID("_DickGirthMapW");
    
    private unsafe struct PenetratorData {
        float blend;
        float worldDickLength;
        float worldDistance;
        float girthScaleFactor;
        float angle;
        fixed float initialRight[3];
        fixed float initialUp[3];
        int holeSubCurveCount;
        public PenetratorData(float blend) {
            this.blend = worldDickLength = worldDistance = girthScaleFactor = angle = blend;
            holeSubCurveCount = 0;
            initialRight[0] = 0;
            initialRight[1] = 0;
            initialRight[2] = 0;
            initialUp[0] = 0;
            initialUp[1] = 0;
            initialUp[2] = 0;
        }
        public PenetratorData(CatmullSpline penetrablePath, PenetratorData penetratorData, Penetrator penetrator, float worldDistance) {
            worldDickLength = penetrator.GetWorldLength();
            blend = worldDistance > worldDickLength ? 0f : 1f;
            this.worldDistance = worldDistance;
            girthScaleFactor = penetratorData.GetGirthScaleFactor();
            angle = penetrator.GetPenetratorAngleOffset();
            holeSubCurveCount = penetrablePath.GetWeights().Count;
            Vector3 iRight = penetrator.GetPath().GetBinormalFromT(0f);
            Vector3 iForward = penetrator.GetPath().GetVelocityFromT(0f).normalized;
            Vector3 iUp = Vector3.Cross(iForward, iRight).normalized;
            initialRight[0] = iRight.x;
            initialRight[1] = iRight.y;
            initialRight[2] = iRight.z;
            initialUp[0] = iUp.x;
            initialUp[1] = iUp.y;
            initialUp[2] = iUp.z;
        }
        public static int GetSize() {
            return sizeof(float)*11+sizeof(int)*1;
        }
    }*/
}
