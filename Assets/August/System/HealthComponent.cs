using System;
using UnityEngine;

namespace Survivor.Game
{
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private float maxHP = 100f;
        [SerializeField] private float current;
        [SerializeField] private bool resetToFullOnEnable = true;

        public float Max => maxHP;
        public float Current => current;
        public bool IsDead => current <= 0;


        public Action<float, float> HealthChanged;
        public Action Died;
        public Action<float, Vector3, bool> Damaged; //amount, worldPos, crit?

        private void Awake()
        {
            current = Mathf.Max(1, maxHP); // authoring-safe
        }

        private void OnEnable()
        {
            if (resetToFullOnEnable)
                SetCurrent(maxHP, raiseEvent: true); // one path, one invoke
        }

        // POLICY: emulate auto-disconnect (listeners must rewire on enable / player swap)
        private void OnDisable()
        {
            HealthChanged = null;
            Died = null;
            Damaged = null;
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
            float next = Mathf.Max(0, current - amount);
            if (next == current) return;
            Damaged?.Invoke(amount, transform.position, crit);

            SetCurrent(next, raiseEvent: true);
            if (next == 0) Died?.Invoke();
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