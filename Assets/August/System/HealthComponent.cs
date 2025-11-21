using System;
using UnityEngine;


namespace Survivor.Game
{
    public enum DamageReception
    {
        Normal,
        Nullified, // Hit registers (sparks/sound) but 0 damage
        Absorb,    // Damage converts to Health
        Invincible // Hit is completely ignored
    }

    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        #region Configuration
        [Header("Stats")]
        [SerializeField, Min(1)] private float maxHP = 100f;
        [SerializeField] private float current;

        [Header("Debug / State")]
        [SerializeField] private bool godMode = false; // Essential for dev
        [SerializeField] private DamageReception receptionState = DamageReception.Normal;

        [Header("Reactions")]
        [SerializeField] private bool triggerHitstop = true;
        [SerializeField, Min(0.0f)] private float hitstopDuration = 0.12f;

        [Header("I-Frame Logic")]
        [Tooltip("If true, prevents multiple hits in rapid succession.")]
        [SerializeField] private bool useIframe = false;
        [SerializeField, Min(0.01f)] private float iframeDuration = 0.5f;

        [Header("Audio Integration")]
        [SerializeField] private SFXResource damageSfx;
        [SerializeField] private SFXResource healSfx;
        [SerializeField] private SFXResource deathSfx;
        #endregion

        #region Public API
        public float Max => maxHP;
        public float Current => current;
        public bool IsDead => current <= 0;
        public DamageReception Reception
        {
            get => receptionState;
            set => receptionState = value;
        }

        // Events
        // (Current, Max)
        public Action<float, float> HealthChanged;

        // (Amount, SourcePosition, IsCrit)
        public Action<float, Vector3, bool> Damaged;

        // (Amount, SourcePosition)
        public Action<float, Vector3> Healed;

        // (KillingBlowDirection, ExcessiveDamageAmount)
        public Action<Vector3, float> Died;
        #endregion

        #region Internal State
        private float _iframeTimer = 0f;
        private float _damageTakenInWindow = 0f;
        #endregion

        private void Awake()
        {
            current = maxHP;
        }

        private void Update()
        {
            if (_iframeTimer > 0)
            {
                _iframeTimer -= Time.deltaTime;
                if (_iframeTimer <= 0)
                {
                    _iframeTimer = 0f;
                    _damageTakenInWindow = 0f; // Reset threshold
                }
            }
        }

        public void SetMaxHP(float newMax, bool healToFull = true)
        {
            maxHP = Mathf.Max(1, newMax);
            if (healToFull)
                SetCurrent(maxHP);
            else
                SetCurrent(Mathf.Clamp(current, 0, maxHP));
        }

        public float GetCurrentPercent() => maxHP <= 0 ? 0f : Mathf.Clamp01(current / maxHP);

        public void Damage(float rawAmount, Vector3 sourcePos, bool isCrit = false)
        {
            if (IsDead || godMode) return;
            if (receptionState == DamageReception.Invincible) return;

            // 1. Handle Absorb
            if (receptionState == DamageReception.Absorb)
            {
                Heal(rawAmount, sourcePos);
                return;
            }

            // 2. Handle Nullify
            if (receptionState == DamageReception.Nullified)
            {
                // fire "HitBlocked" event here?
                return;
            }

            // 3. Handle I-Frame Thresholding
            float actualDamage = rawAmount;

            if (useIframe)
            {
                if (_iframeTimer > 0)
                {
                    // If we are already in iframes, only take damage if this hit is STRONGER
                    // than what we've already endured in this window
                    if (rawAmount <= _damageTakenInWindow) return;

                    actualDamage = rawAmount - _damageTakenInWindow;
                    _damageTakenInWindow = rawAmount; // Update high water mark
                }
                else
                {
                    // Start new window
                    _iframeTimer = iframeDuration;
                    _damageTakenInWindow = rawAmount;
                }
            }

            if (actualDamage <= 0) return;

            // 4. Apply
            float previous = current;
            float next = Mathf.Max(0, current - actualDamage);

            // 5. Side Effects & Events
            Damaged?.Invoke(actualDamage, sourcePos, isCrit);

            if (damageSfx != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(damageSfx, transform.position);

            if (triggerHitstop && HitstopManager.Instance != null)
                HitstopManager.Instance.Request(hitstopDuration, gameObject);

            SetCurrent(next);

            // 6. Death Logic
            if (current <= 0 && previous > 0)
            {
                Vector3 forceDir = (transform.position - sourcePos).normalized;
                float overkill = actualDamage - previous;
                DieInternal(forceDir, overkill);
            }
        }

        public void Heal(float amount, Vector3 sourcePos = default)
        {
            if (amount <= 0 || IsDead) return;

            // Effective Heal check
            float missing = maxHP - current;
            float effectiveHeal = Mathf.Min(missing, amount);

            if (effectiveHeal > 0)
            {
                Healed?.Invoke(effectiveHeal, sourcePos);

                if (healSfx != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(healSfx, transform.position);

                SetCurrent(current + effectiveHeal);
            }
        }

        public void Kill()
        {
            if (IsDead) return;
            SetCurrent(0);
            DieInternal(Vector3.zero, 0f);
        }

        private void DieInternal(Vector3 forceDir, float overkill)
        {
            if (deathSfx != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(deathSfx, transform.position);

            Died?.Invoke(forceDir, overkill);
        }

        private void SetCurrent(float val)
        {
            current = val;
            HealthChanged?.Invoke(current, maxHP);
        }
    }
}