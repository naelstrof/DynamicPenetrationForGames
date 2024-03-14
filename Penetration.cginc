#define SUB_SPLINE_COUNT 8 
#define BINORMAL_COUNT 32
#define DISTANCE_COUNT 32

struct CatmullSplineData {
    int pointCount;
    float arcLength;
    float weightArray[SUB_SPLINE_COUNT*4*4];
    float distanceLUT[DISTANCE_COUNT];
    float binormalLUT[BINORMAL_COUNT*3];
};

// FIXME: I'm not actually sure this can even compile on mobile platforms. We need to double check.
// Thoeretically there's no reason to use dynamic buffers like this (we should have static spline counts anyway).
// But this was the most convienient way I could think of for the programming side of things.
#pragma target 5.0

#if (defined(UNITY_COMPILER_CG) && (defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)) || !defined(UNITY_COMPILER_CG) && !defined(SHADER_API_D3D11_9X))
StructuredBuffer<CatmullSplineData> _CatmullSplines;
#else
CatmullSplineData _CatmullSplines[4];
#endif

float3 GetBinormalFromT(int curveIndex, float t) {
    int count = BINORMAL_COUNT;
    int index = floor(t*(float)(count-1));
    float offseted = t-((float)index/(float)(count-1));
    float lerpT = offseted * (float)(count-1);
    float3 a = float3(_CatmullSplines[curveIndex].binormalLUT[index*3],_CatmullSplines[curveIndex].binormalLUT[index*3+1], _CatmullSplines[curveIndex].binormalLUT[index*3+2]);
    float3 b = float3(_CatmullSplines[curveIndex].binormalLUT[(index+1)*3],_CatmullSplines[curveIndex].binormalLUT[(index+1)*3+1], _CatmullSplines[curveIndex].binormalLUT[(index+1)*3+2]);
    return lerp(a, b, lerpT);
}
float GetCurveSegment(int curveIndex, float t, out int curveSegmentIndex) {
    curveSegmentIndex = clamp((int)floor(t*(_CatmullSplines[curveIndex].pointCount-1)),0,_CatmullSplines[curveIndex].pointCount-1);
    float offset = t-((float)curveSegmentIndex/(float)(_CatmullSplines[curveIndex].pointCount-1));
    return offset * (float)(_CatmullSplines[curveIndex].pointCount-1);
}

