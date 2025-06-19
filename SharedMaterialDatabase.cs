using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DPG {

// This dumb class is used to deal with the fact that Unity doesn't allow Keywords to be set on materials in editor without leaking them into the scene.
public class SharedMaterialDatabase : ScriptableObject {
    [SerializeField]
    private List<Material> trackedMaterials = new List<Material>();
    
    private static SharedMaterialDatabase sharedMaterialDatabase;
    
    public static SharedMaterialDatabase GetInstance() {
        if (sharedMaterialDatabase != null) {
            return sharedMaterialDatabase;
        }
        sharedMaterialDatabase = Resources.FindObjectsOfTypeAll<SharedMaterialDatabase>().FirstOrDefault();
#if UNITY_EDITOR
        if (sharedMaterialDatabase == null) {
            sharedMaterialDatabase = CreateInstance<SharedMaterialDatabase>();
            string path = "Assets/DPG/Resources/SharedMaterialDatabase.asset";
            if (!AssetDatabase.IsValidFolder("Assets/DPG")) {
                AssetDatabase.CreateFolder("Assets", "DPG");
            }
            if (!AssetDatabase.IsValidFolder("Assets/DPG/Resources")) {
                AssetDatabase.CreateFolder("Assets/DPG", "Resources");
            }
            AssetDatabase.CreateAsset(sharedMaterialDatabase, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(sharedMaterialDatabase);
            Debug.Log("Created DPG SharedMaterialDatabase at " + path);
        }
#endif
        return sharedMaterialDatabase;
    }
    
    private static ComputeBuffer nullCatmullBuffer;
    private static NativeArray<CatmullSplineData> nullData;
    
    private static readonly int catmullSplinesID = Shader.PropertyToID("_CatmullSplines");
    private static readonly int penetratorForwardID = Shader.PropertyToID("_PenetratorForwardWorld");
    private static readonly int penetratorRightID = Shader.PropertyToID("_PenetratorRightWorld");
    private static readonly int penetratorUpID = Shader.PropertyToID("_PenetratorUpWorld");
    private static readonly int penetratorRootID = Shader.PropertyToID("_PenetratorRootWorld");
    private static readonly int curveBlendID = Shader.PropertyToID("_CurveBlend");
    private static readonly int penetratorOffsetLengthID = Shader.PropertyToID("_PenetratorOffsetLength");
    private static readonly int penetratorStartWorldID = Shader.PropertyToID("_PenetratorStartWorld");
    private static readonly int squashStretchCorrectionID = Shader.PropertyToID("_SquashStretchCorrection");
    private static readonly int distanceToHoleID = Shader.PropertyToID("_DistanceToHole");
    private static readonly int truncateLengthID = Shader.PropertyToID("_TruncateLength");
    private static readonly int startClipID = Shader.PropertyToID("_StartClip");
    private static readonly int endClipID = Shader.PropertyToID("_EndClip");
    private static readonly int girthRadiusID = Shader.PropertyToID("_GirthRadius");

    private static readonly int DpgBlend = Shader.PropertyToID("_DPGBlend");
    
    private void Awake() {
        if (!nullData.IsCreated) {
            nullCatmullBuffer = new ComputeBuffer(1, CatmullSplineData.GetSize());
            nullData = new NativeArray<CatmullSplineData>(1, Allocator.Persistent);
            nullData[0] = new CatmullSplineData(new CatmullSpline(new List<Vector3>() { Vector3.zero, Vector3.one }));
            nullCatmullBuffer.SetData(nullData, 0, 0, 1);
        }

        if (!Application.isEditor) {
            // Allow the player to set this keyword per-material. Shared materials should default to off as non-deformed meshes won't have a script to help turn it off.
            foreach (var material in trackedMaterials) {
                material.DisableKeyword("_DPG_CURVE_SKINNING");
            }
        } else {
            // In the editor, we can't know if various shared materials have it enabled or not. In Vulkan, buffers aren't 0, therefore we must set some default values.
            foreach (var material in trackedMaterials) {
                material.EnableKeyword("_DPG_CURVE_SKINNING");
                material.SetFloat(penetratorOffsetLengthID, 0f);
                material.SetVector(penetratorStartWorldID, Vector3.zero);
                material.SetFloat(curveBlendID, 1f);
                material.SetVector(penetratorForwardID, Vector3.zero);
                material.SetVector(penetratorRightID, Vector3.zero);
                material.SetVector(penetratorUpID, Vector3.zero);
                material.SetVector(penetratorRootID, Vector3.zero);
                material.SetBuffer(catmullSplinesID, nullCatmullBuffer);
                material.SetFloat(squashStretchCorrectionID, 0f);
                material.SetFloat(DpgBlend, 0f);
                material.SetFloat(distanceToHoleID, 0f);
                material.SetFloat(truncateLengthID, 0f);
                material.SetFloat(girthRadiusID, 1f);
                material.SetFloat(startClipID, 0f);
                material.SetFloat(endClipID, 0f);
            }
            
        }
    }

    private void OnDestroy() {
        if (nullData.IsCreated) {
            nullData.Dispose();
            nullCatmullBuffer.Release();
        }
    }

    public void AddTrackedMaterial(Material material) {
        if (!trackedMaterials.Contains(material)) {
            trackedMaterials.Add(material);
        }
        if (!nullData.IsCreated) {
            nullCatmullBuffer = new ComputeBuffer(1, CatmullSplineData.GetSize());
            nullData = new NativeArray<CatmullSplineData>(1, Allocator.Persistent);
            nullData[0] = new CatmullSplineData(new CatmullSpline(new List<Vector3>() { Vector3.zero, Vector3.one }));
            nullCatmullBuffer.SetData(nullData, 0, 0, 1);
        }
        material.EnableKeyword("_DPG_CURVE_SKINNING");
        material.SetFloat(penetratorOffsetLengthID, 0f);
        material.SetVector(penetratorStartWorldID, Vector3.zero);
        material.SetFloat(curveBlendID, 1f);
        material.SetVector(penetratorForwardID, Vector3.zero);
        material.SetVector(penetratorRightID, Vector3.zero);
        material.SetVector(penetratorUpID, Vector3.zero);
        material.SetVector(penetratorRootID, Vector3.zero);
        material.SetBuffer(catmullSplinesID, nullCatmullBuffer);
        material.SetFloat(squashStretchCorrectionID, 0f);
        material.SetFloat(DpgBlend, 0f);
        material.SetFloat(distanceToHoleID, 0f);
        material.SetFloat(truncateLengthID, 0f);
        material.SetFloat(girthRadiusID, 1f);
        material.SetFloat(startClipID, 0f);
        material.SetFloat(endClipID, 0f);
    }
    private void OnValidate() {
        trackedMaterials.RemoveAll((m) => m == null);
    }
}

}
