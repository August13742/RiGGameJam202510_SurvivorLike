using System;
using UnityEngine;
using AugustsUtility.Tween;
using static AugustsUtility.Telegraph.TelegraphDefinition;

namespace AugustsUtility.Telegraph
{
    public sealed class TelegraphInstance : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer outlineRenderer;
        [SerializeField] private SpriteRenderer fillRenderer;

        [Header("Visual Tuning")]
        [SerializeField] private float startFillAlpha = 0.25f;
        [SerializeField] private float endFillAlpha = 1.0f;

        // State fields
        private float _duration;
        private Action _onFinished;
        private bool _active;

        // Dynamic position fields
        private Func<Vector3> _posProvider;
        private bool _isDynamic;

        private Vector3 _baseOutlineScale;
        private Vector3 _baseFillScale;

        private void Awake()
        {
            if (outlineRenderer == null) outlineRenderer = GetComponent<SpriteRenderer>();
            if (fillRenderer == null) fillRenderer = outlineRenderer;
            _baseOutlineScale = outlineRenderer.transform.localScale;
            _baseFillScale = fillRenderer.transform.localScale;
        }

        public void Begin(TelegraphParams p, Action onFinished)
        {
            _duration = Mathf.Max(0.01f, p.Duration);
            _onFinished = onFinished;
            _posProvider = p.WorldPosProvider;
            _isDynamic = p.IsDynamic;
            _active = true;

            gameObject.SetActive(true);

            // Set initial position and rotation from the provider
            transform.SetPositionAndRotation(
                _posProvider(),
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
                    SetupCircle(p);
                    break;
            }
        }
        private void Update()
        {
            if (!_active || !_isDynamic || _posProvider == null) return;

            // Poll the provider for the current position
            transform.position = _posProvider();
        }

        private void Finish()
        {
            if (!_active) return;
            _active = false;

            // Reset state for object pool
            _posProvider = null;
            _isDynamic = false;

            gameObject.SetActive(false);

            Action cb = _onFinished;
            _onFinished = null;
            cb?.Invoke();
        }

        public void Cancel()
        {
            if (!_active) return;
            _active = false;

            // Reset state for object pool
            _posProvider = null;
            _isDynamic = false;

            gameObject.SetActive(false);
            _onFinished = null;
        }

        // --- Setup Methods (SetupCircle, SetupBox) have no changes ---
        private void SetupCircle(TelegraphParams p)
        {
            float radiusScale = p.Radius;
            Vector3 targetScale = _baseOutlineScale * radiusScale;
            outlineRenderer.transform.localScale = targetScale;
            Color outlineColor = p.Color;
            outlineColor.a = 1.0f;
            outlineRenderer.color = outlineColor;
            fillRenderer.transform.localScale = Vector3.zero;
            Color fillColor = p.Color;
            fillColor.a = startFillAlpha;
            fillRenderer.color = fillColor;
            fillRenderer.transform.TweenLocalScale(targetScale, _duration, EasingFunctions.EaseOutCubic);
            fillRenderer.TweenColorAlpha(endFillAlpha, _duration, EasingFunctions.Linear, Finish);
        }

        private void SetupBox(TelegraphParams p)
        {
            Vector3 targetScale = new Vector3(_baseOutlineScale.x * p.Size.x, _baseOutlineScale.y * p.Size.y, _baseOutlineScale.z);
            outlineRenderer.transform.localScale = targetScale;
            Color outlineColor = p.Color;
            outlineColor.a = 1.0f;
            outlineRenderer.color = outlineColor;
            fillRenderer.transform.localScale = new Vector3(0f, targetScale.y, targetScale.z);
            Color fillColor = p.Color;
            fillColor.a = startFillAlpha;
            fillRenderer.color = fillColor;
            fillRenderer.transform.TweenLocalScale(targetScale, _duration, EasingFunctions.EaseOutCubic);
            fillRenderer.TweenColorAlpha(endFillAlpha, _duration, EasingFunctions.Linear, Finish);
        }
    }
}