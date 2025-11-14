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
                // Optional: allow single-renderer fallback
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

            // Position + rotation
            transform.SetPositionAndRotation(
                p.WorldPos,
                Quaternion.Euler(0f, 0f, p.AngleDeg)
            );

            // Target scale assuming sprite is unit radius (1 unit = radius 1)
            float radiusScale = p.Radius * 2f;
            Vector3 targetScale = _baseOutlineScale * radiusScale;

            // --- Outline: full range, static ---
            outlineRenderer.transform.localScale = targetScale;
            Color outlineColor = p.Color;
            outlineColor.a = 1.0f;
            outlineRenderer.color = outlineColor;

            // --- Fill: starts small and faint, grows to match outline ---
            fillRenderer.transform.localScale = Vector3.zero;

            Color fillColor = p.Color;
            fillColor.a = startFillAlpha;
            fillRenderer.color = fillColor;

            // 1) Scale tween: 0 → targetScale
            fillRenderer.transform.TweenLocalScale(
                targetScale,
                _duration,
                EasingFunctions.EaseOutCubic
            );

            // 2) Alpha tween: startFillAlpha -> endFillAlpha, then finish
            fillRenderer.TweenColorAlpha(
                endFillAlpha,
                _duration,
                EasingFunctions.Linear,
                onComplete: Finish
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

        // Optional: explicit cancel API if need to cut telegraph short
        public void Cancel()
        {
            if (!_active) return;
            _active = false;
            gameObject.SetActive(false);
            _onFinished = null;
            // If start keeping tween handles, call Kill() here.
        }
    }
}
