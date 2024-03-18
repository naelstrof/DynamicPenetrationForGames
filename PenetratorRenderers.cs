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

namespace DPG {

using System.Collections.Generic;
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

    private bool hasTruncateKeyword = false;

    public void GetRenderers(IList<Renderer> output) {
        output.Clear();
        foreach (var renderer in renderers) {
            output.Add(renderer);
        }
    }

    public void AddRenderer(Renderer renderer) {
        if (renderers.Contains(renderer)) {
            return;
        }
        renderers.Add(renderer);
        foreach (var material in Application.isPlaying ? renderer.materials : renderer.sharedMaterials) {
            if (hasTruncateKeyword) {
                material.EnableKeyword("_TRUNCATESPHERIZE_ON");
            } else {
                material.DisableKeyword("_TRUNCATESPHERIZE_ON");
            }
        }
    }
    
    public void RemoveRenderer(Renderer renderer) {
        renderers.Remove(renderer);
    }

    private void UpdateTruncateKeyword(bool newHasTruncate) {
        if (hasTruncateKeyword == newHasTruncate) {
            return;
        }

        foreach (var renderer in renderers) {
            foreach (var material in Application.isPlaying ? renderer.materials : renderer.sharedMaterials) {
                if (newHasTruncate) {
                    material.EnableKeyword("_TRUNCATESPHERIZE_ON");
                } else {
                    material.DisableKeyword("_TRUNCATESPHERIZE_ON");
                }
            }
        }

        hasTruncateKeyword = newHasTruncate;
    }

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
        Transform rootBone, Vector3 localRootForward, Vector3 localRootRight, Vector3 localRootUp, Penetrable.ClippingRangeWorld? clippingRange, Penetrable.Truncation? truncation) {
        Initialize();
        data[0] = new CatmullSplineData(spline);
        catmullBuffer.SetData(data, 0, 0, 1);
        UpdateTruncateKeyword(truncation.HasValue);
        float soFarItCantBeReached = penetratorLength * 100f;
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
            propertyBlock.SetFloat(truncateLengthID, truncation?.length ?? soFarItCantBeReached);
            propertyBlock.SetFloat(girthRadiusID, truncation?.girth ?? 1f);
            propertyBlock.SetFloat(startClipID, clippingRange?.startDistance ?? soFarItCantBeReached);
            propertyBlock.SetFloat(endClipID, clippingRange.HasValue ? clippingRange.Value.endDistance ?? soFarItCantBeReached : 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void OnDestroy() {
        catmullBuffer.Release();
        data.Dispose();
        propertyBlock = null;
    }
    
}

}