using System;
using UnityEngine;
using System.Collections.Generic;
namespace AugustsUtility.Tween
{
    /* 
    Why? Because I want to. But you should just use DOTween instead.



    What's here: 
                    property tweening       e.g. transform.TweenLocalPosition(to,duration, easing...)

                    dynamic following: transform.TweenFollowPosition(getter, setter, target, easing ....)

                    yield return (coroutine), kill() ----> tween chaining

     */
    /// <summary>
    /// Public API for creating and managing tweens.
    /// The static methods here are the primary entry point for creating tweens.
    /// </summary>
    public static class Tween
    {
        // --- Value Tweens: Tween between a fixed start and end value ---

        /// <summary>
        /// Tweens a property from its current value to a specified end value.
        /// </summary>
        /// 
        public static ValueTween<TValue> TweenProperty<TTarget, TValue>(
            TTarget target,                                     // The target object to guard
            Func<TTarget, TValue> getter,                       // A function to get the value from the target
            Action<TTarget, TValue> setter,                     // A function to set the value on the target
            TValue to,
            float duration,
            Func<float, float> ease = null,
            Action<TValue> onUpdate = null,
            Action onComplete = null) where TTarget : UnityEngine.Object // Constraint ensures it's a Unity object
        {
            if (target == null)
            {
                Debug.LogWarning("Tween target is null. Tween will not be created.");
                return null;
            }

            ValueTween<TValue> tween = null;

            // Create a "safe" updater that wraps the user's setter and callbacks.
            void safeUpdater(TValue value)
            {
                // --- THE GUARD CLAUSE ---
                bool isTargetInvalid = target == null;

                // check if the GameObject is active for Components.
                if (!isTargetInvalid && target is Component component)
                {
                    if (!component.gameObject.activeInHierarchy)
                    {
                        isTargetInvalid = true;
                    }
                }

                if (isTargetInvalid)
                {
                    tween?.Kill();
                    return;
                }
                // --- END GUARD CLAUSE ---

                setter(target, value);
                onUpdate?.Invoke(value);
            }

            TValue from = getter(target);
            tween = new ValueTween<TValue>(from, to, duration, ease, safeUpdater, (a, b, t) => Lerp.Get<TValue>().Lerp(a, b, t), onComplete);
            TweenRunner.Instance.Register(tween);
            return tween;
        }


        /// <summary>
        /// Tweens a value from a start to an end over a duration, firing a custom callback on each update.
        /// This is for when you want to manually control what happens with the tweened value.
        /// </summary>

        public static ValueTween<T> TweenValue<T>(T from, T to, float duration,
                                              Action<T> onUpdate,
                                              ILerp<T> lerp,
                                              Func<float, float> ease = null,
                                              Action onComplete = null)
        {
            if (onUpdate == null) throw new ArgumentNullException(nameof(onUpdate));
            if (lerp == null) throw new ArgumentNullException(nameof(lerp));

            var tween = new ValueTween<T>(from, to, duration, ease,
                onUpdate,
                (a, b, t) => lerp.Lerp(a, b, t),
                onComplete);
            TweenRunner.Instance.Register(tween);
            return tween;
        }


