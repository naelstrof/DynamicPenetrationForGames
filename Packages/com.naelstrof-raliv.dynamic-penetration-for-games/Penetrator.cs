using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Penetrator))]
public class PenetratorInspector : Editor {
    protected void OnSceneGUI() {
        var script = (Penetrator)target;
        Undo.RecordObject(this, "Transforms Updated");
        script.DrawSceneGUI();
    }
}

#endif

[ExecuteAlways]
public abstract class Penetrator : MonoBehaviour {
    [FormerlySerializedAs("penetrator")]
    [SerializeField] private PenetratorData penetratorData;
    
    [SerializeField] private PenetratorRenderers penetratorRenderers;
    
    protected abstract IList<Vector3> GetPoints();

    private bool isEditingRoot;

    protected virtual void OnEnable() {
        penetratorData.Initialize();
        penetratorRenderers.Initialize();
    }
    protected virtual void OnDisable() {
        penetratorData.Release();
        penetratorRenderers.OnDestroy();
    }
    
    protected virtual void Update() {
        penetratorData.GetSpline(GetPoints(), out var path, out float distanceAlongSpline);
        penetratorRenderers.Update(path, distanceAlongSpline, penetratorData.GetRootTransform(), penetratorData.GetRootForward(), penetratorData.GetRootRight(), penetratorData.GetRootUp());
    }

    protected virtual void OnDrawGizmos() {
        if (GetPoints().Count == 0) {
            return;
        }
        penetratorData.GetSpline(GetPoints(), out var path, out var distanceAlongSpline);
        Gizmos.color = Color.red;
        Vector3 lastPoint = path.GetPositionFromT(0f);
        for (int i = 0; i < 64; i++) {
            Vector3 newPoint = path.GetPositionFromT((float)i / 64f);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }

        var save = Gizmos.matrix;
        foreach(var weight in path.GetWeights()) {
            Vector3 pointA = CatmullSpline.GetPosition(weight, 0f);
            Vector3 normalA = CatmullSpline.GetVelocity(weight, 0f);
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(pointA, Quaternion.FromToRotation(Vector3.forward, normalA.normalized), Vector3.one - Vector3.forward * 0.8f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one*0.025f);
        }
        Gizmos.matrix = save;
    }
    
#if UNITY_EDITOR
    public void DrawSceneGUI() {
        Undo.RecordObject(this, "Transforms Updated");
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.white;
        var globalDickRootPositionRotation = Quaternion.LookRotation(penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootForward()), penetratorData.GetRootTransform().TransformDirection(penetratorData.GetRootUp()));
        var globalDickRootPositionOffset = penetratorData.GetRootTransform().TransformPoint(penetratorData.GetRootPositionOffset());
        globalDickRootPositionOffset = Handles.PositionHandle(globalDickRootPositionOffset, globalDickRootPositionRotation);
        globalDickRootPositionRotation = Handles.RotationHandle(globalDickRootPositionRotation, globalDickRootPositionOffset);
        if (EditorGUI.EndChangeCheck()) {
            penetratorData.SetDickPositionInfo(
                penetratorData.GetRootTransform().InverseTransformPoint(globalDickRootPositionOffset),
                Quaternion.Inverse(penetratorData.GetRootTransform().rotation) * globalDickRootPositionRotation
            );
        }
        Handles.DrawWireDisc(
            globalDickRootPositionOffset+globalDickRootPositionRotation*Vector3.forward * penetratorData.GetPenetratorWorldLength(),
            globalDickRootPositionRotation*Vector3.forward,
            0.1f
            );
    }
#endif
    
}
