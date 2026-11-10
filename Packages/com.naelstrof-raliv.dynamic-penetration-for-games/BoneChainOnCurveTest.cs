using UnityEngine;

public class BoneChainOnCurveTest : MonoBehaviour {
    
    [SerializeField] private Transform[] bones;
    [SerializeField] private CurveOfTransforms curveOfTransforms;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    
    private BoneChainOnCurve _boneChainOnCurve;
    private BoneChainCompression _boneChainCompression;

    private void OnEnable() {
        _boneChainOnCurve = new BoneChainOnCurve(bones);
        _boneChainCompression = new BoneChainCompression(_boneChainOnCurve.GetTotalLength());
    }
    
    private void LateUpdate() {
        var targetSpline = curveOfTransforms.GetCatmullSplineWithPrependedTransforms(new [] { bones[0] });
        var orificeDist = targetSpline.GetLengthFromSubsection(1,0);
        _boneChainCompression.Tick(orificeDist);
        var aimDist = _boneChainCompression.GetTipPosition();
        var aimPoint = targetSpline.GetPositionFromDistance(aimDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        aimPoint = targetSpline.GetPositionFromDistance(orificeDist);
        Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
        Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
        var lengthMultiplier = _boneChainCompression.GetTipPosition() / _boneChainOnCurve.GetRestLength();
        _boneChainOnCurve.AlignToSpline(targetSpline, lengthMultiplier);
        // get blend shape index by name
        var blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.002");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f/lengthMultiplier * 100f - 100f)*2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.003");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f/lengthMultiplier * 100f - 100f)*2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.004");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f/lengthMultiplier * 100f - 100f)*2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb.005");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f/lengthMultiplier * 100f - 100f)*2f);
        blendShapeIndex = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex("Throb");
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, (1f/lengthMultiplier * 100f - 100f)*2f);
    }
    
}
