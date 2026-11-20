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
    [SerializeField] private PenetratorCollisionSpheres penetratorCollisionSpheres;
    [SerializeField] private GirthLookup girthLookup;
    private BoneChainOnCurve _boneChainOnCurve;
    private BoneChainCompression _boneChainCompression;
    private List<Vector3> cachedPoints = new List<Vector3>();

#if UNITY_EDITOR
    [ContextMenu("Generate Girth Lookup")]
    private void GenerateGirthLookup() {
        var girthUnwrapShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath("34d24fe4568f98c4cae4724e139cb644"));
        var girthLookupTask = GirthLookup.GenerateGirthLookup(skinnedMeshRendererMask, girthUnwrapShader, bones, Vector3.zero, Vector3.up, -Vector3.forward, Vector3.right);
        Debug.Log("Attempting to generate lookups...");
        girthLookupTask.ContinueWith(task => {
            if (task.IsCompletedSuccessfully) {
                girthLookup = task.Result;
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
        var penetration = new Penetration(penetrator, penetrable, cachedPoints);
            
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
    }

    private void OnDrawGizmosSelected() {
        //penetratorCollisionSpheres.OnDrawGizmosSelected();
        if (skinnedMeshRendererMask.renderer == null) {
            return;
        }
        if (bones == null || bones.Length < 1 || bones[0] == null) {
            return;
        }
        var skinnedMeshRenderer = skinnedMeshRendererMask.renderer as SkinnedMeshRenderer;
        Matrix4x4 basis = new Matrix4x4();
        basis.SetRow(0, Vector3.right);
        basis.SetRow(1, -Vector3.forward);
        basis.SetRow(2, Vector3.up);
        basis[3, 3] = 1f;
        girthLookup.DrawGizmos(bones, skinnedMeshRenderer, basis.inverse);
    }

    private void OnValidate() {
        penetratorCollisionSpheres.OnValidate();
    }
}

}