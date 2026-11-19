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

using System.Threading.Tasks;

namespace DPG {

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public struct GirthLookup {
    public float maxGirth;
    [SerializeField] public GirthFrame baseGirth;
    [SerializeField] public GirthFrame[] girthDeltas;
    [SerializeField] public float[] boneTimes;

    public void DrawGizmos(Transform transform, SkinnedMeshRenderer renderer, Matrix4x4 dickBasis, int sampleCount = 32) {
        var length = GetLength(renderer);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.green;
        for (int i = 0; i < sampleCount; i++) {
            float sampleAlpha = (float)i / (float)sampleCount;
            float girthRadius = SampleGirth(renderer, sampleAlpha);
            Vector3 offset = SampleOffset(renderer, sampleAlpha);
            var position = transform.TransformPoint(dickBasis.MultiplyVector(offset+Vector3.forward * sampleAlpha*length));
            DrawCircle(position, transform.TransformDirection(dickBasis.MultiplyVector(Vector3.forward)), girthRadius);
        }
    }

    private float GetLength(SkinnedMeshRenderer renderer) {
        var b = baseGirth.maxLocalLength;
        for(int i=0; i<girthDeltas.Length; i++) {
            b += girthDeltas[i].maxLocalLength * renderer.GetBlendShapeWeight(i)*0.01f;
        }
        return b;
    }
    
    private float SampleGirth(SkinnedMeshRenderer renderer, float alpha) {
        var b = baseGirth.SampleGirth(alpha);
        for(int i=0; i<girthDeltas.Length; i++) {
            float factor = baseGirth.maxLocalLength / girthDeltas[i].maxLocalLength;
            b += girthDeltas[i].SampleGirth(alpha*factor) * renderer.GetBlendShapeWeight(i)*0.01f;
        }
        return b;
    }
    
    private Vector2 SampleOffset(SkinnedMeshRenderer renderer, float alpha) {
        var b = baseGirth.SampleOffset(alpha);
        for(int i=0; i<girthDeltas.Length; i++) {
            float factor = baseGirth.maxLocalLength / girthDeltas[i].maxLocalLength;
            b += girthDeltas[i].SampleOffset(alpha*factor) * renderer.GetBlendShapeWeight(i)*0.01f;
        }
        return b;
    }
    
    private void DrawCircle(Vector3 position, Vector3 normal, float radius, int segments = 16) {
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 0.001f) {
            tangent = Vector3.Cross(normal, Vector3.right);
        }
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent);
        bitangent.Normalize();

