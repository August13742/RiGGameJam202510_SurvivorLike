using System;
using UnityEngine;

namespace AugustsUtility.Tween
{
    public static class TweenExtensions
    {
        #region "Static" Tween Extensions
        // --- Transform Extensions ---
        public static ValueTween<Vector3> TweenPosition(this Transform target, Vector3 to, float duration, Func<float, float> ease = null, Action onComplete = null)
        {
            return Tween.TweenProperty(target, t => t.position, (t, v) => t.position = v, to, duration, ease, null, onComplete);
        }

        public static ValueTween<Vector3> TweenLocalPosition(this Transform target, Vector3 to, float duration, Func<float, float> ease = null, Action onComplete = null)
        {
            return Tween.TweenProperty(target, t => t.localPosition, (t, v) => t.localPosition = v, to, duration, ease, null, onComplete);
        }

        public static ValueTween<Vector2> TweenAnchoredPosition(this RectTransform target, Vector2 to, float duration, Func<float, float> ease = null, Action onComplete = null)
        {
            return Tween.TweenProperty(target, t => t.anchoredPosition, (t, v) => t.anchoredPosition = v, to, duration, ease, null, onComplete);
        }

        public static ValueTween<Vector3> TweenLocalScale(this Transform target, Vector3 to, float duration, Func<float, float> ease = null, Action onComplete = null)
        {
            return Tween.TweenProperty(target, t => t.localScale, (t, v) => t.localScale = v, to, duration, ease, null, onComplete);
        }

        // --- CanvasGroup Extensions ---
        public static ValueTween<float> TweenAlpha(this CanvasGroup target, float to, float duration, Func<float, float> ease = null, Action onComplete = null)
        {
            return Tween.TweenProperty(target, t => t.alpha, (t, v) => t.alpha = v, to, duration, ease, null, onComplete);
        }

        // --- Renderer/Graphic Extensions ---
        public static ValueTween<float> TweenColorAlpha(this UnityEngine.UI.Graphic target, float to, float duration, Func<float, float> ease = null, Action onComplete = null)
        {
            return Tween.TweenProperty(target, t => t.color.a, (t, v) =>
            {
                Color c = t.color;
                c.a = v;
                t.color = c;
            }, to, duration, ease, null, onComplete);
        }

        public static ValueTween<float> TweenColorAlpha(this SpriteRenderer target, float to, float duration, Func<float, float> ease = null, Action onComplete = null)
        {
            return Tween.TweenProperty(target, t => t.color.a, (t, v) =>
            {
                Color c = t.color;
                c.a = v;
                t.color = c;
            }, to, duration, ease, null, onComplete);
        }

        public static ValueTween<Color> TweenColor(this UnityEngine.UI.Graphic target, Color to, float duration, Func<float, float> ease = null, Action onComplete = null)
        {
            return Tween.TweenProperty(target, t => t.color, (t, v) => t.color = v, to, duration, ease, null, onComplete);
        }

        #endregion

        #region Dynamic Tweens (Follow)
        public static PropertyTween<Vector3> TweenFollowPosition(
                this Transform follower,
                Transform target,
                float duration,
                Func<float, float> ease = null,
                Action onComplete = null)
        {
            return Tween.Follow(
                follower,
                f => f.position,                // getter
                (f, v) => f.position = v,       // setter
                target,
                t => t.position,                // target's getter
                duration,
                ease,
                null,
                onComplete);
        }
        #endregion



    }
}