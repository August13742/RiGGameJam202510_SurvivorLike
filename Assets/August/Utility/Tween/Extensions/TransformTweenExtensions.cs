using System;
using UnityEngine;

public static class TransformTweenExtensions
{
    /// <summary>
    /// Tweens the position of a Transform to follow a target Transform.
    /// The target's position is re-evaluated each frame.
    /// </summary>
    /// <param name="subject">The Transform to move.</param>
    /// <param name="target">The Transform to follow.</param>
    /// <param name="duration">The time in seconds for the tween to complete.</param>
    /// <param name="ease">The easing function to use.</param>
    /// <param name="onComplete">An optional action called when the tween finishes.</param>
    /// <returns>The created tween instance.</returns>
    public static DynamicTween<Vector3> TweenPosition(
        this Transform subject, // extension in C#, this tells the compiler to extend Transform's signature
        Transform target,
        float duration,
        Func<float, float> ease,
        Action onComplete = null)
    {
        // The lambda () => target.position is the dynamic getter.
        // It captures the 'target' variable and returns its position whenever called.
        var tween = new DynamicTween<Vector3>(
            start: subject.position,
            endValueGetter: () => target.position, // Dynamically get target's position
            duration: duration,
            ease: ease,
            onUpdate: (currentPos) => {
                // Add a null check in case the object is destroyed during the tween
                if (subject != null)
                {
                    subject.position = currentPos;
                }
            },
            interpolator: Vector3.LerpUnclamped,
            onComplete: onComplete
        );

        TweenRunner.Instance.Register(tween);
        return tween;
    }
}