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

    public Transform GetRootTransform() => dickRootTransform;
    public Vector3 GetRootPositionOffset() => dickRootPositionOffset;
    public Vector3 GetRootForward() => dickRootForward;
    public Vector3 GetRootUp() => dickRootUp;
    public Vector3 GetRootRight() => Vector3.Cross(dickRootUp, dickRootForward);

    public void SetDickPositionInfo(Vector3 position, Quaternion rotation) {
        dickRootPositionOffset = position;
    }
    
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
    
    public void GetSpline(Quaternion rootRotation, IList<Vector3> inputPoints, out CatmullSpline spline, out float baseDistanceAlongSpline) {
        if (!GetInitialized()) {
            Initialize();
        }
        points.Clear();

        Vector3 startPoint = inputPoints[0];
        points.Add(startPoint + rootRotation * dickRootForward * girthData.GetWorldLength() * -0.1f);
        points.AddRange(inputPoints);

        spline = new CatmullSpline();
        spline.SetWeightsFromPoints(points);
        baseDistanceAlongSpline = spline.GetDistanceFromSubT(0, 1, 1f);
    }

}
