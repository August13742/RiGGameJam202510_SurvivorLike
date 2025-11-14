using System;
using UnityEngine;
using AugustsUtility.Tween;
using static AugustsUtility.Telegraph.TelegraphDefinition;

namespace AugustsUtility.Telegraph
{
    public sealed class TelegraphInstance : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer outlineRenderer;   // final range
        [SerializeField] private SpriteRenderer fillRenderer;      // growing part

        [Header("Visual Tuning")]
        [SerializeField] private float startFillAlpha = 0.25f;
        [SerializeField] private float endFillAlpha = 1.0f;

        private float _duration;
        private Action _onFinished;

        private Vector3 _baseOutlineScale;
        private Vector3 _baseFillScale;

        private bool _active;

        private void Awake()
        {
            if (outlineRenderer == null)
            {
                outlineRenderer = GetComponent<SpriteRenderer>();
            }

            if (fillRenderer == null)
            {
                fillRenderer = outlineRenderer;
            }

            _baseOutlineScale = outlineRenderer.transform.localScale;
            _baseFillScale = fillRenderer.transform.localScale;
        }

        public void Begin(TelegraphParams p, Action onFinished)
        {
            _duration = Mathf.Max(0.01f, p.Duration);
            _onFinished = onFinished;
            _active = true;

            gameObject.SetActive(true);

            transform.SetPositionAndRotation(
                p.WorldPos,
                Quaternion.Euler(0f, 0f, p.AngleDeg)
            );

            switch (p.Shape)
            {
                case TelegraphShape.Circle:
                    SetupCircle(p);
                    break;

                case TelegraphShape.Box:
                    SetupBox(p);
                    break;

                default:
                    // fallback: treat as circle
                    SetupCircle(p);
                    break;
            }
        }

        private void SetupCircle(TelegraphParams p)
        {
            float radiusScale = p.Radius * 2f; // unit circle has diameter 2
            Vector3 targetScale = _baseOutlineScale * radiusScale;

            // Outline = full range
            outlineRenderer.transform.localScale = targetScale;
            Color outlineColor = p.Color;
            outlineColor.a = 1.0f;
            outlineRenderer.color = outlineColor;

            // Fill = grow from center
            fillRenderer.transform.localScale = Vector3.zero;

            Color fillColor = p.Color;
            fillColor.a = startFillAlpha;
            fillRenderer.color = fillColor;

            // scale tween (0 -> full radius)
            fillRenderer.transform.TweenLocalScale(
                targetScale,
                _duration,
                EasingFunctions.EaseOutCubic
            );

            // alpha tween
            fillRenderer.TweenColorAlpha(
                endFillAlpha,
                _duration,
                EasingFunctions.Linear,
                Finish
            );
        }

        private void SetupBox(TelegraphParams p)
        {
            // p.Size is in world units (width, height)
            Vector3 targetScale = new Vector3(
                _baseOutlineScale.x * p.Size.x,
                _baseOutlineScale.y * p.Size.y,
                _baseOutlineScale.z
            );

            // Outline = final box immediately
            outlineRenderer.transform.localScale = targetScale;
            Color outlineColor = p.Color;
            outlineColor.a = 1.0f;
            outlineRenderer.color = outlineColor;

            // Fill: horizontal bar from left to right
            fillRenderer.transform.localScale = new Vector3(
                0f,
                targetScale.y,
                targetScale.z
            );

            Color fillColor = p.Color;
            fillColor.a = startFillAlpha;
            fillRenderer.color = fillColor;

            // scale X 0 -> full width, Y constant
            fillRenderer.transform.TweenLocalScale(
                targetScale,
                _duration,
                EasingFunctions.EaseOutCubic
            );

            fillRenderer.TweenColorAlpha(
                endFillAlpha,
                _duration,
                EasingFunctions.Linear,
                Finish
            );
        }

        private void Finish()
        {
            if (!_active) return;

            _active = false;
            gameObject.SetActive(false);

            Action cb = _onFinished;
            _onFinished = null;
            cb?.Invoke();
        }

        public void Cancel()
        {
            if (!_active) return;
            _active = false;
            gameObject.SetActive(false);
            _onFinished = null;
        }
    }
}
