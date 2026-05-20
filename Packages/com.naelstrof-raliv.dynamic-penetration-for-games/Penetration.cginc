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

#include "Splines.cginc"

// FIXME: I'm not actually sure this can even compile on mobile platforms. We need to double check.
// Thoeretically there's no reason to use dynamic buffers like this (we should have static spline counts anyway).
// But this was the most convenient way I could think of for the programming side of things.
#pragma target 5.0

#if (defined(UNITY_COMPILER_CG) && (defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)) || !defined(UNITY_COMPILER_CG) && !defined(SHADER_API_D3D11_9X))
StructuredBuffer<CatmullSplineData> _PenetratorSpline;
#else
CatmullSplineData _PenetratorSpline[4];
#endif

void ToCatmullRomSpace_float(float3 worldPenetratorRootPos, float3 worldPosition, float3 worldPenetratorForward, float3 worldPenetratorUp, float3 worldPenetratorRight, float3 worldNormal, float4 worldTangent, out float3 worldPositionOUT, out float3 worldNormalOUT, out float4 worldTangentOUT) {
    // We want to work in world space, as everything we're working with is there.
    
    // Dot product gives us how far along an axis a position is. This is the penetrator length distance from the penetrator root to the particular position.
    float preDist = dot(worldPenetratorForward,(worldPosition - worldPenetratorRootPos));
    float dist = max(preDist,0);

    CatmullSplineData spline0 = _PenetratorSpline[0];
    // Convert the distance into an overall t sample value
    float t = DistanceToTime(spline0,dist);
    float isPenetrator = saturate(sign(preDist));
    // Since our t sample value is based on a piece-wise curve, we need to figure out which curve weights we're meant to sample.
    int curveSegmentIndex = 0;
    float subT = GetCurveSegment(spline0, t, curveSegmentIndex);

    float3 catPosition = SampleCurveSegmentPosition(spline0,curveSegmentIndex, subT);
    float3 catTangent = SampleCurveSegmentVelocity(spline0,curveSegmentIndex, subT);
    float3 catForward = normalize(catTangent);
    // We sample the Binormal from a lookup-table, to prevent flipping and twisting.
    // https://en.wikipedia.org/wiki/Frenet%E2%80%93Serret_formulas
    // https://janakiev.com/blog/framing-parametric-curves/
    float3 catRight = GetBinormalFromT(spline0,t);
    // We can just figure out our normal with a cross product.
    float3 catUp = normalize(cross(catForward,catRight));
    
    Orthonormalize(catForward, catRight, catUp);

    float3 initialRight = GetBinormalFromT(spline0,0);
    float3 initialForward = normalize(SampleCurveSegmentVelocity(spline0,0,0));
    float3 initialUp = normalize(cross(initialForward, initialRight));
    
    Orthonormalize(initialForward, initialRight, initialUp);

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
