using System;
using UnityEditor;
using UnityEngine;

public static class CurveUtility
{
    public static AnimationCurve Sample(Func<float, float> f, int samples = 48, bool clamp01 = false)
    {
        if (samples < 2) samples = 2;
        var keys = new Keyframe[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (samples - 1f);
            float v = f(t);
            if (clamp01) v = Mathf.Clamp01(v);
            keys[i] = new Keyframe(t, v);
        }
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        return curve;
    }

    public static float EaseOutElastic(float x)
    {
        const float c4 = (2f * Mathf.PI) / 3f;
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f; // overshoots intentionally
    }

    public static float ExpEaseOut(float x) // good for alpha
    {
        // 1 - exp(-k x), normalised to hit 1 at x=1
        const float k = 4f;
        return (1f - Mathf.Exp(-k * x)) / (1f - Mathf.Exp(-k));
    }

    public static float EaseOutCubic(float x) // backup monotone
    {
        float t = 1f - x;
        return 1f - t * t * t;
    }
}
