struct SphereCast {
    float4x4 mat;
    float4x4 matInv;
    float factor;
    float backfaceCull;
    float3 axis;
};

StructuredBuffer<SphereCast> _SphereCasts;
uniform uint _SphereCastCount;

void GetDeformationFromSphereCasts(float4x4 worldToObject, float4x4 objectToWorld, inout float3 localPosition, inout float3 localNormal) {
    for (int i=0;i<_SphereCastCount;i++) {
        SphereCast cast = _SphereCasts[i];
        float4x4 sphereMat = cast.mat;
        float4x4 sphereMatInv = cast.matInv;
        
        float3 spherePosition = mul(sphereMat, float4(0,0,0,1)).xyz;
        
        float3 worldPosition = mul(objectToWorld, float4(localPosition,1));
        float3 worldNormal = mul(objectToWorld, float4(localNormal,0));
        
        float3 deformedPosition = mul(sphereMatInv, float4(worldPosition-spherePosition,0));
        float3 deformedNormal = mul(sphereMatInv, float4(worldNormal,0));
        
        float mag = saturate(length(deformedPosition));
        float3 dir = normalize(deformedPosition);
        
        float oneMinusMag = 1 - mag;
        
        float3 correctionVector = dir*oneMinusMag*cast.axis;
        
        float backfaceCullFactor = (saturate(dot(dir,deformedNormal))+1)*0.5f;
        float d = lerp(1,backfaceCullFactor,cast.backfaceCull);
        
        float factor = cast.factor == 1 ? 1 : pow(oneMinusMag,1-cast.factor)*cast.factor;
            
        float3 projectedPosition = deformedPosition+correctionVector*factor*d;
        float3 projectedNormal = lerp(deformedNormal, dir, oneMinusMag*factor);
        
        float3 undeformedPosition = mul(sphereMat, float4(projectedPosition,0));
        float3 undeformedNormal = mul(sphereMat, float4(projectedNormal,0));
        
        localPosition = mul(worldToObject, float4(undeformedPosition+spherePosition,1));
        localNormal = normalize(mul(worldToObject, float4(undeformedNormal,0)));
    }
}


