using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace GatorDragonGames {

[RequireComponent(typeof(Renderer)), DisallowMultipleComponent]
public class SphereCasterRenderer : MonoBehaviour {
    public static void Render(Renderer targetRenderer, SphereCast[] sphereCasts) {
        if (!targetRenderer.TryGetComponent(out SphereCasterRenderer sphereCasterRenderer)) {
            sphereCasterRenderer = targetRenderer.gameObject.AddComponent<SphereCasterRenderer>();
            sphereCasterRenderer.AddSphereCasts(sphereCasts);
        } else {
            sphereCasterRenderer.AddSphereCasts(sphereCasts);
        }
    }
    public static void Render(Renderer targetRenderer, SphereCast sphereCast) {
        if (!targetRenderer.TryGetComponent(out SphereCasterRenderer sphereCasterRenderer)) {
            sphereCasterRenderer = targetRenderer.gameObject.AddComponent<SphereCasterRenderer>();
            sphereCasterRenderer.AddSphereCast(sphereCast);
        } else {
            sphereCasterRenderer.AddSphereCast(sphereCast);
        }
    }
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

        public void DrawDebug(Color color) {
            DrawDebugCircle(color, mat, Vector3.up, 16);
            DrawDebugCircle(color, mat, Vector3.right, 16);
        }
        
        private void DrawDebugCircle(Color color, Matrix4x4 mat, Vector3 normal, int segments = 16) {
            Vector3 tangent = Vector3.Cross(mat.MultiplyVector(normal), Vector3.up);
            if (tangent.sqrMagnitude < 0.001f) {
                tangent = Vector3.Cross(mat.MultiplyVector(normal), Vector3.right);
            }
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(mat.MultiplyVector(normal), tangent);
            bitangent.Normalize();

            Vector3 prevPoint = mat.MultiplyPoint(Vector3.zero) + tangent * mat.lossyScale.x;
            for (int i = 1; i <= segments; i++) {
                float angle = (float)i / (float)segments * Mathf.PI * 2f;
                Vector3 nextPoint = mat.MultiplyPoint(Vector3.zero) + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * mat.lossyScale.x;
                Debug.DrawLine(prevPoint, nextPoint, color);
                prevPoint = nextPoint;
            }
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