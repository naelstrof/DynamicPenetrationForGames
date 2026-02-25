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

using System;
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
        SerializedProperty renderTargetList = serializedObject.FindProperty("targetRenderers");
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

            SerializedProperty penetrableTargetsProp = serializedObject.FindProperty("penetrables");
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
                    float normalizedDistance = penPath.GetDistanceFromTime(nearestT) / penPath.arcLength;
                    // Debug.DrawLine(worldPosition, penPath.GetPositionFromT(nearestT), Color.red, 10f);
                    switch(o) {
                        case 0: uvs[i] = new Vector4(normalizedDistance,uvs[i].y,uvs[i].z,uvs[i].w);break;
                        case 1: uvs[i] = new Vector4(uvs[i].x,normalizedDistance,uvs[i].z,uvs[i].w);break;
                        case 2: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,normalizedDistance,uvs[i].w);break;
                        case 3: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,uvs[i].z,normalizedDistance);break;
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


[ExecuteAlways]
public class PenetrableProcedural : MonoBehaviour {
    [SerializeField] private List<Penetrable> penetrables;
    [SerializeField] private List<Renderer> targetRenderers;
    [SerializeField] private bool detailOnly;
    
    private ComputeBuffer penetratorBuffer;
    private ComputeBuffer splineBuffer;
    private NativeArray<PenetratorData> data;
    private NativeArray<CatmullSplineData> splineData;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int penetratorDataArrayID = Shader.PropertyToID("_PenetratorData");
    private static readonly int splineDataArrayID = Shader.PropertyToID("_CatmullSplines");
    private static readonly int penetratorGirthMapXID = Shader.PropertyToID("_PenetratorGirthMapX");
    private static readonly int penetratorGirthMapYID = Shader.PropertyToID("_PenetratorGirthMapY");
    private static readonly int penetratorGirthMapZID = Shader.PropertyToID("_PenetratorGirthMapZ");
    private static readonly int penetratorGirthMapWID = Shader.PropertyToID("_PenetratorGirthMapW");
    private bool? setKeyword;
    
    private unsafe struct PenetratorData {
        float squashStretch;
        float blend;
        float worldDickLength;
        float worldDistance;
        float girthScaleFactor;
        float angle;
        fixed float initialRight[3];
        fixed float initialUp[3];
        public PenetratorData(float blend) {
            squashStretch = 1f;
            this.blend = worldDickLength = worldDistance = girthScaleFactor = angle = blend;
            initialRight[0] = 0;
            initialRight[1] = 0;
            initialRight[2] = 0;
            initialUp[0] = 0;
            initialUp[1] = 0;
            initialUp[2] = 0;
        }
        public PenetratorData(CatmullSpline penetrablePath, CatmullSpline penetratorPath, float worldDistance, float worldDickLength, float squashStretch, float girthScaleFactor, float angleOffset) {
            this.squashStretch = squashStretch;
            this.worldDickLength = worldDickLength;
            blend = worldDistance > worldDickLength ? 0f : 1f;
            this.worldDistance = worldDistance;
            this.girthScaleFactor = girthScaleFactor;
            angle = angleOffset;
            Vector3 iRight = penetrablePath.GetBinormalFromT(0f);
            Vector3 iForward = penetrablePath.GetVelocityFromT(0f).normalized;
            Vector3 iUp = Vector3.Cross(iForward, iRight).normalized;
            initialRight[0] = iRight.x;
            initialRight[1] = iRight.y;
            initialRight[2] = iRight.z;
            initialUp[0] = iUp.x;
            initialUp[1] = iUp.y;
            initialUp[2] = iUp.z;
        }
        public static int GetSize() {
            return sizeof(float)*12;
        }
    }

    public void AddTargetRenderer(Renderer renderer) {
        if (targetRenderers.Contains(renderer)) {
            return;
        }
        targetRenderers.Add(renderer);
        foreach (Material sharedMat in Application.isPlaying ? renderer.materials : renderer.sharedMaterials) {
            if (setKeyword ?? false) {
                sharedMat.EnableKeyword("_PENETRATION_DEFORMATION_ON");
            } else {
                sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_ON");
            }
            if (detailOnly) {
                sharedMat.EnableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
            } else {
                sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
            }
        }
    }

    public void GetTargetRenderers(IList<Renderer> renderers) {
        renderers.Clear();
        foreach (var renderer in targetRenderers) {
            renderers.Add(renderer);
        }
    }

    public void RemoveTargetRenderer(Renderer renderer) {
        targetRenderers.Remove(renderer);
        foreach (Material sharedMat in Application.isPlaying ? renderer.materials : renderer.sharedMaterials) {
            sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_ON");
            sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
        }
    }

    private void OnEnable() {
        Initialize();
    }

    private void OnDisable() {
        if (!data.IsCreated) {
            return;
        }
        penetratorBuffer.Dispose();
        splineBuffer.Dispose();
        splineData.Dispose();
        data.Dispose();
    }

