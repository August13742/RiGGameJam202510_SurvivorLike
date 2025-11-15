using UnityEngine;
using System;

namespace Survivor.Weapon
{
    public static class WeaponStatAggregator
    {
        public static EffectiveWeaponStats ComputeEffective(
            WeaponDef def,
            ReadOnlySpan<WeaponLevelBonus> bonuses,
            int typeBasePierce /* pass 0 for non-projectile, or ProjectileWeaponDef.Pierce */)
        {
            // Accumulate linear bonuses (multiplicative-on-base behavior)
            float dmgMul = 0f, cdRed = 0f, areaMul = 0f, speedMul = 0f;
            int projAdd = 0, pierceAdd = 0;
            float critChanceAdd = 0f, critMultAdd = 0f;

            for (int i = 0; i < bonuses.Length; i++)
            {
                var b = bonuses[i];
                dmgMul += b.DamageMultiplierBonus;
                cdRed += b.CooldownReduction;
                areaMul += b.AreaScaleBonus;
                speedMul += b.SpeedMultiplierBonus;
                projAdd += b.ProjectileCountBonus;
                pierceAdd += b.PierceCountBonus;
                critChanceAdd += b.CritChanceBonus;
                critMultAdd += b.CritDamageMultiplierBonus;
            }

            float damage = def.BaseDamage * (1f + dmgMul);
            float cooldown = def.BaseCooldown * Mathf.Max(0.01f, 1f - cdRed);
            float area = def.AreaScale * (1f + areaMul);
            int proj = Mathf.Max(1, def.Projectiles + projAdd);
            int pierce = Mathf.Max(0, typeBasePierce + pierceAdd);

            float cChance = Mathf.Clamp01(def.BaseCritChance + critChanceAdd);
            float cMult = Mathf.Max(1f, def.BaseCritMultiplier + critMultAdd);

            return new EffectiveWeaponStats
            {
                Damage = damage,
                Cooldown = cooldown,
                AreaScale = area,
                Projectiles = proj,
                Pierce = pierce,
                CritChance = cChance,
                CritMultiplier = cMult,
                SpeedFactor = 1f + speedMul,

                DamageMultiplierFromBase = (def.BaseDamage > 0f) ? damage / def.BaseDamage : 1f,
                CooldownMultiplierFromBase = (def.BaseCooldown > 0f) ? cooldown / def.BaseCooldown : 1f,
                AreaMultiplierFromBase = (def.AreaScale > 0f) ? area / def.AreaScale : 1f
            };
        }
    }
}
