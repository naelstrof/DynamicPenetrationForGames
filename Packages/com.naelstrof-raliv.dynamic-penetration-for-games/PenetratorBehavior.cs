using System.Collections.Generic;
using DPG;
using UnityEngine;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames {

public class PenetratorBehavior : MonoBehaviour {
    [SerializeField] private Transform[] bones;
    [SerializeField] private PenetrableBehavior penetrableBehavior;
    [SerializeField] private RendererSubMeshMask skinnedMeshRendererMask;
    [SerializeField] private PenetratorCollisionSpheres penetratorCollisionSpheres;
    [FormerlySerializedAs("girthLookup")] [SerializeField] private SkinnedMeshGirth skinnedMeshGirth;
    private BoneChainOnCurve _boneChainOnCurve;
    private BoneChainCompression _boneChainCompression;
    private List<Vector3> cachedPoints = new List<Vector3>();

    private Penetration penetration;
    private GirthFrame penetrationGirthFrame;

#if UNITY_EDITOR
    [ContextMenu("Generate Girth Lookup")]
    private void GenerateGirthLookup() {
        Matrix4x4 bindPose = GirthFrame.GetDefaultPenetratorPose();
        var girthUnwrapShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("34d24fe4568f98c4cae4724e139cb644"));
        var girthLookupTask = SkinnedMeshGirth.GenerateGirthLookup(skinnedMeshRendererMask, girthUnwrapShader, bones, bindPose);
        Debug.Log("Attempting to generate lookups...");
        girthLookupTask.ContinueWith(task => {
            if (task.IsCompletedSuccessfully) {
                skinnedMeshGirth = task.Result;
                Debug.Log("Girth lookup generated successfully.");
            } else {
                Debug.LogError("Failed to generate girth lookup: " + task.Exception);
            }
        });
    }
#endif
    
    private Penetrator GetPenetrator() {
        return new Penetrator() {
            position = bones[0].position,
            direction = (bones[1].position - bones[0].position).normalized,
            penetratorLength = _boneChainOnCurve.GetTotalLength()
        };
    }

    private void OnEnable() {
        _boneChainOnCurve = new BoneChainOnCurve(bones);
        _boneChainCompression = new BoneChainCompression(_boneChainOnCurve.GetTotalLength());
        penetratorCollisionSpheres.Initialize(GetPenetrator());
    }
    

    private void LateUpdate() {
        var skinnedMeshRenderer = skinnedMeshRendererMask.renderer as SkinnedMeshRenderer;
        Penetrator penetrator = GetPenetrator();
        var penetrable = penetrableBehavior.GetPenetrable();
        penetration = new Penetration(penetrator, penetrable, cachedPoints);
            
        var orificeDist = penetration.spline3D.GetDistanceFromTime(penetration.penetrableT);
        _boneChainCompression.Tick(orificeDist);
        var aimDist = _boneChainCompression.GetTipPosition();
        var aimPoint = penetration.spline3D.GetPositionFromDistance(aimDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        aimPoint = penetration.spline3D.GetPositionFromDistance(orificeDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        var lengthMultiplier = _boneChainCompression.GetTipPosition() / _boneChainOnCurve.GetRestLength();
        penetration.Render(_boneChainOnCurve, lengthMultiplier);
        // get blend shape index by name
        var blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.002");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.003");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.004");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.005");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f / lengthMultiplier * 100f - 100f) * 2f);
        
        penetratorCollisionSpheres.Render(penetrableBehavior.GetRenderers());
        
        penetrationGirthFrame = skinnedMeshGirth.GetGirthFrame(skinnedMeshRenderer, bones[0]);
    }

    private void OnDrawGizmosSelected() {
        if (Application.isPlaying) {
            if (penetration.spline3D == null) return;
            if (bones == null || bones.Length < 1 || bones[0] == null) return;
            penetrationGirthFrame.DrawGizmos(bones[0].localToWorldMatrix, 256);
        } else {
            if (skinnedMeshRendererMask.renderer == null) return;
            if (bones == null || bones.Length < 1 || bones[0] == null) return;
            skinnedMeshGirth.GetGirthFrame(skinnedMeshRendererMask.renderer, bones[0]).DrawGizmos(bones[0].localToWorldMatrix, 256);
        }
    }

    private void OnValidate() {
        penetratorCollisionSpheres.OnValidate();
    }
}

}