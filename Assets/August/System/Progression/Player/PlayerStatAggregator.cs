using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Progression
{
    public static class PlayerStatAggregator
    {
        public static EffectivePlayerStats ComputeEffective(
            BasePlayerStats baseStats,
            IReadOnlyList<PlayerStatBonus> bonuses)
        {
            // 1. Accumulate all bonuses from the list.
            float moveSpeedBonus = 0f;
            float pickupRadiusBonus = 0f;
            int maxHpBonus = 0;

            foreach (var b in bonuses)
            {
                moveSpeedBonus += b.MoveSpeedBonus;
                pickupRadiusBonus += b.PickupRadiusBonus;
                maxHpBonus += b.MaxHpBonus;
            }

            // 2. Apply bonuses to base stats.
            // Multiplicative bonuses are added together before being applied once.
            // e.g., two +10% bonuses become (1 + 0.1 + 0.1) = 1.2x multiplier.
            float finalMoveSpeed = baseStats.MoveSpeed * (1f + moveSpeedBonus);
            float finalPickupRadius = baseStats.PickupRadius * (1f + pickupRadiusBonus);

            // Flat bonuses are simply added on top.
            float finalMaxHp = baseStats.MaxHP + maxHpBonus;

            return new EffectivePlayerStats(
                Mathf.Max(0f, finalMoveSpeed),
                Mathf.Max(0f, finalPickupRadius),
                Mathf.Max(1, finalMaxHp)
            );
        }
    }
}