float GetTFromSubT(int curveIndex, int start, int end, float subT) {
    int subSplineCount = _CatmullSplines[curveIndex].pointCount-1;
    int subSection = end - start;
    float multi = (float)subSection / (float)subSplineCount;
    float startT = (float)start / (float)subSplineCount;
    return subT * multi + startT;
}
float TimeToDistance(int curveIndex, float t) {
    t = saturate(t);
    int index = clamp(floor(t*(DISTANCE_COUNT-1)),0,DISTANCE_COUNT-2);
    float offseted = t-((float)index/(float)(DISTANCE_COUNT-1));
    float lerpT = offseted * (float)(DISTANCE_COUNT-1);
    return lerp(_CatmullSplines[curveIndex].distanceLUT[index], _CatmullSplines[curveIndex].distanceLUT[index+1], lerpT);
}
float DistanceToTime(int curveIndex, float distance) {
    if (distance > 0 && distance < _CatmullSplines[curveIndex].arcLength) {
        for(int i=0;i<DISTANCE_COUNT-1;i++) {
            if (distance>_CatmullSplines[curveIndex].distanceLUT[i] && distance<_CatmullSplines[curveIndex].distanceLUT[i+1]) {
                // Remap
                float from1 = _CatmullSplines[curveIndex].distanceLUT[i];
                float to1 = _CatmullSplines[curveIndex].distanceLUT[i+1];
                float from2 = (float)i/(float)(DISTANCE_COUNT-1);
                float to2 = (float)(i+1)/(float)(DISTANCE_COUNT-1);
                return (distance - from1) / (to1 - from1) * (to2 - from2) + from2;
            }
        }
    }
    return distance/_CatmullSplines[curveIndex].arcLength;
}
float GetVectorAngle(float3 a, float3 b) {
    return acos(dot(a,b));
}
float3 RotateAroundAxisPenetration(float3 original, float3 axis, float angle ) {
    float C = cos( angle );
    float S = sin( angle );
    float t = 1 - C;
    float m00 = t * axis.x * axis.x + C;
    float m01 = t * axis.x * axis.y - S * axis.z;
    float m02 = t * axis.x * axis.z + S * axis.y;
    float m10 = t * axis.x * axis.y + S * axis.z;
    float m11 = t * axis.y * axis.y + C;
    float m12 = t * axis.y * axis.z - S * axis.x;
    float m20 = t * axis.x * axis.z - S * axis.y;
    float m21 = t * axis.y * axis.z + S * axis.x;
    float m22 = t * axis.z * axis.z + C;
    float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
    return mul( finalMatrix, original );
}
float3 SampleCurveSegmentPosition(int curveIndex, int curveSegmentIndex, float t) {
    int index = curveSegmentIndex*4*4;
    const float4x4 mat = float4x4(
        _CatmullSplines[curveIndex].weightArray[index],_CatmullSplines[curveIndex].weightArray[index+1],_CatmullSplines[curveIndex].weightArray[index+2],_CatmullSplines[curveIndex].weightArray[index+3],
        _CatmullSplines[curveIndex].weightArray[index+4],_CatmullSplines[curveIndex].weightArray[index+5],_CatmullSplines[curveIndex].weightArray[index+6],_CatmullSplines[curveIndex].weightArray[index+7],
        _CatmullSplines[curveIndex].weightArray[index+8],_CatmullSplines[curveIndex].weightArray[index+9],_CatmullSplines[curveIndex].weightArray[index+10],_CatmullSplines[curveIndex].weightArray[index+11],
        _CatmullSplines[curveIndex].weightArray[index+12],_CatmullSplines[curveIndex].weightArray[index+13],_CatmullSplines[curveIndex].weightArray[index+14],_CatmullSplines[curveIndex].weightArray[index+15]);
    return mul(mat,float4(1,t,t*t,t*t*t)).xyz;
}
float3 SampleCurveSegmentVelocity(int curveIndex, int curveSegmentIndex, float t) {
    int index = curveSegmentIndex*4*4;
    const float4x4 mat = float4x4(
        _CatmullSplines[curveIndex].weightArray[index],_CatmullSplines[curveIndex].weightArray[index+1],_CatmullSplines[curveIndex].weightArray[index+2],_CatmullSplines[curveIndex].weightArray[index+3],
        _CatmullSplines[curveIndex].weightArray[index+4],_CatmullSplines[curveIndex].weightArray[index+5],_CatmullSplines[curveIndex].weightArray[index+6],_CatmullSplines[curveIndex].weightArray[index+7],
        _CatmullSplines[curveIndex].weightArray[index+8],_CatmullSplines[curveIndex].weightArray[index+9],_CatmullSplines[curveIndex].weightArray[index+10],_CatmullSplines[curveIndex].weightArray[index+11],
        _CatmullSplines[curveIndex].weightArray[index+12],_CatmullSplines[curveIndex].weightArray[index+13],_CatmullSplines[curveIndex].weightArray[index+14],_CatmullSplines[curveIndex].weightArray[index+15]);
    return mul(mat,float4(0,1,2*t,3*t*t)).xyz;
}
void ToCatmullRomSpace_float(float3 worldPenetratorRootPos, float3 worldPosition, float3 worldPenetratorForward, float3 worldPenetratorUp, float3 worldPenetratorRight, float3 worldNormal, float4 worldTangent, out float3 worldPositionOUT, out float3 worldNormalOUT, out float4 worldTangentOUT) {
    // We want to work in world space, as everything we're working with is there.
    
    // Dot product gives us how far along an axis a position is. This is the penetrator length distance from the penetrator root to the particular position.
    float preDist = dot(worldPenetratorForward,(worldPosition - worldPenetratorRootPos));
    float dist = max(preDist,0);

    // Convert the distance into an overall t sample value
    float t = DistanceToTime(0,dist);
    float isPenetrator = saturate(sign(preDist));
    // Since our t sample value is based on a piece-wise curve, we need to figure out which curve weights we're meant to sample.
    int curveSegmentIndex = 0;
    float subT = GetCurveSegment(0, t, curveSegmentIndex);

    float3 catPosition = SampleCurveSegmentPosition(0,curveSegmentIndex, subT);
    float3 catTangent = SampleCurveSegmentVelocity(0,curveSegmentIndex, subT);
    float3 catForward = normalize(catTangent);
    // We sample the Binormal from a lookup-table, to prevent flipping and twisting.
    // https://en.wikipedia.org/wiki/Frenet%E2%80%93Serret_formulas
    // https://janakiev.com/blog/framing-parametric-curves/
    float3 catRight = GetBinormalFromT(0,t);
    // We can just figure out our normal with a cross product.
    float3 catUp = normalize(cross(catForward,catRight));

    float3 initialRight = GetBinormalFromT(0,0);
    float3 initialForward = normalize(SampleCurveSegmentVelocity(0,0,0));
    float3 initialUp = normalize(cross(initialForward, initialRight));


    // Change of basis https://math.stackexchange.com/questions/3540973/change-of-coordinates-and-change-of-basis-matrices
    // It also shows up here: https://docs.unity3d.com/ScriptReference/Vector3.OrthoNormalize.html
    // Goes from penetrator space into catmull rom space.
    float3x3 penetratorToCatmullBasisTransform = 0;
    penetratorToCatmullBasisTransform[0][0] = catRight.x;
    penetratorToCatmullBasisTransform[0][1] = catRight.y;
    penetratorToCatmullBasisTransform[0][2] = catRight.z;
    penetratorToCatmullBasisTransform[1][0] = catUp.x;
    penetratorToCatmullBasisTransform[1][1] = catUp.y;
    penetratorToCatmullBasisTransform[1][2] = catUp.z;
    penetratorToCatmullBasisTransform[2][0] = catForward.x;
    penetratorToCatmullBasisTransform[2][1] = catForward.y;
    penetratorToCatmullBasisTransform[2][2] = catForward.z;
    penetratorToCatmullBasisTransform = transpose(penetratorToCatmullBasisTransform);

    // Goes from XYZ world space, into dX,dY,dZ space (where dX,dY,dZ are penetrator orientations.)
    float3x3 worldToPenetratorBasisTransform = 0;
    worldToPenetratorBasisTransform[0][0] = worldPenetratorRight.x;
    worldToPenetratorBasisTransform[0][1] = worldPenetratorRight.y;
    worldToPenetratorBasisTransform[0][2] = worldPenetratorRight.z;
    worldToPenetratorBasisTransform[1][0] = worldPenetratorUp.x;
    worldToPenetratorBasisTransform[1][1] = worldPenetratorUp.y;
    worldToPenetratorBasisTransform[1][2] = worldPenetratorUp.z;
    worldToPenetratorBasisTransform[2][0] = worldPenetratorForward.x;
    worldToPenetratorBasisTransform[2][1] = worldPenetratorForward.y;
    worldToPenetratorBasisTransform[2][2] = worldPenetratorForward.z;

    // Get the rotation around penetratorforward that we need to do.
    float2 worldPenetratorUpFlat = float2(dot(worldPenetratorUp,initialRight), dot(worldPenetratorUp,initialUp));
    float angle = atan2(worldPenetratorUpFlat.y, worldPenetratorUpFlat.x)-1.57079632679;

    // Frame refers to the particular slice of the model we're working on, normals don't really have anything special about them in the frame.
    float3 worldFrameNormal = worldNormal;
    float3 localFrameNormal = mul(worldToPenetratorBasisTransform, worldFrameNormal.xyz).xyz;
    float3 worldFrameNormalRotated = mul(penetratorToCatmullBasisTransform, localFrameNormal.xyz);
    worldFrameNormalRotated = RotateAroundAxisPenetration(worldFrameNormalRotated, catForward, angle);
    worldNormalOUT = lerp(worldNormal, normalize(worldFrameNormalRotated), isPenetrator);

    float3 worldFrameTangent = worldTangent;
    float3 localFrameTangent = mul(worldToPenetratorBasisTransform, worldFrameTangent.xyz).xyz;
    float3 worldFrameTangentRotated = mul(penetratorToCatmullBasisTransform, localFrameTangent.xyz).xyz;
    worldFrameTangentRotated = RotateAroundAxisPenetration(worldFrameTangentRotated, catForward, angle);
    worldTangentOUT = lerp(worldTangent, float4(normalize(worldFrameTangentRotated).xyz, worldTangent.w), isPenetrator);

    // Frame refers to the particular slice of the model we're working on, 0,0,0 being the core of the cylinder.
    float3 worldFrame = (worldPosition - (worldPenetratorRootPos+worldPenetratorForward*dist));
    // Rotate into penetrator space, using the basis transform
    float3 localFrame = mul(worldToPenetratorBasisTransform, worldFrame.xyz).xyz;
    // Then we basis transform it again into catmull rom-space, with another basis transform.
    float3 worldFrameRotated = mul(penetratorToCatmullBasisTransform,localFrame).xyz;
    // Finally rotate it to face our original updir
    worldFrameRotated = RotateAroundAxisPenetration(worldFrameRotated, catForward, angle);

    // It will still be centered around 0,0,0, so we simply add the curve sample position we made earlier.
    float3 catmullSpacePosition = catPosition+worldFrameRotated;

    // Bring it back into object space, now that we're done working on it.
    worldPositionOUT = lerp(worldPosition, catmullSpacePosition, isPenetrator);
}

