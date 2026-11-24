using System;
using UnityEngine;

namespace GatorDragonGames.DPG {

[Serializable]
public struct BoneChainCompression {
    private float currentTip;
    private float prevTip;
    private float restTip;
    private float compressionCurrent;
    private float compressionPrev;
    private float previousFinalCompression;
    private bool inside;

    private float outsideDamping;
    private float insideDamping;
    private float damping => inside ? insideDamping : outsideDamping;

    private float timeAccumulation;

    public float GetTipPosition() => currentTip;

    public BoneChainCompression(float restTip) {
        this.restTip = restTip;
        currentTip = restTip;
        prevTip = restTip;
        compressionCurrent = 0f;
        compressionPrev = 0f;
        outsideDamping = 0.9f;
        insideDamping = 0.5f;
        timeAccumulation = 0f;
        previousFinalCompression = 0f;
        inside = false;
    }

    public void Tick(float compressionPoint) {
        timeAccumulation += Time.deltaTime;
        int failSafe = 3;
        while (timeAccumulation >= 0.02f && failSafe > 0) {
            failSafe--;
            timeAccumulation -= 0.02f;
            SolveVerlet(compressionPoint);
        }
    }

    private void SolveVerlet(float compressionPoint) {
        var dt = Time.deltaTime;
        if (dt <= 0f) return;

        var actualCompressionPoint = compressionPoint + compressionCurrent;
        var slideDelta = previousFinalCompression - compressionPoint;

        var prevTipCompressionOffset = actualCompressionPoint - currentTip;

        var predicted = currentTip + (currentTip - prevTip) + 0f * dt * dt;
        var damped = currentTip + (predicted - currentTip) * damping;
        prevTip = currentTip;
        currentTip = damped;

        var tipError = Mathf.Abs(currentTip - restTip);
        tipError = Mathf.Clamp(tipError, 0f, 0.5f);
        currentTip = Mathf.Lerp(currentTip, restTip, tipError);

        var tipCompressionOffset = actualCompressionPoint - currentTip;

        const float pushDistance = 0.1f;
        if (inside) {
            if (inside) currentTip -= slideDelta * 0.25f;
            if (tipCompressionOffset > pushDistance * 0.5f) {
                inside = false;
            }
        } else {
            if (tipCompressionOffset > 0f) {
                if (tipCompressionOffset < pushDistance) {
                    var compressionResult = Mathf.Lerp(tipCompressionOffset, pushDistance, 0.5f);
                    currentTip = actualCompressionPoint - compressionResult;
                }
            } else {
                inside = true;
            }
        }

        previousFinalCompression = actualCompressionPoint;
    }

}

}