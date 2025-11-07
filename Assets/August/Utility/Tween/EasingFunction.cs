using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EasingFunctions
{
    // Samples a function to create an AnimationCurve. Useful for visual inspection in the editor.
#if UNITY_EDITOR
    public static AnimationCurve Sample(Func<float, float> f, int samples = 48)
    {
        if (samples < 2) samples = 2;
        var keys = new Keyframe[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (samples - 1f);
            float v = f(t);
            keys[i] = new Keyframe(t, v);
        }
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }
        return curve;
    }
#endif

    public static float EaseOutElastic(float x)
    {
        const float c4 = (2f * Mathf.PI) / 3f;
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1f;
    }

    public static float ExpEaseOut(float x)
    {
        const float k = 4f;
        return (1f - Mathf.Exp(-k * x)) / (1f - Mathf.Exp(-k));
    }

    public static float EaseOutCubic(float x)
    {
        float t = 1f - x;
        return 1f - t * t * t;
    }

    public static float EaseInOutCirc(float x)
    {
        return x < 0.5f
          ? (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * x, 2))) / 2
          : (Mathf.Sqrt(1 - Mathf.Pow(-2 * x + 2, 2)) + 1) / 2;
    }

    public static float EaseInOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c2 = c1 * 1.525f;
        return x < 0.5
          ? (Mathf.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2
          : (Mathf.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2;
    }

    public static float EaseInOutQuint(float x)
    {
        return x < 0.5f ? 16 * x * x * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 5) / 2;
    }
    public static float EaseInOutExpo(float x)
    {
        return x == 0 ? 0 : x == 1 ? 1 : 
            x < 0.5 ? Mathf.Pow(2, 20 * x - 10) / 2f : (2 - Mathf.Pow(2, -20 * x + 10)) / 2;
    }

    public static float EaseInOutElastic(float x)
        {
            const float c5 = (2f * MathF.PI) / 4.5f;

            if (x == 0f)
                return 0f;
            if (x == 1f)
                return 1f;

            if (x < 0.5f)
            {
                return -(MathF.Pow(2f, 20f * x - 10f) *
                         MathF.Sin((20f * x - 11.125f) * c5)) / 2f;
            }
            else
            {
                return (MathF.Pow(2f, -20f * x + 10f) *
                        MathF.Sin((20f * x - 11.125f) * c5)) / 2f + 1f;
            }

        }



    public static float Linear(float x) => x; // baseline
    }