// Penetratable stuff down below
sampler2D _PenetratorGirthMapX;
sampler2D _PenetratorGirthMapY;
sampler2D _PenetratorGirthMapZ;
sampler2D _PenetratorGirthMapW;

struct PenetratorData {
    float blend;
    float worldPenetratorLength;
    float worldDistance;
    float girthScaleFactor;
    float angle;
    float3 initialRight;
    float3 initialUp;
};

#if (defined(UNITY_COMPILER_CG) && (defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)) || !defined(UNITY_COMPILER_CG) && !defined(SHADER_API_D3D11_9X))
StructuredBuffer<PenetratorData> _PenetratorData;
#else
PenetratorData _PenetratorData[4];
#endif

void GetDeformationFromPenetrator(inout float3 worldPosition, float holeT, float compressibleDistance, sampler2D girthMap, PenetratorData data, int curveIndex, float smoothness) {
    // Just skip everything if blend is 0, we might not even have curves to sample.
    if (data.blend == 0) {
        return;
    }
    // Since our t sample value is based on a piece-wise curve, we need to figure out which curve weights we're meant to sample.
    int curveSegmentIndex = 0;
    // TODO: This could possibly be a bug! though it seems to work fine at all scales
    // Trigger earlier than baked, only slightly. Otherwise things trigger "perceptively" too late. (in reality they're perfect, but it just doesn't look right).
    float anticipation = 0.012;
    float subT = GetCurveSegment(curveIndex, holeT-anticipation, curveSegmentIndex);

    float3 catPosition = SampleCurveSegmentPosition(curveIndex,curveSegmentIndex, subT);

    float3 diff = worldPosition-catPosition;
    float3 diffNorm = normalize(diff);
    // Get the rotation around the curve that we need to sample.
    float2 holeFlat = float2(dot(diffNorm,data.initialRight), dot(diffNorm,data.initialUp));
    float holeAngle = atan2(holeFlat.y, holeFlat.x)+3.14159265359;

    float diffDistance = length(diff);

    float dist = TimeToDistance(curveIndex, holeT)+data.worldDistance;
    float2 girthSampleUV = float2(dist/data.worldPenetratorLength, (-holeAngle+data.angle)/6.28318530718);

    float texSample = tex2Dlod(girthMap,float4(frac(girthSampleUV.xy),0,diffDistance*smoothness*smoothness)).r;
    texSample *= 1-pow(2,-10*saturate(1-girthSampleUV.x));
    float girthSample = texSample*data.girthScaleFactor;

    float compressionFactor = saturate((diffDistance-girthSample)/compressibleDistance);
    
    worldPosition += diffNorm*(girthSample)*(1-compressionFactor);
}