        /// <summary>
        /// Tweens a property towards a dynamically evaluated target. Both the follower and target
        /// are guarded against destruction or deactivation.
        /// </summary>
        public static PropertyTween<TValue> Follow<TFollower, TTarget, TValue>(
            TFollower follower,                             // The object being moved
            Func<TFollower, TValue> getter,                 // How to get the follower's value
            Action<TFollower, TValue> setter,               // How to set the follower's value
            TTarget target,                                 // The object to follow
            Func<TTarget, TValue> toGetter,                 // How to get the target's value
            float duration,
            Func<float, float> ease = null,
            Action<TValue> onUpdate = null,
            Action onComplete = null)
            where TFollower : UnityEngine.Object
            where TTarget : UnityEngine.Object
        {
            // Initial checks to prevent starting a doomed tween
            if (follower == null)
            {
                Debug.LogWarning("Follower target is null. Tween will not be created.");
                return null;
            }
            if (target == null)
            {
                Debug.LogWarning("Follow target is null. Tween will not be created.");
                return null;
            }

            PropertyTween<TValue> tween = null;

            // 1. Guard the FOLLOWER
            void safeWriter(TValue value)
            {
                bool isFollowerInvalid = follower == null;
                if (!isFollowerInvalid && follower is Component comp && !comp.gameObject.activeInHierarchy)
                {
                    isFollowerInvalid = true;
                }

                if (isFollowerInvalid)
                {
                    tween?.Kill();
                    return;
                }

                setter(follower, value);
                onUpdate?.Invoke(value);
            }

            // 2. Guard the TARGET
            TValue safeEndGetter()
            {
                bool isTargetInvalid = target == null;
                if (!isTargetInvalid && target is Component comp && !comp.gameObject.activeInHierarchy)
                {
                    isTargetInvalid = true;
                }

                if (isTargetInvalid)
                {
                    // If the target disappears, stop
                    tween?.Kill();
                    // Return the start value so the last frame doesn't get a null reference
                    return tween.StartValue;
                }

                return toGetter(target);
            }

            // Use a temporary getter just to get the initial "from" value safely
            // We can't use the full safeEndGetter yet as the tween doesn't exist to be killed
            TValue from = getter(follower);

            tween = new PropertyTween<TValue>(from, safeEndGetter, duration, ease, safeWriter, Lerp.Get<TValue>(), onComplete);
            TweenRunner.Instance.Register(tween);
            return tween;
        }
    }



    #region Interfaces and Interpolation
    public interface ILerp<T>
    {
        T Lerp(T a, T b, float t);
    }

    public static class Lerp // built-ins
    {

        private static readonly Dictionary<Type, object> Lerpers = new()
    {
        { typeof(float), new Float() },
        { typeof(Vector2), new V2() },
        { typeof(Vector3), new V3() },
        { typeof(Color), new Color() },
    };

        public static ILerp<T> Get<T>()
        {
            if (Lerpers.TryGetValue(typeof(T), out var lerper))
            {
                return (ILerp<T>)lerper;
            }
            throw new NotSupportedException($"No built-in ILerp found for type {typeof(T)}");
        }

        public sealed class Float : ILerp<float> { public float Lerp(float a, float b, float t) => a + (b - a) * t; }
        public sealed class V2 : ILerp<Vector2> { public Vector2 Lerp(Vector2 a, Vector2 b, float t) => Vector2.LerpUnclamped(a, b, t); }
        public sealed class V3 : ILerp<Vector3> { public Vector3 Lerp(Vector3 a, Vector3 b, float t) => Vector3.LerpUnclamped(a, b, t); }
        public sealed class Color : ILerp<UnityEngine.Color> { public UnityEngine.Color Lerp(UnityEngine.Color a, UnityEngine.Color b, float t) => UnityEngine.Color.LerpUnclamped(a, b, t); }
        // Add Quaternion (slerp) etc. as needed.
    }
    #endregion


    #region Tween Implementations
    /// <summary>
    /// A common interface for the TweenRunner to manage active tweens polymorphically.
    /// </summary>
    public interface ITween
    {
        /// <summary>
        /// Advances the tween by deltaTime. Returns false when the tween is complete.
        /// </summary>
        bool IsActive { get; }
        bool Tick(float deltaTime);
        void Kill();
    }

    /// <summary>
    /// A tween that animates a value from a fixed start to a fixed end.
    /// </summary>
    public class ValueTween<T> : CustomYieldInstruction, ITween
    {
        // Configuration
        public T StartValue { get; private set; }
        public T EndValue { get; private set; }
        public float Duration { get; private set; }
        public Func<float, float> Ease { get; private set; }
        public Action<T> OnUpdate { get; private set; }
        public Action OnComplete { get; private set; }