        Vector3 prevPoint = position + tangent * radius;
        for (int i = 1; i <= segments; i++) {
            float angle = (float)i / (float)segments * Mathf.PI * 2f;
            Vector3 nextPoint = position + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    private static void GetBindPoseBoneLocalPositionRotation(Matrix4x4 boneMatrix, out Vector3 position, out Quaternion rotation, out float scale) {
        // Get global matrix for bone
        Matrix4x4 bindMatrixGlobal = boneMatrix.inverse;

        // Get local X, Y, Z, and position of matrix
        Vector3 mX = new Vector3(bindMatrixGlobal.m00, bindMatrixGlobal.m10, bindMatrixGlobal.m20);
        Vector3 mY = new Vector3(bindMatrixGlobal.m01, bindMatrixGlobal.m11, bindMatrixGlobal.m21);
        Vector3 mZ = new Vector3(bindMatrixGlobal.m02, bindMatrixGlobal.m12, bindMatrixGlobal.m22);
        Vector3 mP = new Vector3(bindMatrixGlobal.m03, bindMatrixGlobal.m13, bindMatrixGlobal.m23);
        position = mP;

        scale = mX.magnitude;

        // Set rotation
        // Check if scaling is negative and handle accordingly
        if (Vector3.Dot(Vector3.Cross(mX, mY), mZ) >= 0) {
            rotation = Quaternion.LookRotation(mZ, mY);
        } else {
            rotation = Quaternion.LookRotation(-mZ, -mY);
        }
    }

    private static float GetMaxGirth(RendererSubMeshMask rendererMask, Transform penetratorRoot, Vector3 rendererLocalPenetratorForward, Vector3 rendererLocalPenetratorRoot, List<Vector3> staticVertices) {
        float maxGirth = 0f;
        staticVertices.Clear();
        var mesh = rendererMask.GetMesh();
        mesh.GetVertices(staticVertices);
        Vector3[] blendDeltaVertices = new Vector3[staticVertices.Count];
        for (int blendshapeIndex = -1; blendshapeIndex < mesh.blendShapeCount; blendshapeIndex++) {
            if (blendshapeIndex != -1) {
                mesh.GetBlendShapeFrameVertices(blendshapeIndex, 0, blendDeltaVertices, null, null);
            }
            for (int i = 0; i < staticVertices.Count; i++) {
                blendDeltaVertices[i] += staticVertices[i];
            }

            // If we're a skinned mesh renderer, we mask by bone weights.
            if (rendererMask.renderer is SkinnedMeshRenderer meshRenderer) {
                var bones = meshRenderer.bones;
                HashSet<int> validBones = new HashSet<int>();
                for (int i = 0; i < bones.Length; i++) {
                    if (bones[i].IsChildOf(penetratorRoot)) {
                        validBones.Add(i);
                    }
                }

                var weights = mesh.GetAllBoneWeights();
                var bonesPerVertex = mesh.GetBonesPerVertex();
                int vt = 0;
                int wt = 0;
                for (int o = 0; o < bonesPerVertex.Length; o++) {
                    for (int p = 0; p < bonesPerVertex[o]; p++) {
                        BoneWeight1 weight = weights[wt];
                        if (validBones.Contains(weight.boneIndex) && weight.weight > 0.1f) {
                            Vector3 pos = blendDeltaVertices[vt];
                            float length = Vector3.Dot(rendererLocalPenetratorForward, pos - rendererLocalPenetratorRoot);
                            float girth = Vector3.Distance(pos, (rendererLocalPenetratorRoot + rendererLocalPenetratorForward * length));
                            maxGirth = Mathf.Max(girth, maxGirth);
                        }
                        wt++;
                    }
                    vt++;
                }
            } else {
                // Otherwise we can just use every vert.
                foreach (Vector3 vertexPosition in blendDeltaVertices) {
                    float length = Vector3.Dot(rendererLocalPenetratorForward, vertexPosition - rendererLocalPenetratorRoot);
                    float girth = Vector3.Distance(vertexPosition, (rendererLocalPenetratorRoot + rendererLocalPenetratorForward * length));
                    maxGirth = Mathf.Max(girth, maxGirth);
                }
            }
        }
        return maxGirth;
    }
    public static async Task<GirthLookup> GenerateGirthLookup(RendererSubMeshMask rendererWithMask, Shader girthUnwrapShader, Transform penetratorRoot, Vector3 rootLocalPenetratorRoot, Vector3 rootLocalPenetratorForward, Vector3 rootLocalPenetratorUp, Vector3 rootLocalPenetratorRight) {
        Vector3 rendererLocalPenetratorForward = Vector3.zero;
        Vector3 rendererLocalPenetratorUp = Vector3.zero;
        Vector3 rendererLocalPenetratorRight = Vector3.zero;
        Vector3 rendererLocalPenetratorRoot = Vector3.zero;
        Matrix4x4 poseMatrix = Matrix4x4.identity;
        if (rendererWithMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
            int rootBoneID = -1;
            for (int i = 0; i < skinnedMeshRenderer.bones.Length; i++) {
                if (skinnedMeshRenderer.bones[i] == penetratorRoot) {
                    rootBoneID = i;
                }
            }

            if (rootBoneID == -1) {
                throw new UnityException("You must choose a bone on the armature...");
            }

            Mesh skinnedMesh = skinnedMeshRenderer.sharedMesh;
            GetBindPoseBoneLocalPositionRotation(skinnedMesh.bindposes[rootBoneID], out Vector3 posePosition, out Quaternion poseRotation, out float scale);
            rendererLocalPenetratorForward = (poseRotation * rootLocalPenetratorForward).normalized;
            rendererLocalPenetratorUp = (poseRotation * rootLocalPenetratorUp).normalized;
            rendererLocalPenetratorRight = (poseRotation * rootLocalPenetratorRight).normalized;
            rendererLocalPenetratorRoot = posePosition+(poseRotation*rootLocalPenetratorRoot*scale);
            poseMatrix = skinnedMeshRenderer.sharedMesh.bindposes[rootBoneID];
        } else {
            var worldToRenderer = rendererWithMask.renderer.worldToLocalMatrix;
            rendererLocalPenetratorForward = worldToRenderer.MultiplyVector(penetratorRoot.TransformDirection(rootLocalPenetratorForward)).normalized;
            rendererLocalPenetratorUp = worldToRenderer.MultiplyVector(penetratorRoot.TransformDirection(rootLocalPenetratorUp)).normalized;
            rendererLocalPenetratorRight = worldToRenderer.MultiplyVector(penetratorRoot.TransformDirection(rootLocalPenetratorRight)).normalized;
            rendererLocalPenetratorRoot = worldToRenderer.MultiplyPoint(penetratorRoot.TransformPoint(rootLocalPenetratorRoot));
            poseMatrix = Matrix4x4.identity;
        }

        var staticVertices = new List<Vector3>();
        var maxGirth = GetMaxGirth(rendererWithMask, penetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorRoot, staticVertices);
        
        var baseGirth = await GirthFrame.GenerateGirthFrame(rendererWithMask, penetratorRoot, rendererLocalPenetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorUp, rendererLocalPenetratorRight, -1, maxGirth, girthUnwrapShader);
        var blendshapeCount = rendererWithMask.GetMesh().blendShapeCount;
        var blendshapeGirths = new GirthFrame[blendshapeCount];
        for (int i = 0; i < blendshapeCount; i++) {
            var blendshapeGirth = await GirthFrame.GenerateGirthFrame(rendererWithMask, penetratorRoot, rendererLocalPenetratorRoot, rendererLocalPenetratorForward, rendererLocalPenetratorUp, rendererLocalPenetratorRight, i, maxGirth, girthUnwrapShader);
            blendshapeGirth.ConvertToBlendshape(baseGirth);
            blendshapeGirths[i] = blendshapeGirth;
        }

        return new GirthLookup() {
            baseGirth = baseGirth,
            girthDeltas = blendshapeGirths,
            maxGirth = maxGirth,
        };
    }
}

}