void GetDetailFromPenetrator(inout float3 worldPosition, float holeT, float compressibleDistance, sampler2D girthMap, PenetratorData data, int curveIndex, float smoothness) {
    // Just skip everything if blend is 0, we might not even have curves to sample.
    if (data.blend == 0) {
        return;
    }
    // Since our t sample value is based on a piece-wise curve, we need to figure out which curve weights we're meant to sample.
    int curveSegmentIndex = 0;
    // TODO: This could possibly be a bug! though it seems to work fine at all scales
    // Trigger earlier than baked, only slightly. Otherwise things trigger "perceptively" too late. (in reality they're perfect, but it just doesn't look right).
    float anticipation = 0.012;
    float subT = GetCurveSegment(curveIndex, holeT-anticipation, curveSegmentIndex);

    float3 catPosition = SampleCurveSegmentPosition(curveIndex,curveSegmentIndex, subT);

    float3 diff = worldPosition-catPosition;
    float3 diffNorm = normalize(diff);
    // Get the rotation around the curve that we need to sample.
    float2 holeFlat = float2(dot(diffNorm,data.initialRight), dot(diffNorm,data.initialUp));
    float holeAngle = atan2(holeFlat.y, holeFlat.x)+3.14159265359;

    float diffDistance = length(diff);
    float dist = TimeToDistance(curveIndex, holeT)+data.worldDistance;
    float2 girthSampleUV = float2(dist/data.worldPenetratorLength, (-holeAngle+data.angle)/6.28318530718);

    float girthSample = (tex2Dlod(girthMap,float4(frac(girthSampleUV.xy),0,diffDistance*smoothness*smoothness)).r-0.5)*data.girthScaleFactor;

    if (girthSampleUV.x >= 1 || girthSampleUV.x <= 0) {
        girthSample = 0;
    }
    worldPosition += diffNorm*(girthSample);
}