        public bool IsActive => !_isComplete && !_isKilled;
        private bool _isKilled;

        public override bool keepWaiting => IsActive;
        // State
        private float _elapsedTime;
        private bool _isComplete;
        private readonly Func<T, T, float, T> _interpolator;

        public ValueTween(T start, T end, float duration, Func<float, float> ease,
                          Action<T> onUpdate, Func<T, T, float, T> interpolator, Action onComplete = null)
        {
            StartValue = start;
            EndValue = end;
            Duration = duration > 0 ? duration : 0.001f;
            Ease = ease ?? EasingFunctions.Linear; // Assuming EasingFunctions.Linear exists
            OnUpdate = onUpdate;
            OnComplete = onComplete;
            _interpolator = interpolator ?? ((a, b, t) => Lerp.Get<T>().Lerp(a, b, t));
            _elapsedTime = 0f;
            _isComplete = false;
        }

        public bool Tick(float deltaTime)
        {
            if (_isComplete || _isKilled) return false;

            _elapsedTime += deltaTime;
            float t01 = Mathf.Clamp01(_elapsedTime / Duration);
            float easedT = Ease(t01);

            T currentValue = _interpolator(StartValue, EndValue, easedT);
            OnUpdate?.Invoke(currentValue);

            if (_elapsedTime >= Duration)
            {
                _isComplete = true;
                if (!_isKilled)
                {
                    OnComplete?.Invoke();
                }
                return false;
            }
            return true;
        }
        public void Kill()
        {
            _isKilled = true;
        }
    }

    /// <summary>
    /// A tween that dynamically re-evaluates its end value every frame via a delegate.
    /// Useful for following moving targets.
    /// </summary>
    public sealed class PropertyTween<T> : CustomYieldInstruction, ITween
    {
        public T StartValue { get; private set; }
        public float Duration { get; private set; }
        public Func<float, float> Ease { get; private set; }
        public Action<T> OnUpdate { get; private set; }
        public Action OnComplete { get; private set; }

        private readonly Func<T> _endGetter;
        private readonly ILerp<T> _lerp;
        private float _elapsedTime;
        private bool _isComplete;

        public bool IsActive => !_isComplete && !_isKilled;
        public override bool keepWaiting => IsActive;

        private bool _isKilled;

        public PropertyTween(T start, Func<T> endGetter, float duration,
                             Func<float, float> ease, Action<T> onUpdate,
                             ILerp<T> lerp, Action onComplete = null)
        {
            StartValue = start;
            _endGetter = endGetter ?? throw new ArgumentNullException(nameof(endGetter));
            Duration = duration > 0f ? duration : 0.001f;
            Ease = ease ?? EasingFunctions.Linear;
            OnUpdate = onUpdate ?? throw new ArgumentNullException(nameof(onUpdate));
            _lerp = lerp ?? throw new ArgumentNullException(nameof(lerp));
            OnComplete = onComplete;
            _elapsedTime = 0f; _isComplete = false;
        }

        public bool Tick(float deltaTime)
        {
            if (_isComplete || _isKilled) return false;
            _elapsedTime += deltaTime;
            float t01 = Mathf.Clamp01(_elapsedTime / Duration);
            float easedT = Ease(t01);

            T currentEnd = _endGetter();
            T currentValue = _lerp.Lerp(StartValue, currentEnd, easedT);
            OnUpdate?.Invoke(currentValue);

            if (_elapsedTime >= Duration)
            {
                _isComplete = true;
                if (!_isKilled)
                {
                    OnComplete?.Invoke();
                }
                return false;
            }
            return true;
        }
        public void Kill()
        {
            _isKilled = true;
        }
    }
    #endregion


    #region Tween Runner
    /// <summary>
    /// A MonoBehaviour responsible for updating all active tweens.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TweenRunner : MonoBehaviour
    {
        private static TweenRunner _instance;
        private readonly List<ITween> _activeTweens = new();
        private readonly List<ITween> _tweensToAdd = new();

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
    #endregion
}