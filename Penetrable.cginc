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

// FIXME: I'm not actually sure this can even compile on mobile platforms. We need to double check.
// Thoeretically there's no reason to use dynamic buffers like this (we should have static spline counts anyway).
// But this was the most convenient way I could think of for the programming side of things.

#include "Splines.cginc"

#pragma target 5.0

#if (defined(UNITY_COMPILER_CG) && (defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)) || !defined(UNITY_COMPILER_CG) && !defined(SHADER_API_D3D11_9X))
StructuredBuffer<CatmullSplineData> _PenetrableSplines;
#else
CatmullSplineData _PenetrableSplines[4];
#endif

sampler2D _PenetratorGirthMapX;
sampler2D _PenetratorGirthMapY;
sampler2D _PenetratorGirthMapZ;
sampler2D _PenetratorGirthMapW;

struct PenetratorData {
    float squashStretch;
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

void GetDeformationFromPenetrator(inout float3 worldPosition, float holeNormalizedDistance, float compressibleDistance, sampler2D girthMap, PenetratorData data, int curveIndex, float smoothness) {
    // Just skip everything if blend is 0, we might not even have curves to sample.
    if (data.blend == 0) {
        return;
    }
    // Since our t sample value is based on a piece-wise curve, we need to figure out which curve weights we're meant to sample.
    int curveSegmentIndex = 0;
    
    CatmullSplineData spline = _PenetrableSplines[curveIndex];
    float holeT = DistanceToTime(spline, holeNormalizedDistance*TimeToDistance(spline, 1));
    float subT = GetCurveSegment(spline, holeT, curveSegmentIndex);

    float3 catPosition = SampleCurveSegmentPosition(spline,curveSegmentIndex, subT);

    float3 diff = worldPosition-catPosition;
    float3 diffNorm = normalize(diff);
    // Get the rotation around the curve that we need to sample.
    float2 holeFlat = float2(dot(diffNorm,data.initialRight), dot(diffNorm,data.initialUp));
    float holeAngle = atan2(holeFlat.y, holeFlat.x)+3.14159265359;

    float diffDistance = length(diff);

    float dist = TimeToDistance(spline, holeT)+data.worldDistance;
    float2 girthSampleUV = float2(dist/(data.worldPenetratorLength*data.squashStretch), (-holeAngle+data.angle)/6.28318530718);
    
    float texSample = tex2Dlod(girthMap,float4(girthSampleUV.xy,0,diffDistance*smoothness*smoothness)).r;
    texSample *= 1-pow(2,-20*saturate(1-girthSampleUV.x));
    float girthSample = texSample*(data.girthScaleFactor/data.squashStretch);

    float compressionFactor = saturate((diffDistance-girthSample)/compressibleDistance);
    
    worldPosition += diffNorm*(girthSample)*(1-compressionFactor);
}

void GetDetailFromPenetrator(inout float3 worldPosition, float holeNormalizedDistance, float compressibleDistance, sampler2D girthMap, PenetratorData data, int curveIndex, float smoothness) {
    // Just skip everything if blend is 0, we might not even have curves to sample.
    if (data.blend == 0) {
        return;
    }
    // Since our t sample value is based on a piece-wise curve, we need to figure out which curve weights we're meant to sample.
    int curveSegmentIndex = 0;
    
    CatmullSplineData spline = _PenetrableSplines[curveIndex];
    float holeT = DistanceToTime(spline, holeNormalizedDistance*TimeToDistance(spline, 1));
    float subT = GetCurveSegment(spline, holeT, curveSegmentIndex);

    float3 catPosition = SampleCurveSegmentPosition(spline,curveSegmentIndex, subT);

    float3 diff = worldPosition-catPosition;
    float3 diffNorm = normalize(diff);
    // Get the rotation around the curve that we need to sample.
    float2 holeFlat = float2(dot(diffNorm,data.initialRight), dot(diffNorm,data.initialUp));
    float holeAngle = atan2(holeFlat.y, holeFlat.x)+3.14159265359;

    float diffDistance = length(diff);
    float dist = TimeToDistance(spline, holeT)+data.worldDistance;
    float2 girthSampleUV = float2(dist/data.worldPenetratorLength, (-holeAngle+data.angle)/6.28318530718);

    
    float texSample = tex2Dlod(girthMap,float4(girthSampleUV.xy,0,diffDistance*smoothness*smoothness)).r;
    texSample *= 1-pow(2,-20*saturate(1-girthSampleUV.x));
    float girthSample = (texSample-0.5)*data.girthScaleFactor;
    
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
