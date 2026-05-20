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

#ifndef GDG_SPLINES

#define GDG_SPLINES

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

float3 GetBinormalFromT(CatmullSplineData spline, float t) {
    int count = BINORMAL_COUNT;
    int index = floor(t*(float)(count-1));
    float offseted = t-((float)index/(float)(count-1));
    float lerpT = offseted * (float)(count-1);
    float3 a = float3(spline.binormalLUT[index*3],spline.binormalLUT[index*3+1], spline.binormalLUT[index*3+2]);
    float3 b = float3(spline.binormalLUT[(index+1)*3],spline.binormalLUT[(index+1)*3+1], spline.binormalLUT[(index+1)*3+2]);
    return lerp(a, b, lerpT);
}
float GetCurveSegment(CatmullSplineData spline, float t, out int curveSegmentIndex) {
    curveSegmentIndex = clamp((int)floor(t*(spline.pointCount-1)),0,spline.pointCount-1);
    float offset = t-((float)curveSegmentIndex/(float)(spline.pointCount-1));
    return offset * (float)(spline.pointCount-1);
}

float GetTFromSubT(CatmullSplineData spline, int start, int end, float subT) {
    int subSplineCount = spline.pointCount-1;
    int subSection = end - start;
    float multi = (float)subSection / (float)subSplineCount;
    float startT = (float)start / (float)subSplineCount;
    return subT * multi + startT;
}
float TimeToDistance(CatmullSplineData spline, float t) {
    t = saturate(t);
    int index = clamp(floor(t*(DISTANCE_COUNT-1)),0,DISTANCE_COUNT-2);
    float offseted = t-((float)index/(float)(DISTANCE_COUNT-1));
    float lerpT = offseted * (float)(DISTANCE_COUNT-1);
    return lerp(spline.distanceLUT[index], spline.distanceLUT[index+1], lerpT);
}
float DistanceToTime(CatmullSplineData spline, float distance) {
    if (distance > 0 && distance < spline.arcLength) {
        for(int i=0;i<DISTANCE_COUNT-1;i++) {
            if (distance>spline.distanceLUT[i] && distance<spline.distanceLUT[i+1]) {
                // Remap
                float from1 = spline.distanceLUT[i];
                float to1 = spline.distanceLUT[i+1];
                float from2 = (float)i/(float)(DISTANCE_COUNT-1);
                float to2 = (float)(i+1)/(float)(DISTANCE_COUNT-1);
                return (distance - from1) / (to1 - from1) * (to2 - from2) + from2;
            }
        }
    }
    return distance/spline.arcLength;
}
float3 SampleCurveSegmentPosition(CatmullSplineData spline, int curveSegmentIndex, float t) {
    int index = curveSegmentIndex*4*4;
    const float4x4 mat = float4x4(
        spline.weightArray[index],spline.weightArray[index+1],spline.weightArray[index+2],spline.weightArray[index+3],
        spline.weightArray[index+4],spline.weightArray[index+5],spline.weightArray[index+6],spline.weightArray[index+7],
        spline.weightArray[index+8],spline.weightArray[index+9],spline.weightArray[index+10],spline.weightArray[index+11],
        spline.weightArray[index+12],spline.weightArray[index+13],spline.weightArray[index+14],spline.weightArray[index+15]);
    return mul(mat,float4(1,t,t*t,t*t*t)).xyz;
}
float3 SampleCurveSegmentVelocity(CatmullSplineData spline, int curveSegmentIndex, float t) {
    int index = curveSegmentIndex*4*4;
    const float4x4 mat = float4x4(
        spline.weightArray[index],spline.weightArray[index+1],spline.weightArray[index+2],spline.weightArray[index+3],
        spline.weightArray[index+4],spline.weightArray[index+5],spline.weightArray[index+6],spline.weightArray[index+7],
        spline.weightArray[index+8],spline.weightArray[index+9],spline.weightArray[index+10],spline.weightArray[index+11],
        spline.weightArray[index+12],spline.weightArray[index+13],spline.weightArray[index+14],spline.weightArray[index+15]);
    return mul(mat,float4(0,1,2*t,3*t*t)).xyz;
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

void Orthonormalize(inout float3 normal, inout float3 tangent, inout float3 binormal) {
    float3 N = normalize(normal);
    float3 T = tangent - N * dot(tangent, N);
    T = normalize(T);
    float3 B = normalize(cross(N, T));
    T = cross(B, N); 
    normal = N;
    tangent = T;
    binormal = B;
}
#endif
