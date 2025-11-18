using UnityEngine;

namespace Survivor.Weapon
{
    [System.Serializable]
    public sealed class WeaponLevelBonus
    {
        [Header("Damage & Cooldown")]
        [Tooltip("+X = X× increase in damage")]
        public float DamageMultiplierBonus = 0f;   // +0.20 = +20% damage
        [Tooltip("+X = X× reduction in cooldown time (0.1 = -10%)")]
        public float CooldownReduction = 0f;       // +0.10 = -10% cooldown

        [Header("Range & Speed")]
        public float AreaScaleBonus = 0f;          // +0.10 = +10% radius
        public float SpeedMultiplierBonus = 0f;    // +0.15 = +15% projectile speed

        [Header("Projectile & Pierce")]
        public int ProjectileCountBonus = 0;
        public int PierceCountBonus = 0;

        [Header("Critical Stats")]
        [Range(-1f, 1f)] public float CritChanceBonus = 0f;           // +0.05 = +5%
        public float CritDamageMultiplierBonus = 0f;                  // +0.25 = +0.25×

        public bool IsNoop()
        {
            return DamageMultiplierBonus == 0f &&
                   CooldownReduction == 0f &&
                   AreaScaleBonus == 0f &&
                   SpeedMultiplierBonus == 0f &&
                   ProjectileCountBonus == 0 &&
                   PierceCountBonus == 0 &&
                   Mathf.Approximately(CritChanceBonus, 0f) &&
                   Mathf.Approximately(CritDamageMultiplierBonus, 0f);
        }
    }
}
