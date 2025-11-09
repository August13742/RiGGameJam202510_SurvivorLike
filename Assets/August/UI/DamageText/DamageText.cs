using UnityEngine;
using Survivor.Game;
using TMPro;
using AugustsUtility.Tween;
using System.Collections.Generic;

namespace Survivor.UI
{
    [RequireComponent(typeof(PrefabStamp))]
    public sealed class DamageText : MonoBehaviour, IPoolable
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private DamageTextStyle style;

        private PrefabStamp _stamp;
        private Color _baseColor;
        private float _baseScale = 1f;

        private readonly List<ITween> _activeTweens = new ();

        private void Awake()
        {
            _stamp = GetComponent<PrefabStamp>();
            if (!label) label = GetComponentInChildren<TMP_Text>();
        }

        public void Show(Vector3 worldPos, string text, Color color, float scale, float lifetime, Vector3 initialOffset, float riseSpeed)
        {
            // Kill any tweens that might still be running on this pooled object.
            KillActiveTweens();

            // --- Set Initial State ---
            transform.position = worldPos + initialOffset;
            transform.localScale = Vector3.zero;
            label.text = text;
            _baseColor = color;
            _baseScale = scale;
            label.color = _baseColor;

            // --- Create Tweens ---
            var endPos = transform.position + Vector3.up * riseSpeed * lifetime;

            // Tween the position upwards, slowing down at the end.
            // When this tween completes, the object is returned to the pool.
            var moveTween = transform.TweenPosition(endPos, lifetime, EasingFunctions.EaseOutCubic, onComplete: ReturnToPool);

            var scaleTween = transform.TweenLocalScale(Vector3.one * _baseScale, lifetime, EasingFunctions.EaseOutElastic);

            var alphaTween = label.TweenColorAlpha(0f, lifetime, EasingFunctions.EaseInOutQuint);

            _activeTweens.Add(moveTween);
            _activeTweens.Add(scaleTween);
            _activeTweens.Add(alphaTween);
        }

        private void ReturnToPool()
        {
            // The onComplete action will only fire if the tween wasn't killed,
            // but we call KillActiveTweens() anyway to be safe.
            KillActiveTweens();
            if (_stamp.OwnerPool != null)
                _stamp.OwnerPool.Return(gameObject);
            else
                gameObject.SetActive(false);
        }

        private void KillActiveTweens()
        {
            if (_activeTweens.Count > 0)
            {
                foreach (var tween in _activeTweens)
                    tween?.Kill();
                _activeTweens.Clear();
            }
        }

        #region IPoolable
        public void OnSpawned()
        {
            if (label) label.color = Color.white;
        }

        public void OnDespawned()
        {
            KillActiveTweens();
        }
        #endregion

        #region Convenience Presets
        public void ShowNormal(Vector3 pos, float amount)
        {
            var s = style;
            Show(pos,
                 amount.ToString("F1"),
                 s ? s.normalColor : Color.white,
                 s ? s.normalScale : 1f,
                 s ? s.lifetime : 0.7f,
                 s ? new Vector3(Random.Range(-s.horizontalJitter, s.horizontalJitter), 0f, 0f) : Vector3.zero,
                 s ? s.riseSpeed : 2f);
        }

        public void ShowCrit(Vector3 pos, float amount)
        {
            var s = style;
            Show(pos,
                 $"{amount:F1}!",
                 s ? s.critColor : new Color(1f, 0.85f, 0.2f),
                 s ? s.critScale : 1.3f,
                 s ? s.lifetime : 0.7f,
                 s ? new Vector3(Random.Range(-s.horizontalJitter, s.horizontalJitter), 0f, 0f) : Vector3.zero,
                 s ? s.riseSpeed : 2f);
        }

        public void ShowHeal(Vector3 pos, float amount)
        {
            var s = style;
            Show(pos,
                 $"+{amount:F1}",
                 s ? s.healColor : new Color(0.2f, 1f, 0.2f),
                 s ? s.healScale : 1.0f,
                 s ? s.lifetime : 0.7f,
                 s ? new Vector3(Random.Range(-s.horizontalJitter, s.horizontalJitter), 0f, 0f) : Vector3.zero,
                 s ? s.riseSpeed : 2f);
        }
        #endregion
    }
}