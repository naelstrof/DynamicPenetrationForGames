using UnityEngine;

[System.Serializable]
public struct AnimationCurveFast {
    [SerializeField] private float[] samples;
    public int GetSampleCount() => samples.Length;

    public AnimationCurveFast(int sampleCount) {
        samples = new float[sampleCount];
    }

    public void SetSample(int index, float value) {
        samples[index] = value;
    }

    public float Sample(float time) {
        var maxIndex = samples.Length - 1;
        var normalizedTime = time;
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

    public float GetPiecewiseDerivative(float time) {
        float epsilon = 0.001f;
        float a = Sample(time - epsilon);
        float b = Sample(time + epsilon);
        return (b - a) / (epsilon * 2f);
    }
}
