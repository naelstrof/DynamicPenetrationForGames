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
    private static readonly int dickForwardID = Shader.PropertyToID("_DickForwardWorld");
    private static readonly int dickRightID = Shader.PropertyToID("_DickRightWorld");
    private static readonly int dickUpID = Shader.PropertyToID("_DickUpWorld");
    private static readonly int dickRootID = Shader.PropertyToID("_DickRootWorld");
    private static readonly int curveBlendID = Shader.PropertyToID("_CurveBlend");

    private ComputeBuffer catmullBuffer;
    private NativeArray<CatmullSplineData> data;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int dickOffsetLengthID = Shader.PropertyToID("_DickOffsetLength");
    private static readonly int dickStartWorldID = Shader.PropertyToID("_DickStartWorld");

    private unsafe struct CatmullSplineData {
        private const int subSplineCount = 8;
        private const int binormalCount = CatmullSpline.BINORMAL_LUT_COUNT;
        private const int distanceCount = 32;
        int pointCount;
        float arcLength;
        fixed float weights[subSplineCount*4*4];
        fixed float distanceLUT[distanceCount];
        fixed float binormalLUT[binormalCount*3];
        public CatmullSplineData(CatmullSpline spline) {
            pointCount = spline.GetWeights().Count+1;
            arcLength = spline.arcLength;
            for(int i=0;i<subSplineCount&&i<spline.GetWeights().Count;i++) {
                Vector4 row1 = spline.GetWeights()[i].GetRow(0);
                Vector4 row2 = spline.GetWeights()[i].GetRow(1);
                Vector4 row3 = spline.GetWeights()[i].GetRow(2);
                Vector4 row4 = spline.GetWeights()[i].GetRow(3);
                weights[i*16] = row1.x;
                weights[i*16+1] = row1.y;
                weights[i*16+2] = row1.z;
                weights[i*16+3] = row1.w;
                
                weights[i*16+4] = row2.x;
                weights[i*16+5] = row2.y;
                weights[i*16+6] = row2.z;
                weights[i*16+7] = row2.w;
                
                weights[i*16+8] = row3.x;
                weights[i*16+9] = row3.y;
                weights[i*16+10] = row3.z;
                weights[i*16+11] = row3.w;
                
                weights[i*16+12] = row4.x;
                weights[i*16+13] = row4.y;
                weights[i*16+14] = row4.z;
                weights[i*16+15] = row4.w;
            }
            UnityEngine.Assertions.Assert.AreEqual(spline.GetDistanceLUT().Length, distanceCount);
            for(int i=0;i<distanceCount;i++) {
                distanceLUT[i] = spline.GetDistanceLUT()[i];
            }
            UnityEngine.Assertions.Assert.AreEqual(spline.GetBinormalLUT().Length, binormalCount);
            for(int i=0;i<binormalCount;i++) {
                Vector3 binormal = spline.GetBinormalLUT()[i];
                binormalLUT[i*3] = binormal.x;
                binormalLUT[i*3+1] = binormal.y;
                binormalLUT[i*3+2] = binormal.z;
            }
        }
        public static int GetSize() {
            return sizeof(float)*(subSplineCount*16+1+binormalCount*3+distanceCount) + sizeof(int);
        }
    }

    public void Initialize() {
        if (data.IsCreated) return; // Triggers leaks if we Initialize twice
        catmullBuffer = new ComputeBuffer(1, CatmullSplineData.GetSize());
        data = new NativeArray<CatmullSplineData>(1, Allocator.Persistent);
        propertyBlock = new MaterialPropertyBlock();
    }

    public void Update(CatmullSpline spline, float baseDistanceAlongSpline, Transform rootBone, Vector3 localRootForward, Vector3 localRootRight, Vector3 localRootUp) {
        data[0] = new CatmullSplineData(spline);
        catmullBuffer.SetData(data, 0, 0, 1);
        foreach(Renderer renderer in renderers) {
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(dickOffsetLengthID, baseDistanceAlongSpline);
            propertyBlock.SetVector(dickStartWorldID, spline.GetPositionFromDistance(baseDistanceAlongSpline));
            propertyBlock.SetFloat(curveBlendID, 1f);
            propertyBlock.SetVector(dickForwardID, rootBone.TransformDirection(localRootForward));
            propertyBlock.SetVector(dickRightID, rootBone.TransformDirection(localRootRight));
            propertyBlock.SetVector(dickUpID, rootBone.TransformDirection(localRootUp));
            propertyBlock.SetVector(dickRootID, rootBone.position);
            propertyBlock.SetBuffer(catmullSplinesID, catmullBuffer);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void OnDestroy() {
        catmullBuffer.Release();
        data.Dispose();
        propertyBlock = null;
    }
    
}
