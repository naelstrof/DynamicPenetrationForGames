using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

public class SphereCaster : MonoBehaviour {
    [Serializable]
    public class SphereCast {
        public Transform transform;
        [Range(0f,1f)]
        public float factor;
        [Range(0f,1f)]
        public float backfaceCull;
        public bool X;
        public bool Y;
        public bool Z;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct SphereCastData {
        public Matrix4x4 mat;
        public Matrix4x4 matInv;
        public float factor;
        public float backfaceCull;
        public Vector3 axis;

        public SphereCastData(SphereCast sphereCast) {
            mat = sphereCast.transform.localToWorldMatrix;
            matInv = sphereCast.transform.worldToLocalMatrix;
            factor = Mathf.Clamp01(sphereCast.factor);
            axis = new Vector3(sphereCast.X ? 1f : 0f, sphereCast.Y ? 1f : 0f, sphereCast.Z ? 1f : 0f);
            backfaceCull = sphereCast.backfaceCull;
        }
    }

    [SerializeField] private List<SphereCast> sphereCasts;
    [SerializeField] private Renderer[] targetRenderers;

    private SphereCastData[] sphereCastArray;
    
    private NativeArray<SphereCastData> sphereCastData;
    private ComputeBuffer sphereCastBuffer;
    private MaterialPropertyBlock propertyBlock;

    private void OnEnable() {
        if (sphereCasts is { Count: > 0 }) {
            foreach (var r in targetRenderers) {
                foreach (var material in r.materials) {
                    material.EnableKeyword("_SPHERE_CAST_ON");
                }
            }

            if (!sphereCastData.IsCreated) {
                sphereCastBuffer = new ComputeBuffer(sphereCasts.Count, Marshal.SizeOf(typeof(SphereCastData)));
                sphereCastData = new NativeArray<SphereCastData>(sphereCasts.Count, Allocator.Persistent);
                propertyBlock = new MaterialPropertyBlock();
            }

            var count = sphereCasts.Count;
            sphereCastArray = new SphereCastData[count];
            for (int i = 0; i < count; i++) {
                sphereCastArray[i] = new SphereCastData(sphereCasts[i]);
            }
        }
    }
    
    private void OnDisable() {
        foreach (var r in targetRenderers) {
            if (!r) {
                continue;
            }
            foreach (var material in r.materials) {
                material.DisableKeyword("_SPHERE_CAST_ON");
            }
        }
        if (sphereCastData.IsCreated) {
            sphereCastBuffer.Release();
            sphereCastData.Dispose();
        }
    }

    public void TransformPoint(ref Vector3 pos, ref Vector3 normal, float blend) {
        var originalpos = pos;
        var originalnormal = normal;
        
        int sphereCount = sphereCastData.Length;
        for (int i=0;i<sphereCount;i++) {
            SphereCastData cast = sphereCastData[i];
            var sphereMat = cast.mat;
            var sphereMatInv = cast.matInv;
            var spherePosition = sphereMat.MultiplyPoint(new Vector3(0,0,0));

            var worldPosition = pos;
            var worldNormal = normal;
        
            var deformedPosition = sphereMatInv.MultiplyVector(worldPosition-spherePosition);
            var deformedNormal = sphereMatInv.MultiplyVector(worldNormal);
        
            float mag = Mathf.Clamp01(deformedPosition.magnitude);
            var dir = deformedPosition.normalized;
        
            float oneMinusMag = 1f - mag;
        
            var correctionVector = Vector3.Scale(dir*oneMinusMag,cast.axis);
        
            float backfaceCullFactor = (Mathf.Clamp01(Vector3.Dot(dir,deformedNormal))+1f)*0.5f;
            float d = Mathf.Lerp(1f, backfaceCullFactor,cast.backfaceCull);
        
            float factor = Mathf.Approximately(cast.factor, 1f) ? 1f : Mathf.Pow(oneMinusMag,1-cast.factor)*cast.factor;
            
            var projectedPosition = deformedPosition+correctionVector*factor*d;
            var projectedNormal = Vector3.Lerp(deformedNormal, dir, oneMinusMag*factor);
        
            var undeformedPosition = sphereMat.MultiplyVector(projectedPosition);
            var undeformedNormal = sphereMat.MultiplyVector(projectedNormal);
        
            pos = undeformedPosition+spherePosition;
            normal = undeformedNormal;
        }
        pos = Vector3.Lerp(originalpos, pos, blend);
        normal = Vector3.Lerp(originalnormal, normal, blend).normalized;
    }

    public void Update() {
        if (!sphereCastData.IsCreated) return;
        var sphereCount = sphereCastArray.Length;
        for (int i = 0; i < sphereCount; i++) {
            sphereCastArray[i] = new SphereCastData(sphereCasts[i]);
        }
        sphereCastData.CopyFrom(sphereCastArray);
        sphereCastBuffer.SetData(sphereCastData, 0, 0, sphereCount);
        var rendererCount = targetRenderers.Length;
        for (int i = 0; i < rendererCount; i++) {
            var renderer = targetRenderers[i];
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetInt("_SphereCastCount", sphereCount);
            propertyBlock.SetBuffer("_SphereCasts", sphereCastBuffer);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
