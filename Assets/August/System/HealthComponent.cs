using System;
using UnityEngine;

namespace Survivor.Game
{
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private float maxHP = 100f;
        [SerializeField] private float current;
        //[SerializeField] private bool resetToFullOnEnable = true;
        [SerializeField] private bool triggerHitstopOnDamaged = true;
        [SerializeField,Min(0.01f)] private float hitstopDurationSec = 0.15f;
        [SerializeField] private bool useIframe = false;
        [SerializeField, Min(0.01f)] private float iframeDuration = 0.05f;
        public float Max => maxHP;
        public float Current => current;
        public bool IsDead => current <= 0;


        public Action<float, float> HealthChanged;
        public Action Died;
        public Action<float, Vector3, bool> Damaged; //amount, worldPos, crit?

        private float iframeTimer = 0f;
        private void Awake()
        {
            current = Mathf.Max(1, maxHP); // authoring-safe
            ResetFull();
        }


        public void DisconnectAllSignals()
        {
            HealthChanged = null;
            Died = null;
            Damaged = null;
        }
        private void Update()
        {
            if (!useIframe) return;
            if (iframeTimer >= 0) iframeTimer -= Time.deltaTime;
            iframeTimer = Mathf.Max(0f,iframeTimer);

        }

        public void SetMaxHP(float hp, bool resetCurrent = true, bool raiseEvent = true)
        {
            maxHP = Mathf.Max(1, hp);
            if (resetCurrent)
                SetCurrent(maxHP, raiseEvent);
            else
                SetCurrent(Mathf.Clamp(current, 0, maxHP), raiseEvent);
        }

        public void ResetFull(bool raiseEvent = true) => SetCurrent(maxHP, raiseEvent);

        public float GetCurrentPercent() => maxHP <= 0 ? 0f : Mathf.Clamp01((float)current / maxHP);

        public void Damage(float amount, bool crit = false)
        {
            if (amount <= 0 || IsDead) return;
            if (useIframe && (iframeTimer > 0)) return;

            float next = Mathf.Max(0, current - amount);
            if (next == current) return;
            Damaged?.Invoke(amount, transform.position, crit);
            SetCurrent(next, raiseEvent: true);

            if (next == 0) Died?.Invoke();

            if (triggerHitstopOnDamaged && (HitstopManager.Instance != null)) HitstopManager.Instance.Request(hitstopDurationSec, gameObject);
            if (useIframe) iframeTimer = iframeDuration;
        }

        public void Heal(float amount)
        {
            if (amount <= 0 || IsDead) return;
            float next = Mathf.Min(maxHP, current + amount);
            if (next != current) SetCurrent(next, raiseEvent: true);
        }

        public void Kill()
        {
            if (IsDead) return;
            SetCurrent(0, raiseEvent: true);
            Died?.Invoke();
        }

        private void SetCurrent(float value, bool raiseEvent)
        {
            value = Mathf.Clamp(value, 0, maxHP);
            if (value == current) return;
            float previous = current;
            current = value;
            if (raiseEvent) HealthChanged?.Invoke(current, previous);
        }
    }
}