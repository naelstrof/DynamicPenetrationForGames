using System.Collections.Generic;
using PenetrationTech;
using UnityEngine;

[System.Serializable]
public class Penetrator {
    [SerializeField] private Shader girthUnwrapShader;
    [SerializeField] private RendererSubMeshMask mask;
    
    [SerializeField] private Transform dickRootTransform;
    [SerializeField] private Vector3 dickRootPositionOffset;
    [SerializeField] private Vector3 dickRootForward;
    [SerializeField] private Vector3 dickRootUp;
    
    private GirthData girthData;
    private static List<Vector3> points = new List<Vector3>();

    private bool GetInitialized() => girthData != null; 

    public void Initialize() {
        if (GetInitialized()) {
            return;
        }
        Vector3 up = dickRootUp;
        if (up == dickRootForward) {
            throw new UnityException("Non-orthogonal basis given!!!");
        }
        Vector3 right = Vector3.right;
        Vector3.OrthoNormalize(ref dickRootForward, ref up, ref right);
        girthData = new GirthData(mask, girthUnwrapShader, dickRootTransform, dickRootPositionOffset, dickRootForward, dickRootUp, right);
    }
    
    public CatmullSpline GetSpline(Quaternion rootRotation, IList<Vector3> inputPoints) {
        if (!GetInitialized()) {
            Initialize();
        }
        points.Clear();

        Vector3 startPoint = inputPoints[0];
        points.Add(startPoint + rootRotation * dickRootForward * girthData.GetWorldLength() * -0.1f);
        points.AddRange(inputPoints);

        CatmullSpline spline = new CatmullSpline();
        spline.SetWeightsFromPoints(points);
        return spline;
    }
}
