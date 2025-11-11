namespace Survivor.Weapon
{
    /// Runtime-effective stats derived from WeaponDef base + bonuses.
    public sealed class EffectiveWeaponStats
    {
        public float Damage;              // BaseDamage × (1 + Σ bonus)
        public float Cooldown;            // BaseCooldown × (1 - Σ reduction)
        public float AreaScale;           // AreaScale × (1 + Σ bonus)
        public int Projectiles;         // Base + Σ bonus
        public int Pierce;              // Base (type-specific) + Σ bonus (see note)
        public float CritChance;          // clamp01(base + Σ)
        public float CritMultiplier;      // ≥ 1

        // Type-specific factors the runtime can consume:
        public float SpeedFactor;         // (1 + Σ SpeedMultiplierBonus)

        // convenience getters for UI
        public float DamageMultiplierFromBase; // (Damage / BaseDamage)
        public float CooldownMultiplierFromBase; // (Cooldown / BaseCooldown)
        public float AreaMultiplierFromBase; // (AreaScale / BaseArea)
    }
}
