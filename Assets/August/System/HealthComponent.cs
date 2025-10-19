using System;
using UnityEngine;

namespace Survivor.Game
{
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHP = 100;
        [SerializeField] private int current;
        [SerializeField] private bool resetToFullOnEnable = true;

        public int Max => maxHP;
        public int Current => current;
        public bool IsDead => current <= 0;

        // Consider Action<int,int> (old,new) if you want richer info.
        public event Action<int> HealthChanged;
        public event Action Died;

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
        }

        public void SetMaxHP(int hp, bool resetCurrent = true, bool raiseEvent = true)
        {
            maxHP = Mathf.Max(1, hp);
            if (resetCurrent)
                SetCurrent(maxHP, raiseEvent);
            else
                SetCurrent(Mathf.Clamp(current, 0, maxHP), raiseEvent);
        }

        public void ResetFull(bool raiseEvent = true) => SetCurrent(maxHP, raiseEvent);

        public float GetCurrentPercent() => maxHP <= 0 ? 0f : Mathf.Clamp01((float)current / maxHP);

        public void Damage(int amount)
        {
            if (amount <= 0 || IsDead) return;
            int next = Mathf.Max(0, current - amount);
            if (next == current) return;
            SetCurrent(next, raiseEvent: true);
            if (next == 0) Died?.Invoke();
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;
            int next = Mathf.Min(maxHP, current + amount);
            if (next != current) SetCurrent(next, raiseEvent: true);
        }

        public void Kill()
        {
            if (IsDead) return;
            SetCurrent(0, raiseEvent: true);
            Died?.Invoke();
        }

        private void SetCurrent(int value, bool raiseEvent)
        {
            value = Mathf.Clamp(value, 0, maxHP);
            if (value == current) return;
            current = value;
            if (raiseEvent) HealthChanged?.Invoke(current);
        }
    }
}