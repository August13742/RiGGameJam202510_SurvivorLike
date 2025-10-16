using System;
using UnityEngine;

public interface IDamageable { void Damage(int amount); void Heal(int amount); float GetCurrentPercent(); bool IsDead { get; } }

public sealed class HealthComponent : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP = 100;
    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public Action Died;
    public Action<int> HealthChanged;

    private void Awake() { Current = maxHP; }
    public void Damage(int amount)
    {
        if (IsDead) return;
        Current -= amount;
        if (Current <= 0) { Current = 0; Died?.Invoke(); }
    }
    public void Heal(int amount)
    {
        if (IsDead) return;
        if (amount < 0) return;
        Current += amount;
        HealthChanged?.Invoke(Current);
    }

    public float GetCurrentPercent()
    {
        if (IsDead) return 0;
        return Current / maxHP;
    }
}
