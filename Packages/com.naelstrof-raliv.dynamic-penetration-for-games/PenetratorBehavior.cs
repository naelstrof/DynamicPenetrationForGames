using System.Collections.Generic;
using DPG;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GatorDragonGames {

public class PenetratorBehavior : MonoBehaviour {
    [SerializeField] private Transform[] bones;
    [SerializeField] private PenetrableBehavior penetrableBehavior;
    [SerializeField] private RendererSubMeshMask skinnedMeshRendererMask;
    [SerializeField] private SkinnedMeshGirth skinnedMeshGirth;
    [SerializeField] private BoneChainOnCurve boneChainOnCurve;
    [SerializeField] private BoneChainCompression boneChainCompression;
    private List<Vector3> cachedPoints = new List<Vector3>();
    private Transform[] rendererTransforms;

    private Penetration penetration;
    private GirthFrame penetrationGirthFrame;
    private SkinnedMeshGirth.SkinnedMeshGirthSampler skinnedMeshGirthSampler;
    
    // TODO: Maybe splines should be a struct??
    private Spline3D reusableSpline3D = new Spline3D();

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
            penetratorLength = boneChainOnCurve.GetTotalLength()
        };
    }

    private void OnEnable() {
        boneChainOnCurve = new BoneChainOnCurve(bones);
        boneChainCompression = new BoneChainCompression(boneChainOnCurve.GetTotalLength());
        skinnedMeshGirthSampler = new SkinnedMeshGirth.SkinnedMeshGirthSampler(skinnedMeshRendererMask.renderer);
        penetrationGirthFrame = new GirthFrame(1f, 0.1f);
    }

    private void LateUpdate() {
        var skinnedMeshRenderer = skinnedMeshRendererMask.renderer;
        Penetrator penetrator = GetPenetrator();
        var penetrable = penetrableBehavior.GetPenetrable();
        penetration = new Penetration(penetrator, penetrable, cachedPoints, reusableSpline3D);
            
        var orificeDist = penetration.spline3D.GetDistanceFromTime(penetration.penetrableT);
        boneChainCompression.Tick(orificeDist);
        var aimDist = boneChainCompression.GetTipPosition();
        var aimPoint = penetration.spline3D.GetPositionFromDistance(aimDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        aimPoint = penetration.spline3D.GetPositionFromDistance(orificeDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        var lengthMultiplier = boneChainCompression.GetTipPosition() / boneChainOnCurve.GetRestLength();
        penetration.Render(boneChainOnCurve, lengthMultiplier);
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
        
        skinnedMeshGirth.GetGirthFrame(ref penetrationGirthFrame, skinnedMeshRenderer, skinnedMeshGirthSampler, bones[0]);
        PenetratorCollisionSpheresAuto.Render(penetrableBehavior.GetRenderers(), penetrationGirthFrame,0.25f, 0.5f, 32);
    }

    private void OnDrawGizmosSelected() {
        if (Application.isPlaying) {
            if (penetration.spline3D == null) return;
            if (bones == null || bones.Length < 1 || bones[0] == null) return;
            penetrationGirthFrame.DrawGizmos(256);
        } else {
            penetrationGirthFrame.OnValidate();
            if (skinnedMeshRendererMask.renderer == null) return;
            if (bones == null || bones.Length < 1 || bones[0] == null) return;
            skinnedMeshGirth.DrawGizmos(ref penetrationGirthFrame, skinnedMeshRendererMask.renderer, new SkinnedMeshGirth.SkinnedMeshGirthSampler(skinnedMeshRendererMask.renderer), bones[0], 256);
        }
    }
}

}