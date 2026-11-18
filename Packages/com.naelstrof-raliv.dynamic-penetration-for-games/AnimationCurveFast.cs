using UnityEngine;

public struct AnimationCurveFast {
    private int sampleCount;
    private float[] samples;
    private float fullLength;
    private int maxIndex;

    public AnimationCurveFast(int sampleCount, float fullLength) {
        this.sampleCount = sampleCount;
        this.fullLength = fullLength;
        samples = new float[sampleCount];
        maxIndex = sampleCount - 1;
    }

    public void AddSample(int index, float value) {
        samples[index] = value;
    }

    public float Sample(float time) {
        var normalizedTime = time / fullLength;
        int loc = (int)(normalizedTime * (float)maxIndex);
        if (loc >= (maxIndex)) {
            return samples[maxIndex];
        }
        if (loc <= 0) {
            return samples[0];
        }
        float lerp = (normalizedTime*maxIndex) - loc;
        return Mathf.Lerp(samples[loc], samples[loc + 1], lerp);
    }
}
