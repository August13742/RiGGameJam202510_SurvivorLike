using System;
using UnityEngine;
using System.Collections.Generic;

// Why? Why not
/// <summary>
/// Public API for creating tweens.
/// </summary>
public static class CustomTween
{
    // Interpolation function for float
    private static float InterpolateFloat(float a, float b, float t) => a + (b - a) * t;

    /// <summary>
    /// Tweens a float value.
    /// </summary>
    public static Tween<float> To(float from, float to, float duration, Func<float, float> ease, Action<float> onUpdate, Action onComplete = null)
    {
        var tween = new Tween<float>(from, to, duration, ease, onUpdate, InterpolateFloat, onComplete);
        TweenRunner.Instance.Register(tween);
        return tween;
    }

    /// <summary>
    /// Tweens a Vector2 value.
    /// </summary>
    public static Tween<Vector2> To(Vector2 from, Vector2 to, float duration, Func<float, float> ease, Action<Vector2> onUpdate, Action onComplete = null)
    {
        // Use LerpUnclamped to allow easing functions to overshoot the 0-1 range (e.g., elastic/back).
        var tween = new Tween<Vector2>(from, to, duration, ease, onUpdate, Vector2.LerpUnclamped, onComplete);
        TweenRunner.Instance.Register(tween);
        return tween;
    }

    /// <summary>
    /// Tweens a Vector3 value.
    /// </summary>
    public static Tween<Vector3> To(Vector3 from, Vector3 to, float duration, Func<float, float> ease, Action<Vector3> onUpdate, Action onComplete = null)
    {
        var tween = new Tween<Vector3>(from, to, duration, ease, onUpdate, Vector3.LerpUnclamped, onComplete);
        TweenRunner.Instance.Register(tween);
        return tween;
    }
}
/// <summary>
/// A tween that dynamically re-evaluates its end value every frame via a delegate.
/// Useful for following moving targets.
/// </summary>
public class DynamicTween<T> : ITween
{
    // Configuration
    public T StartValue { get; private set; }
    public float Duration { get; private set; }
    public Func<float, float> Ease { get; private set; }
    public Action<T> OnUpdate { get; private set; }
    public Action OnComplete { get; private set; }

    // State
    private float _elapsedTime;
    private bool _isComplete;
    private readonly Func<T> _endValueGetter; // a function to get the end value
    private readonly Func<T, T, float, T> _interpolator;

    public DynamicTween(
        T start,
        Func<T> endValueGetter, // Takes a delegate instead of a value
        float duration,
        Func<float, float> ease,
        Action<T> onUpdate,
        Func<T, T, float, T> interpolator,
        Action onComplete = null
    )
    {
        StartValue = start;
        _endValueGetter = endValueGetter;
        Duration = duration > 0 ? duration : 0.001f;
        Ease = ease ?? EasingFunctions.Linear;
        OnUpdate = onUpdate;
        OnComplete = onComplete;
        _interpolator = interpolator;
        _elapsedTime = 0f;
        _isComplete = false;
    }

    public bool Tick(float deltaTime)
    {
        if (_isComplete) return false;

        _elapsedTime += deltaTime;
        float t = Mathf.Clamp01(_elapsedTime / Duration);
        float easedT = Ease(t);

        // Get the current end value dynamically
        T currentEndValue = _endValueGetter();

        // Interpolate between the fixed start and the dynamic end
        T currentValue = _interpolator(StartValue, currentEndValue, easedT);

        OnUpdate?.Invoke(currentValue);

        if (_elapsedTime >= Duration)
        {
            _isComplete = true;
            OnComplete?.Invoke();
            return false;
        }
        return true;
    }
}

/// <summary>
/// A common interface for the TweenRunner to manage active tweens polymorphically.
/// </summary>
public interface ITween
{
    /// <summary>
    /// Advances the tween by deltaTime. Returns false when the tween is complete.
    /// </summary>
    bool Tick(float deltaTime);
}

/// <summary>
/// Manages a single generic tweening operation.
/// </summary>
public class Tween<T> : ITween
{
    // Configuration
    public T StartValue { get; private set; }
    public T EndValue { get; private set; }
    public float Duration { get; private set; }
    public Func<float, float> Ease { get; private set; }
    public Action<T> OnUpdate { get; private set; }
    public Action OnComplete { get; private set; }

    // State
    private float _elapsedTime;
    private bool _isComplete;
    private readonly Func<T, T, float, T> _interpolator;

    public Tween(
        T start,
        T end,
        float duration,
        Func<float, float> ease,
        Action<T> onUpdate,
        Func<T, T, float, T> interpolator, // Delegate for type-specific interpolation
        Action onComplete = null
    )
    {
        StartValue = start;
        EndValue = end;
        Duration = duration > 0 ? duration : 0.001f;
        Ease = ease ?? EasingFunctions.Linear;
        OnUpdate = onUpdate;
        OnComplete = onComplete;
        _interpolator = interpolator;

        _elapsedTime = 0f;
        _isComplete = false;
    }

    public bool Tick(float deltaTime)
    {
        if (_isComplete) return false;

        _elapsedTime += deltaTime;
        float t = Mathf.Clamp01(_elapsedTime / Duration);
        float easedT = Ease(t);

        // Use the provided interpolator to calculate the current value
        T currentValue = _interpolator(StartValue, EndValue, easedT);

        OnUpdate?.Invoke(currentValue);

        if (_elapsedTime >= Duration)
        {
            _isComplete = true;
            OnComplete?.Invoke();
            return false;
        }
        return true;
    }
}


/// <summary>
/// A MonoBehaviour responsible for updating all active tweens.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TweenRunner : MonoBehaviour
{
    private static TweenRunner _instance;
    private readonly List<ITween> _activeTweens = new List<ITween>();
    private readonly List<ITween> _tweensToAdd = new List<ITween>();

    public static TweenRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("TweenRunner");
                _instance = go.AddComponent<TweenRunner>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public void Register(ITween tween)
    {
        _tweensToAdd.Add(tween);
    }

    private void Update()
    {
        if (_tweensToAdd.Count > 0)
        {
            _activeTweens.AddRange(_tweensToAdd);
            _tweensToAdd.Clear();
        }

        for (int i = _activeTweens.Count - 1; i >= 0; i--)
        {
            if (!_activeTweens[i].Tick(Time.deltaTime))
            {
                _activeTweens.RemoveAt(i);
            }
        }
    }
}