    private void SetKeyword(bool enableKeyword) {
        if (setKeyword == enableKeyword) {
            return;
        }
        foreach (Renderer ren in targetRenderers) {
            Material[] mats = Application.isPlaying ? ren.materials : ren.sharedMaterials;

            foreach (Material sharedMat in mats) {
                if (enableKeyword) {
                    sharedMat.EnableKeyword("_PENETRATION_DEFORMATION_ON");
                } else {
                    sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_ON");
                }

                if (detailOnly) {
                    sharedMat.EnableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
                } else {
                    sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
                }
            }
        }
        setKeyword = enableKeyword;
    }
    
    private void Initialize() {
        if (data.IsCreated || penetrables == null || penetrables.Count == 0 || targetRenderers == null || targetRenderers.Count == 0) {
            return;
        }

        penetratorBuffer = new ComputeBuffer(4,PenetratorData.GetSize());
        data = new NativeArray<PenetratorData>(4, Allocator.Persistent);
        splineBuffer = new ComputeBuffer(4,CatmullSplineData.GetSize());
        splineData = new NativeArray<CatmullSplineData>(4, Allocator.Persistent);
        
        CatmullSpline d = new CatmullSpline(new List<Vector3> { Vector3.zero, Vector3.one });
        for (int i=0;i<4;i++) {
            data[i] = new PenetratorData(0);
            splineData[i] = new CatmullSplineData(d);
        }
        

        propertyBlock = new MaterialPropertyBlock();

        foreach (Penetrable penetrable in penetrables) {
            if (penetrable == null) {
                continue;
            }
            penetrable.penetrated -= NotifyPenetration;
            penetrable.penetrated += NotifyPenetration;
            penetrable.unpenetrated -= NotifyUnpenetration;
            penetrable.unpenetrated += NotifyUnpenetration;
        }
        SetKeyword(false);
        foreach (Renderer target in targetRenderers) {
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(penetratorGirthMapYID, Texture2D.blackTexture);
            propertyBlock.SetTexture(penetratorGirthMapZID, Texture2D.blackTexture);
            propertyBlock.SetTexture(penetratorGirthMapWID, Texture2D.blackTexture);
            propertyBlock.SetBuffer(splineDataArrayID, splineBuffer);
            propertyBlock.SetBuffer(penetratorDataArrayID, penetratorBuffer);
            target.SetPropertyBlock(propertyBlock);
        }
    }

    private void NotifyUnpenetration(Penetrable penetrable, Penetrator penetrator) {
        Initialize();
        int index = penetrables.IndexOf(penetrable);
        foreach (Renderer target in targetRenderers) {
            target.GetPropertyBlock(propertyBlock);
            Texture targetTexture = detailOnly ? Texture2D.grayTexture : Texture2D.blackTexture;
            switch (index) {
                case 0: propertyBlock.SetTexture(penetratorGirthMapXID, targetTexture); break;
                case 1: propertyBlock.SetTexture(penetratorGirthMapYID, targetTexture); break;
                case 2: propertyBlock.SetTexture(penetratorGirthMapZID, targetTexture); break;
                case 3: propertyBlock.SetTexture(penetratorGirthMapWID, targetTexture); break;
            }
            target.SetPropertyBlock(propertyBlock);
        }
    }

    private void NotifyPenetration(Penetrable penetrable, Penetrator.PenetrationArgs penetrationArgs) {
        Initialize();
        SetKeyword(true);
        
        int index = penetrables.IndexOf(penetrable);

        float diff = penetrationArgs.penetratorData.GetWorldLength() - penetrationArgs.penetratorFinalWorldLength;
        var penetrableSpline = new CatmullSpline(penetrable.GetPoints());
        data[index] = new PenetratorData(
            penetrableSpline,
            penetrationArgs.alongSpline,
            penetrationArgs.penetratorFinalWorldLength-penetrationArgs.penetrationDepth+diff,
            penetrationArgs.penetratorData.GetWorldLength(),
            penetrationArgs.penetratorStretchFactor,
            penetrationArgs.penetratorData.GetGirthScaleFactor(),
            Penetrator.GetPenetratorAngleOffset(penetrationArgs.alongSpline,penetrationArgs.worldPenetratorUp)
            );
        splineData[index] = new CatmullSplineData(penetrableSpline);
        penetratorBuffer.SetData(data);
        splineBuffer.SetData(splineData);

        foreach (Renderer target in targetRenderers) {
            target.GetPropertyBlock(propertyBlock);
            Texture targetTexture = detailOnly ? penetrationArgs.penetratorData.GetDetailMap() : penetrationArgs.penetratorData.GetGirthMap();
            switch (index) {
                case 0: propertyBlock.SetTexture(penetratorGirthMapXID, targetTexture); break;
                case 1: propertyBlock.SetTexture(penetratorGirthMapYID, targetTexture); break;
                case 2: propertyBlock.SetTexture(penetratorGirthMapZID, targetTexture); break;
                case 3: propertyBlock.SetTexture(penetratorGirthMapWID, targetTexture); break;
            }
            propertyBlock.SetBuffer(splineDataArrayID, splineBuffer);
            propertyBlock.SetBuffer(penetratorDataArrayID, penetratorBuffer);
            target.SetPropertyBlock(propertyBlock);
        }
    }
}

}