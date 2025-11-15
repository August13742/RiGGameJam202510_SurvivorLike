using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [System.Serializable]
    public class BossConfig
    {
        public float MaxHealth = 1000f;
        public Color EnragedColour = Color.red;
        public float MeleeDamage = 10f;

        [Header("Movement")]
        public float ChaseSpeed = 3.5f;
        public float IdleWanderSpeed = 1.5f;

        [Header("Behaviour Ranges")]
        [Tooltip("Distance at which the boss will start chasing the player.")]
        public float EngageRange = 15f;
        [Tooltip("The MAXIMUM distance at which the boss will consider any attack.")]
        public float AttackRange = 8f;
        [Tooltip("The MINIMUM distance the boss wants to be for ranged attacks. Inside this, it prefers melee.")]
        public float RangedAttackMinRange = 4f;

        [Header("Combat Timings")]
        public float GlobalAttackCooldown = 1.0f;

        [Header("Facing Logic")]
        public float MinFlipInterval = 0.5f;
        [Range(0f, 1f)] public float FacingDeadzone = 0.2f;
        [Header("Range Hysteresis")]
        [Tooltip("Half-width of the buffer zone between Pocket and Melee to prevent jitter.")]
        public float MeleePocketHysteresis = 0.5f;

        [Header("Selection Weights")]
        [Tooltip("Applied to ranged attacks when the boss is in melee range.")]
        public float RangedInMeleeWeightMultiplier = 0.35f;

        [Header("Attack Patterns")]
        [Tooltip("A list of all possible attack patterns this boss can use.")]
        public ScriptableAttackDefinition[] AttackPatterns;

        [Header("Enrage Action")]
        [Tooltip("Special attack that should be forced once after the boss becomes enraged.")]
        public ScriptableAttackDefinition EnrageAction;

        [Tooltip("Only used if we want to bias instead of hard-force. Not strictly needed.")]
        public float EnrageWeightMultiplier = 9999f;
    }
}
