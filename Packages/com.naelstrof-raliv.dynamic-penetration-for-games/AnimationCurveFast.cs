using UnityEngine;

namespace DPG {

[System.Serializable]
public struct GirthSlice {
    public float radius;
    public Vector3 position;
    public Vector4 boneWeights;
    public int boneIndex0;
    public int boneIndex1;
    public int boneIndex2;
    public int boneIndex3;
    public static GirthSlice Lerp(GirthSlice a, GirthSlice b, float t) {
        GirthSlice result = new GirthSlice {
            radius = Mathf.Lerp(a.radius, b.radius, t),
            position = Vector3.Lerp(a.position, b.position, t),
            boneWeights = Vector4.Lerp(a.boneWeights, b.boneWeights, t),
            boneIndex0 = a.boneIndex0,
            boneIndex1 = a.boneIndex1,
            boneIndex2 = a.boneIndex2,
            boneIndex3 = a.boneIndex3
        };
        return result;
    }
    public static GirthSlice operator +(GirthSlice a, GirthSlice b) {
        GirthSlice result = new GirthSlice {
            radius = a.radius + b.radius,
            position = a.position + b.position,
            boneWeights = a.boneWeights,
            boneIndex0 = a.boneIndex0,
            boneIndex1 = a.boneIndex1,
            boneIndex2 = a.boneIndex2,
            boneIndex3 = a.boneIndex3
        };
        return result;
    }
    public static GirthSlice operator -(GirthSlice a, GirthSlice b) {
        GirthSlice result = new GirthSlice {
            radius = a.radius - b.radius,
            position = a.position - b.position,
            boneWeights = a.boneWeights,
            boneIndex0 = a.boneIndex0,
            boneIndex1 = a.boneIndex1,
            boneIndex2 = a.boneIndex2,
            boneIndex3 = a.boneIndex3
        };
        return result;
    }
    public static GirthSlice operator *(GirthSlice a, float b) {
        GirthSlice result = new GirthSlice {
            radius = a.radius * b,
            position = a.position * b,
            boneWeights = a.boneWeights,
            boneIndex0 = a.boneIndex0,
            boneIndex1 = a.boneIndex1,
            boneIndex2 = a.boneIndex2,
            boneIndex3 = a.boneIndex3
        };
        return result;
    }
}


[System.Serializable]
public struct GirthSliceCurve {
    [SerializeField] private GirthSlice[] samples;

    public int GetSampleCount() => samples.Length;

    public GirthSliceCurve(int sampleCount) {
        samples = new GirthSlice[sampleCount];
    }

    public GirthSliceCurve Clone() {
        var newCurve = new GirthSliceCurve(samples.Length);
        System.Array.Copy(samples, newCurve.samples, GetSampleCount());
        return newCurve;
    }

    public void SetSample(int index, GirthSlice value) {
        samples[index] = value;
    }

    public GirthSlice GetSample(int index) {
        return samples[index];
    }

    public GirthSlice Sample(float time) {
        var maxIndex = samples.Length - 1;
        var normalizedTime = time;
        int loc = (int)(normalizedTime * (float)maxIndex);
        if (loc >= (maxIndex)) {
            return samples[maxIndex];
        }

        if (loc <= 0) {
            return samples[0];
        }

        float lerp = (normalizedTime * maxIndex) - loc;
        return GirthSlice.Lerp(samples[loc], samples[loc + 1], lerp);
    }

    public GirthSlice GetPiecewiseDerivative(float time) {
        float epsilon = 0.001f;
        GirthSlice a = Sample(time - epsilon);
        GirthSlice b = Sample(time + epsilon);
        return (b - a) * (1f/(epsilon * 2f));
    }
}

}