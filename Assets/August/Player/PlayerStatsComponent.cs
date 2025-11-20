using UnityEngine;
using System.Collections.Generic;
using Survivor.Game;

namespace Survivor.Progression
{
    public class PlayerStatBonus
    {
        public float MoveSpeedBonus { get; set; } = 0f;
        public float PickupRadiusBonus { get; set; } = 0f;
        public int MaxHpBonus { get; set; } = 0;
        // Future additions: Armor, Damage Reduction, Greed (currency gain), etc.
    }

    [DisallowMultipleComponent]
    public sealed class PlayerStatsComponent : MonoBehaviour
    {
        // --- Base Stats ---
        [Header("Base Stats")]
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float basePickupRadius = 2f;
        [SerializeField] private int baseMaxHP = 100;

        // --- Live Data ---
        // all bonuses applied during the run
        private readonly List<PlayerStatBonus> _bonuses = new();

        // The final, calculated stats after applying all bonuses
        public EffectivePlayerStats EffectiveStats { get; private set; }

        private HealthComponent _healthComponent;

        private void Awake()
        {
            _healthComponent = GetComponent<HealthComponent>();
            if (_healthComponent == null)
            {
                Debug.LogError("PlayerStatsComponent requires a HealthComponent on the same GameObject.", this);
                return;
            }
            // Initialise with base stats on awake
            RecalculateStats();
        }

        /// <summary>
        /// Adds a new stat bonus and recalculates effective stats.
        /// </summary>
        public void AddBonus(PlayerStatBonus newBonus)
        {
            _bonuses.Add(newBonus);
            RecalculateStats();
        }

        /// <summary>
        /// Recalculates all effective stats from base values and the list of bonuses.
        /// This should be called whenever a bonus is added or removed.
        /// </summary>
        public void RecalculateStats()
        {
            // Pass the base stats and the list of bonuses to the aggregator.
            var baseStats = new BasePlayerStats(baseMoveSpeed, basePickupRadius, baseMaxHP);
            EffectiveStats = PlayerStatAggregator.ComputeEffective(baseStats, _bonuses);

            // Apply the calculated stats to other components.
            // Example: Update HealthComponent with new Max HP.
            if (_healthComponent)
            {
                // heal by the amount increased?
                float difference = EffectiveStats.MaxHP - _healthComponent.Max;
                _healthComponent.SetMaxHP(EffectiveStats.MaxHP);
                if (difference > 0)
                {
                    _healthComponent.Heal(difference);
                }
            }

            // Other components would read from EffectiveStats.
            // For example, a PlayerMovement script would use EffectiveStats.MoveSpeed.
        }
    }

    // Use "readonly struct" to enforce immutability.
    public readonly struct BasePlayerStats
    {
        public readonly float MoveSpeed;
        public readonly float PickupRadius;
        public readonly float MaxHP;

        public BasePlayerStats(float moveSpeed, float pickupRadius, float maxHP)
        {
            MoveSpeed = moveSpeed;
            PickupRadius = pickupRadius;
            MaxHP = maxHP;
        }
    }

    public readonly struct EffectivePlayerStats
    {
        public readonly float MoveSpeed;
        public readonly float PickupRadius;
        public readonly float MaxHP;

        // Internal constructor for the aggregator to use.
        internal EffectivePlayerStats(float moveSpeed, float pickupRadius, float maxHP)
        {
            MoveSpeed = moveSpeed;
            PickupRadius = pickupRadius;
            MaxHP = maxHP;
        }
    }

}