using UnityEngine;
namespace Survivor.Enemy.FSM
{
    public enum AttackCategory
    {
        Melee,
        Ranged
    }

    [System.Serializable]
    public class ScriptableAttackDefinition
    {
        [Tooltip("Is this a close-quarters or a long-distance attack? This determines which range band it's used in.")]
        public AttackCategory Category;

        [Tooltip("The ScriptableObject asset that defines the attack logic.")]
        public AttackPattern Pattern;

        [Tooltip("Unique identifier for this attack's cooldown.")]
        public string CooldownTag;

        [Tooltip("How long (in seconds) before this specific attack can be used again.")]
        public float Cooldown = 3.0f;

        [Tooltip("The chance for this attack to be chosen relative to others in the same category.")]
        public float Weight = 1.0f;
    }
}