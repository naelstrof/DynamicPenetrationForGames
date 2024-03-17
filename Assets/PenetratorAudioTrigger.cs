using System;
using System.Collections;
using System.Collections.Generic;
using DPG;
using UnityEngine;

public class PenetratorAudioTrigger : MonoBehaviour {
    [SerializeField] private Penetrator penetrator;
    [SerializeField] private AudioClip clip;
    [SerializeField,Range(0f,1f)] private float normalizedDistance;

    private float? lastPenetrationDepth;

    private CatmullSpline cachedCatmullSpline;

    private void OnEnable() {
        penetrator.penetrated += OnPenetrated;
    }
    
    private void OnDisable() {
        penetrator.penetrated -= OnPenetrated;
    }

    private void OnPenetrated(Penetrator penetrator1, Penetrable penetrable, Penetrator.PenetrationArgs penetrationArgs, Penetrable.PenetrationResult result) {
        float triggerDepth = (1f-normalizedDistance) * penetrator1.GetSquashStretchedWorldLength();
        // TODO: Make it louder if the depentration is slow and deliberate, and make it quieter for fast thrusts.
        if ((lastPenetrationDepth ?? penetrationArgs.penetrationDepth) > triggerDepth && penetrationArgs.penetrationDepth < triggerDepth) {
            AudioSource.PlayClipAtPoint(clip, penetrator1.GetRootTransform().position);
        }
        lastPenetrationDepth = penetrationArgs.penetrationDepth;
    }

    private void OnDrawGizmosSelected() {
        if (penetrator == null) {
            return;
        }
        cachedCatmullSpline ??= new CatmullSpline(new List<Vector3>() { Vector3.zero, Vector3.one });
        penetrator.GetFinalizedSpline(ref cachedCatmullSpline, out var distanceAlongSpline, out var insertionLerp, out var penetrationArgs);
        Vector3 pos = cachedCatmullSpline.GetPositionFromDistance(distanceAlongSpline + penetrator.GetSquashStretchedWorldLength() * normalizedDistance);
        var oldColor = Gizmos.color;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pos, 0.025f);
        Gizmos.color = oldColor;
    }
}
