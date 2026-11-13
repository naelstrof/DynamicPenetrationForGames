using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace GatorDragonGames {

[RequireComponent(typeof(Renderer)), DisallowMultipleComponent]
public class SphereCasterRenderer : MonoBehaviour {
    [StructLayout(LayoutKind.Sequential)]
    public struct SphereCast {
        public Matrix4x4 mat;
        public Matrix4x4 matInv;
        public float factor;
        public float backfaceCull;
        public Vector3 axis;
        
        public SphereCast(Matrix4x4 mat, float factor, float backfaceCull, bool X, bool Y, bool Z) {
            this.mat = mat;
            matInv = mat.inverse;
            this.factor = Mathf.Clamp01(factor);
            axis = new Vector3(X ? 1f : 0f, Y ? 1f : 0f, Z ? 1f : 0f);
            this.backfaceCull = backfaceCull;
        }

        public SphereCast(Vector3 point, Vector3 scale, Quaternion rotation, float factor, float backfaceCull, bool X, bool Y, bool Z) {
            mat = Matrix4x4.TRS(point, rotation, scale);
            matInv = mat.inverse;
            this.factor = Mathf.Clamp01(factor);
            axis = new Vector3(X ? 1f : 0f, Y ? 1f : 0f, Z ? 1f : 0f);
            this.backfaceCull = backfaceCull;
        }

        public SphereCast(Transform point, float factor, float backfaceCull, bool X, bool Y, bool Z) {
            mat = point.localToWorldMatrix;
            matInv = point.worldToLocalMatrix;
            this.factor = Mathf.Clamp01(factor);
            axis = new Vector3(X ? 1f : 0f, Y ? 1f : 0f, Z ? 1f : 0f);
            this.backfaceCull = backfaceCull;
        }
    }

    private Renderer targetRenderer;

    private SphereCast[] sphereCasts;
    private NativeArray<SphereCast> sphereCastData;

    private ComputeBuffer sphereCastBuffer;
    private MaterialPropertyBlock propertyBlock;
    private bool isInitialized = false;
    private const int maxSphereCasts = 32;
    private int sphereCastCount = 0;

    private void OnEnable() {
        targetRenderer = GetComponent<Renderer>();
        sphereCasts = new SphereCast[maxSphereCasts];
    }

    private void OnDisable() {
        CleanUp();
        if (sphereCastData.IsCreated) {
            sphereCastBuffer.Release();
            sphereCastData.Dispose();
        }
    }

    public void TransformPoint(ref Vector3 pos, ref Vector3 normal, float blend) {
        var originalpos = pos;
        var originalnormal = normal;

        int sphereCount = sphereCastData.Length;
        for (int i = 0; i < sphereCount; i++) {
            SphereCast cast = sphereCastData[i];
            var sphereMat = cast.mat;
            var sphereMatInv = cast.matInv;
            var spherePosition = sphereMat.MultiplyPoint(new Vector3(0, 0, 0));

            var worldPosition = pos;
            var worldNormal = normal;

            var deformedPosition = sphereMatInv.MultiplyVector(worldPosition - spherePosition);
            var deformedNormal = sphereMatInv.MultiplyVector(worldNormal);

            float mag = Mathf.Clamp01(deformedPosition.magnitude);
            var dir = deformedPosition.normalized;

            float oneMinusMag = 1f - mag;

            var correctionVector = Vector3.Scale(dir * oneMinusMag, cast.axis);

            float backfaceCullFactor = (Mathf.Clamp01(Vector3.Dot(dir, deformedNormal)) + 1f) * 0.5f;
            float d = Mathf.Lerp(1f, backfaceCullFactor, cast.backfaceCull);

            float factor = Mathf.Approximately(cast.factor, 1f)
                ? 1f
                : Mathf.Pow(oneMinusMag, 1 - cast.factor) * cast.factor;

            var projectedPosition = deformedPosition + correctionVector * factor * d;
            var projectedNormal = Vector3.Lerp(deformedNormal, dir, oneMinusMag * factor);

            var undeformedPosition = sphereMat.MultiplyVector(projectedPosition);
            var undeformedNormal = sphereMat.MultiplyVector(projectedNormal);

            pos = undeformedPosition + spherePosition;
            normal = undeformedNormal;
        }

        pos = Vector3.Lerp(originalpos, pos, blend);
        normal = Vector3.Lerp(originalnormal, normal, blend).normalized;
    }

    void Initialize() {
        if (isInitialized) {
            return;
        }

        isInitialized = true;
        foreach (var material in targetRenderer.materials) {
            material.EnableKeyword("_SPHERE_CAST_ON");
        }

        if (!sphereCastData.IsCreated) {
            sphereCastBuffer = new ComputeBuffer(maxSphereCasts, Marshal.SizeOf(typeof(SphereCast)));
            sphereCastData = new NativeArray<SphereCast>(maxSphereCasts, Allocator.Persistent);
            sphereCastData.CopyFrom(sphereCasts);
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void CleanUp() {
        if (!isInitialized) {
            return;
        }

        isInitialized = false;
        foreach (var material in targetRenderer.materials) {
            material.DisableKeyword("_SPHERE_CAST_ON");
        }
    }

    public void AddSphereCast(SphereCast sphereCast) {
        if (sphereCastCount >= maxSphereCasts) {
            return;
        }

        sphereCasts[sphereCastCount] = sphereCast;
        sphereCastCount++;
    }
    
    public void AddSphereCasts(SphereCast[] newSphereCasts) {
        var addCount = Mathf.Min(maxSphereCasts - sphereCastCount, newSphereCasts.Length);
        Array.Copy(newSphereCasts, 0, sphereCasts, sphereCastCount, addCount);
        sphereCastCount += addCount;
    }

    public void LateUpdate() {
        if (sphereCastCount <= 0) {
            CleanUp();
            return;
        }

        Initialize();
        sphereCastData.CopyFrom(sphereCasts);
        sphereCastBuffer.SetData(sphereCastData, 0, 0, sphereCastCount);
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetInt("_SphereCastCount", sphereCastCount);
        propertyBlock.SetBuffer("_SphereCasts", sphereCastBuffer);
        targetRenderer.SetPropertyBlock(propertyBlock);
        sphereCastCount = 0;
    }
}

}