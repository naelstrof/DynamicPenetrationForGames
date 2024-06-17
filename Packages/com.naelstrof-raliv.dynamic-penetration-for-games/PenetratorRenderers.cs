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

using System.Linq;

namespace DPG {

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[System.Serializable]
public class PenetratorRenderers {
    [SerializeField]
    private List<Renderer> renderers;

    private List<Renderer> previousRenderers;

    private struct MaterialReference {
        public Renderer targetRenderer;
        public bool IsValid(Material targetMaterial, bool isUnityValidating) {
            if (targetRenderer == null) {
                return false;
            }
            if (!isUnityValidating && !targetRenderer.sharedMaterials.Contains(targetMaterial)) {
                return false;
            }
            return true;
        }
    }
    private static Dictionary<Material,List<MaterialReference>> sharedMaterialUses;
    
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
    private static readonly int DpgBlend = Shader.PropertyToID("_DPGBlend");

    public void GetRenderers(IList<Renderer> output) {
        output.Clear();
        foreach (var renderer in renderers) {
            output.Add(renderer);
        }
    }

    static void AddRefCount(Renderer renderer, Material material, bool isUnityValidating) {
#if UNITY_EDITOR
        if (!Application.isPlaying && renderer == null) {
            return;
        }
#endif
        sharedMaterialUses ??= new Dictionary<Material, List<MaterialReference>>();
        if (!sharedMaterialUses.ContainsKey(material)) {
            sharedMaterialUses.Add(material,new List<MaterialReference>());
        }
        foreach (var use in sharedMaterialUses[material]) {
            if (use.targetRenderer == renderer) {
                return;
            }
        }
        sharedMaterialUses[material].Add(new MaterialReference() {
            targetRenderer = renderer
        });
        UpdateReferences(isUnityValidating);
    }

    static void RemoveRefCount(Renderer renderer, Material material, bool isUnityValidating) {
#if UNITY_EDITOR
        if (!Application.isPlaying && renderer == null) {
            return;
        }
#endif
        sharedMaterialUses ??= new Dictionary<Material, List<MaterialReference>>();
        if (!sharedMaterialUses.ContainsKey(material)) {
            sharedMaterialUses.Add(material, new List<MaterialReference>());
        }
        sharedMaterialUses[material].RemoveAll((a) => a.targetRenderer == renderer);
        UpdateReferences(isUnityValidating);
    }

    static void UpdateReferences(bool isUnityValidating) {
        List<Material> removeList = new List<Material>();
        foreach (var pair in sharedMaterialUses) {
            pair.Value.RemoveAll((a) => !a.IsValid(pair.Key, isUnityValidating));
            if (pair.Value.Count == 0) {
                pair.Key.DisableKeyword("_DPG_CURVE_SKINNING");
                removeList.Add(pair.Key);
            } else {
                pair.Key.EnableKeyword("_DPG_CURVE_SKINNING");
            }
        }

        foreach (var key in removeList) {
            sharedMaterialUses.Remove(key);
        }
    }

    private void SetFlags(Renderer renderer, bool active, bool isUnityValidating) {
#if UNITY_EDITOR
        if (!Application.isPlaying && renderer == null) {
            return;
        }
#endif
        if (!Application.isPlaying || isUnityValidating) {
            foreach (var material in renderer.sharedMaterials) {
                if (hasTruncateKeyword && active) {
                    material.EnableKeyword("_DPG_TRUNCATE_SPHERIZE");
                } else {
                    material.DisableKeyword("_DPG_TRUNCATE_SPHERIZE");
                }

                if (active) {
                    AddRefCount(renderer, material, isUnityValidating);
                } else {
                    RemoveRefCount(renderer, material, isUnityValidating);
                    renderer.SetPropertyBlock(null);
                }
            }
        } else {
            foreach (var material in renderer.materials) {
                if (hasTruncateKeyword && active) {
                    material.EnableKeyword("_DPG_TRUNCATE_SPHERIZE");
                } else {
                    material.DisableKeyword("_DPG_TRUNCATE_SPHERIZE");
                }

                if (active) {
                    material.EnableKeyword("_DPG_CURVE_SKINNING");
                } else {
                    material.DisableKeyword("_DPG_CURVE_SKINNING");
                    renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat(DpgBlend, 0f);
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }
    }

    public void AddRenderer(Renderer renderer) {
        if (renderers.Contains(renderer)) {
            return;
        }
        renderers.Add(renderer);
        SetFlags(renderer, true, false);
    }
    
    public void RemoveRenderer(Renderer renderer) {
        SetFlags(renderer, false, false);
        if (renderers.Contains(renderer)) {
            renderers.Remove(renderer);
        }
    }

    private void UpdateTruncateKeyword(bool newHasTruncate, bool isUnityValidating) {
        if (hasTruncateKeyword == newHasTruncate) {
            return;
        }
        hasTruncateKeyword = newHasTruncate;
        
        foreach (var renderer in renderers) {
            SetFlags(renderer, true, isUnityValidating);
        }
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
        UpdateTruncateKeyword(truncation.HasValue, false);
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
            propertyBlock.SetFloat(DpgBlend, 1f);
            propertyBlock.SetFloat(distanceToHoleID, distanceToHole);
            // FIXME: For an unknown reason, gotta multiply in the squash and stretch here.
            propertyBlock.SetFloat(truncateLengthID, truncation?.length * squashAndStretch ?? soFarItCantBeReached);
            propertyBlock.SetFloat(girthRadiusID, truncation?.girth ?? 1f);
            propertyBlock.SetFloat(startClipID, clippingRange?.startDistance * squashAndStretch ?? soFarItCantBeReached);
            propertyBlock.SetFloat(endClipID, clippingRange.HasValue ? clippingRange.Value.endDistance * squashAndStretch ?? soFarItCantBeReached : 0f);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void OnValidate() {
        if (renderers == null) {
            return;
        }

        previousRenderers ??= new List<Renderer>();
        foreach (var renderer in renderers) {
            if (!previousRenderers.Contains(renderer)) {
                // new renderer
                SetFlags(renderer, true, true);
            }
        }
        foreach (var renderer in previousRenderers) {
            if (!renderers.Contains(renderer)) {
                // removed renderer
                SetFlags(renderer, false, true);
            }
        }
        previousRenderers = new List<Renderer>(renderers);
    }

    public void OnEnable() {
        Initialize();
        if (renderers == null) {
            return;
        }
        foreach (var renderer in renderers) {
            SetFlags(renderer, true, false);
        }
        previousRenderers = new List<Renderer>(renderers);
    }

    public void OnDisable() {
        if (previousRenderers != null) {
            foreach (var renderer in previousRenderers) {
                SetFlags(renderer, false, false);
            }

            previousRenderers = null;
        }
        catmullBuffer.Release();
        data.Dispose();
        propertyBlock = null;
    }
    
}

}