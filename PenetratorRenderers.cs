using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
using Unity.Collections;
using UnityEngine;

[System.Serializable]
public class PenetratorRenderers {
    [SerializeField]
    private List<Renderer> renderers;
    
    private static readonly int catmullSplinesID = Shader.PropertyToID("_CatmullSplines");
    private static readonly int penetratorForwardID = Shader.PropertyToID("_PenetratorForwardWorld");
    private static readonly int penetratorRightID = Shader.PropertyToID("_PenetratorRightWorld");
    private static readonly int penetratorUpID = Shader.PropertyToID("_PenetratorUpWorld");
    private static readonly int penetratorRootID = Shader.PropertyToID("_PenetratorRootWorld");
    private static readonly int curveBlendID = Shader.PropertyToID("_CurveBlend");

    private ComputeBuffer catmullBuffer;
    private NativeArray<CatmullSplineData> data;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int penetratorOffsetLengthID = Shader.PropertyToID("_PenetratorOffsetLength");
    private static readonly int penetratorStartWorldID = Shader.PropertyToID("_PenetratorStartWorld");
    private static readonly int squashStretchCorrectionID = Shader.PropertyToID("_SquashStretchCorrection");
    private static readonly int distanceToHoleID = Shader.PropertyToID("_DistanceToHole");
    private static readonly int penetratorWorldLengthID = Shader.PropertyToID("_PenetratorWorldLength");
    private static readonly int truncateLengthID = Shader.PropertyToID("_TruncateLength");
    private static readonly int startClipID = Shader.PropertyToID("_StartClip");
    private static readonly int endClipID = Shader.PropertyToID("_EndClip");
    private static readonly int girthRadiusID = Shader.PropertyToID("_GirthRadius");


    public bool IsValid() {
        foreach (var renderer in renderers) {
            if (renderer == null) {
                return false;
            }
        }
        return true;
    }
    public void Initialize() {
        if (data.IsCreated) return; // Triggers leaks if we Initialize twice
        catmullBuffer = new ComputeBuffer(1, CatmullSplineData.GetSize());
        data = new NativeArray<CatmullSplineData>(1, Allocator.Persistent);
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Update(CatmullSpline spline, float penetratorLength, float squashAndStretch, float distanceToHole, float baseDistanceAlongSpline,
        Transform rootBone, Vector3 localRootForward, Vector3 localRootRight, Vector3 localRootUp, float truncateLength, float clippingStart, float clippingEnd, float truncateGirthRadius) {
        Initialize();
        data[0] = new CatmullSplineData(spline);
        catmullBuffer.SetData(data, 0, 0, 1);
        foreach(Renderer renderer in renderers) {
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(penetratorOffsetLengthID, baseDistanceAlongSpline);
            propertyBlock.SetVector(penetratorStartWorldID, spline.GetPositionFromDistance(baseDistanceAlongSpline));
            propertyBlock.SetFloat(curveBlendID, 1f);
            propertyBlock.SetVector(penetratorForwardID, rootBone.TransformDirection(localRootForward));
            propertyBlock.SetVector(penetratorRightID, rootBone.TransformDirection(localRootRight));
            propertyBlock.SetVector(penetratorUpID, rootBone.TransformDirection(localRootUp));
            propertyBlock.SetVector(penetratorRootID, rootBone.position);
            propertyBlock.SetBuffer(catmullSplinesID, catmullBuffer);
            propertyBlock.SetFloat(squashStretchCorrectionID, squashAndStretch);
            propertyBlock.SetFloat(penetratorWorldLengthID, penetratorLength);
            propertyBlock.SetFloat(distanceToHoleID, distanceToHole);
            propertyBlock.SetFloat(truncateLengthID, truncateLength);
            propertyBlock.SetFloat(girthRadiusID, truncateGirthRadius);
            propertyBlock.SetFloat(startClipID, clippingStart);
            propertyBlock.SetFloat(endClipID, clippingEnd);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void OnDestroy() {
        catmullBuffer.Release();
        data.Dispose();
        propertyBlock = null;
    }
    
}
