using UnityEngine;
using Survivor.Game;
using TMPro;

namespace Survivor.UI
{
    [RequireComponent(typeof(PrefabStamp))]
    public sealed class DamageText : MonoBehaviour, IPoolable
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private DamageTextStyle style;

        private PrefabStamp _stamp;
        private float _t;              // elapsed
        private float _life;           // duration
        private Vector3 _vel;          // rise direction
        private Color _baseColor;
        private float _baseScale = 1f;
        private bool _active;

        private void Awake()
        {
            _stamp = GetComponent<PrefabStamp>();
            if (!label) label = GetComponentInChildren<TMP_Text>();
        }

        public void Show(Vector3 worldPos, string text, Color color, float scale, float lifetime, Vector3 initialOffset, float riseSpeed)
        {
            transform.position = worldPos + initialOffset;
            label.text = text;
            _baseColor = color;
            _baseScale = scale;
            _life = Mathf.Max(0.05f, lifetime);
            _vel = Vector3.up * riseSpeed;
            _t = 0f;
            _active = true;

            // immediate first frame
            ApplyVisuals(0f);
        }

        private void Update()
        {
            if (!_active) return;

            float dt = Time.deltaTime;
            _t += dt;

            // move
            transform.position += _vel * dt;

            // visuals
            float u = Mathf.Clamp01(_t / _life);
            ApplyVisuals(u);

            if (_t >= _life)
                ReturnToPool();
        }

        private void ApplyVisuals(float u)
        {
            // alpha/scale curves
            float a = style ? style.alphaOverLife.Evaluate(u) : (1f - u);
            float s = style ? style.scaleOverLife.Evaluate(u) : 1f;

            var c = _baseColor; c.a *= a;
            label.color = c;
            float scale = _baseScale * s;
            transform.localScale = Vector3.one * scale;
        }

        private void ReturnToPool()
        {
            _active = false;
            if (_stamp?.OwnerPool != null) _stamp.OwnerPool.Return(gameObject);
            else gameObject.SetActive(false);
        }

        // --- IPoolable ---
        public void OnSpawned()
        {
            _active = true;
            // ensure fully visible on spawn
            if (label) label.alpha = 1f;
        }

        public void OnDespawned()
        {
            _active = false;
        }

        // Convenience presets
        public void ShowNormal(Vector3 pos, int amount)
        {
            var s = style;
            Show(pos,
                 amount.ToString(),
                 s ? s.normalColor : Color.white,
                 s ? s.normalScale : 1f,
                 s ? s.lifetime : 0.7f,
                 s ? new Vector3(Random.Range(-s.horizontalJitter, s.horizontalJitter), 0f, 0f) : Vector3.zero,
                 s ? s.riseSpeed : 2f);
        }

        public void ShowCrit(Vector3 pos, int amount)
        {
            var s = style;
            Show(pos,
                 amount.ToString(),
                 s ? s.critColor : new Color(1f, 0.85f, 0.2f),
                 s ? s.critScale : 1.3f,
                 s ? s.lifetime : 0.7f,
                 s ? new Vector3(Random.Range(-s.horizontalJitter, s.horizontalJitter), 0f, 0f) : Vector3.zero,
                 s ? s.riseSpeed : 2f);
        }

        public void ShowHeal(Vector3 pos, int amount)
        {
            var s = style;
            Show(pos,
                 $"+{amount}",
                 s ? s.healColor : new Color(0.2f, 1f, 0.2f),
                 s ? s.healScale : 1.0f,
                 s ? s.lifetime : 0.7f,
                 s ? new Vector3(Random.Range(-s.horizontalJitter, s.horizontalJitter), 0f, 0f) : Vector3.zero,
                 s ? s.riseSpeed : 2f);
        }
    }
}