void GetDeformationFromPenetrators_float(float3 worldPosition, float4 uv2, float compressibleDistance, float smoothness, out float3 deformedPosition) {
    #if !defined(_PENETRATION_DEFORMATION_DETAIL_ON)
    GetDeformationFromPenetrator(worldPosition, uv2.x, compressibleDistance, _PenetratorGirthMapX, _PenetratorData[0], 0, smoothness);
    GetDeformationFromPenetrator(worldPosition, uv2.y, compressibleDistance, _PenetratorGirthMapY, _PenetratorData[1], 1, smoothness);
    GetDeformationFromPenetrator(worldPosition, uv2.z, compressibleDistance, _PenetratorGirthMapZ, _PenetratorData[2], 2, smoothness);
    GetDeformationFromPenetrator(worldPosition, uv2.w, compressibleDistance, _PenetratorGirthMapW, _PenetratorData[3], 3, smoothness);
    #else
    GetDetailFromPenetrator(worldPosition, uv2.x, compressibleDistance, _PenetratorGirthMapX, _PenetratorData[0], 0, smoothness);
    GetDetailFromPenetrator(worldPosition, uv2.y, compressibleDistance, _PenetratorGirthMapY, _PenetratorData[1], 1, smoothness);
    GetDetailFromPenetrator(worldPosition, uv2.z, compressibleDistance, _PenetratorGirthMapZ, _PenetratorData[2], 2, smoothness);
    GetDetailFromPenetrator(worldPosition, uv2.w, compressibleDistance, _PenetratorGirthMapW, _PenetratorData[3], 3, smoothness);
    #endif
    deformedPosition = worldPosition;
}
