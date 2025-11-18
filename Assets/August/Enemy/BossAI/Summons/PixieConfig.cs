using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New PixieConfig",
        menuName = "Defs/Pixie Config")]
    public sealed class PixieConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Movement speed when adjusting distance to the player.")]
        public float followSpeed = 5f;

        [Tooltip("Preferred distance to hold from the player.")]
        public float preferredDistance = 6f;

        [Tooltip("How much slack around preferredDistance before adjusting.")]
        public float bandHalfWidth = 1.5f;

        [Header("Cast Band")]
        [Tooltip("Minimum distance from player at which the pixie is allowed to cast.")]
        public float castMinDistance = 3f;

        [Tooltip("Maximum distance from player at which the pixie is allowed to cast.")]
        public float castMaxDistance = 10f;

        [Header("Cast Loop")]
        public float initialDelay = 0.5f;
        public float castInterval = 1.6f;
        public float telegraphDuration = 0.6f;

        [Header("Roadblock Geometry")]
        [Tooltip("Distance from player base position to the circle center.")]
        public float stepSize = 3.5f;
        public float blastRadius = 1.8f;

        [Header("Roadblock Direction")]
        [Tooltip("0 = ignore input, 1 = fully follow player input direction when choosing where to block.")]
        [Range(0f, 1f)] public float inputPriority = 1f;

        [Tooltip("Random angular jitter (degrees) added around the chosen direction.")]
        public float maxAngleJitter = 20f;

        [Header("Damage & FX")]
        public float damage = 6f;
        public LayerMask hitMask;
        public bool showVFX = true;
        public GameObject vfxPrefab;
        public float cameraShakeStrength = 2f;
        public float cameraShakeDuration = 0.2f;

        [Header("Telegraph Visual")]
        public Color telegraphColor = Color.cyan;

        [Header("Lifetime")]
        public float lifeTime = 15f;

        [Header("Pixie HP")]
        public float maxHP = 20f;
    }
}
