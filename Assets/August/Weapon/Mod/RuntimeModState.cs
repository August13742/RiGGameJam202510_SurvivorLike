namespace Survivor.Weapon
{
    /// Per-tick, reset-each-frame modifiers that mods write into.
    [System.Serializable]
    public sealed class RuntimeModState
    {
        // same semantics as WeaponLevelBonus
        public float DamageMultiplierBonus;     // +0.20 = +20% dmg
        public float CooldownReduction;         // +0.10 = -10% cooldown
        public float AreaScaleBonus;            // +0.10 = +10% area
        public float SpeedMultiplierBonus;      // +0.15 = +15% speed

        public int ProjectileCountBonus;
        public int PierceCountBonus;

        public float CritChanceBonus;           // +0.05 = +5pp
        public float CritDamageMultiplierBonus; // +0.25 = +0.25×

        public void Clear()
        {
            DamageMultiplierBonus = 0f;
            CooldownReduction = 0f;
            AreaScaleBonus = 0f;
            SpeedMultiplierBonus = 0f;
            ProjectileCountBonus = 0;
            PierceCountBonus = 0;
            CritChanceBonus = 0f;
            CritDamageMultiplierBonus = 0f;
        }
    }
}
