using UnityEngine;

public class BoneChainCompression : MonoBehaviour {

    private float currentTip;
    private float prevTip;
    private float restTip;
    private float compressionCurrent;
    private float compressionPrev;
    private float previousFinalCompression;
    private bool inside;

    private float outsideDamping = 0.9f;
    private float insideDamping = 0.5f;
    private float damping => inside ? insideDamping : outsideDamping;

    private float timeAccumulation = 0f;

    public float GetTipPosition() => currentTip;

    public BoneChainCompression(float restTip) {
        this.restTip = restTip;
        this.currentTip = restTip;
        this.prevTip = restTip;
        compressionCurrent = 0f;
        compressionPrev = 0f;
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
