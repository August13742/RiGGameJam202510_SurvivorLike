using System;
using UnityEngine;

namespace Survivor.Game
{
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField] private int maxHP = 100;
        [SerializeField] private int current;

        [SerializeField] private bool resetToFullOnEnable = true;

        public int Max => maxHP;
        public int Current => current;
        public bool IsDead => current <= 0;

        public event Action Died;
        public event Action<int> HealthChanged;

        private void Awake()
        {
            // Initialise once; don't invoke events in Awake (listeners may not be attached yet).
            current = Mathf.Max(1, maxHP);
        }

        private void OnEnable()
        {
            if (resetToFullOnEnable)
            {
                ResetFull(raiseEvent: true);
            }
        }

        /// <summary>Setter for external systems (e.g., spawner) to define per-spawn HP cap.</summary>
        public void SetMaxHP(int hp, bool resetCurrent = true, bool raiseEvent = true)
        {
            maxHP = Mathf.Max(1, hp);
            if (resetCurrent) ResetFull(raiseEvent);
        }

        public void ResetFull(bool raiseEvent = true)
        {
            current = Mathf.Max(1, maxHP);
            if (raiseEvent) HealthChanged?.Invoke(current);
        }

        public float GetCurrentPercent()
        {
            if (maxHP <= 0) return 0f;
            return Mathf.Clamp01((float)current / maxHP);
        }

        public void Damage(int amount)
        {
            if (amount <= 0 || IsDead) return;

            current -= amount;
            if (current <= 0)
            {
                current = 0;
                HealthChanged?.Invoke(current);
                Died?.Invoke();
                return;
            }

            HealthChanged?.Invoke(current);
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;

            int newVal = Mathf.Min(current + amount, maxHP);
            if (newVal != current)
            {
                current = newVal;
                HealthChanged?.Invoke(current);
            }
        }
    }
}
