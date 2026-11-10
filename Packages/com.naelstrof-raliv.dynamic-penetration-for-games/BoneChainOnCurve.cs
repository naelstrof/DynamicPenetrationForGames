
using UnityEngine;

public class BoneChainOnCurve {
    
    private Transform[] _boneChain;
    private Vector3[] _restBonePositions;
    private Quaternion[] _restBoneRotations;
    private float _restLength;

    public BoneChainOnCurve(Transform[] bones) {
        _boneChain = bones;
        RecordRestPosition();
        _restLength = GetTotalLength();
    }

    public float GetTotalLength() {
        var totalLength = 0f;
        for (var index = 0; index < _boneChain.Length; index++) {
            var bone = _boneChain[index];
            var currentBoneLength = 0f;
            if (index < _boneChain.Length - 1) {
                var nextBone = _boneChain[index + 1];
                currentBoneLength = Vector3.Distance(bone.position, nextBone.position);
            }
            totalLength += currentBoneLength;
        }
        return totalLength;
    }
    
    public float GetRestLength() {
        return _restLength;
    }

    public void RecordRestPosition() {
        _restBonePositions = new Vector3[_boneChain.Length];
        _restBoneRotations = new Quaternion[_boneChain.Length];
        for (var index = 0; index < _boneChain.Length; index++) {
            _restBonePositions[index] = _boneChain[index].localPosition;
            _restBoneRotations[index] = _boneChain[index].localRotation;
        }
    }

    public void AlignToSpline(CatmullSpline targetSpline, float lengthMultiplier = 1f) {
        PoseBonesToRestPose();
        var totalLength = 0f;
        var previousSplinePosition = Vector3.zero;
        var currentBoneLength = 0f;
        var boneTailVector = Vector3.forward;
        for (var index = 0; index < _boneChain.Length; index++) {
            var bone = _boneChain[index];
            if (index < _boneChain.Length - 1) {
                var nextBone = _boneChain[index + 1];
                currentBoneLength = Vector3.Distance(bone.position, nextBone.position);
                currentBoneLength *= lengthMultiplier;
                boneTailVector = nextBone.position - bone.position;
            }
            totalLength += currentBoneLength;
            if (index!=0) bone.position = previousSplinePosition;
            var aimPoint = targetSpline.GetPositionFromDistance(totalLength);
            //Debug.DrawLine(aimPoint - Vector3.up * 0.1f, aimPoint + Vector3.up * 0.1f, Color.red);
            //Debug.DrawLine(aimPoint - Vector3.left * 0.1f, aimPoint + Vector3.left * 0.1f, Color.red);
            //Debug.DrawLine(aimPoint - Vector3.forward * 0.1f, aimPoint + Vector3.forward * 0.1f, Color.red);
            var aimDirection = (aimPoint - bone.position).normalized;
            var fromToRotation = Quaternion.FromToRotation(boneTailVector, aimDirection);
            bone.rotation = fromToRotation * bone.rotation;
            previousSplinePosition = aimPoint;
        }
        DebutDraw();
    }

    public void DebutDraw() {
        var firstBone = true;
        Transform previousBone = null;
        foreach (var _bone in _boneChain) {
            if (!firstBone) {
                Debug.DrawLine(previousBone.position, _bone.position, Color.white);
            }
            previousBone = _bone;
            firstBone = false;
        }
    }
    
    private void PoseBonesToRestPose() {
        for (var index = 0; index < _boneChain.Length; index++) {
            _boneChain[index].localPosition = _restBonePositions[index];
            _boneChain[index].localRotation = _restBoneRotations[index];
        }
    }
    
}
