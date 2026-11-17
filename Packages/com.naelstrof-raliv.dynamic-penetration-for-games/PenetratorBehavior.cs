using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace GatorDragonGames {

public class PenetratorBehavior : MonoBehaviour {

    [SerializeField] private Transform[] bones;
    [SerializeField] private PenetrableBehavior penetrableBehavior;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private PenetratorCollisionSpheres penetratorCollisionSpheres;
    private BoneChainOnCurve _boneChainOnCurve;
    private BoneChainCompression _boneChainCompression;
    private List<Vector3> cachedPoints = new List<Vector3>();
    
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
        Penetrator penetrator = GetPenetrator();
        var penetrable = penetrableBehavior.GetPenetrable();
        var penetration = new Penetration(penetrator, penetrable, cachedPoints);
            
        var orificeDist = penetration.spline.GetDistanceFromTime(penetration.penetrableT);
        _boneChainCompression.Tick(orificeDist);
        var aimDist = _boneChainCompression.GetTipPosition();
        var aimPoint = penetration.spline.GetPositionFromDistance(aimDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        aimPoint = penetration.spline.GetPositionFromDistance(orificeDist);
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
        penetratorCollisionSpheres.OnDrawGizmosSelected();
    }

    private void OnValidate() {
        penetratorCollisionSpheres.OnValidate();
    }
}

}