using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New FlamethrowerPattern",
        menuName = "Defs/Boss Attacks/Flamethrower")]
    public sealed class AttackPattern_Flamethrower : AttackPattern
    {
        [Header("Prefab")]
        [SerializeField] private GameObject flamethrowerPrefab;

        [Header("Duration")]
        [SerializeField] private float minDuration = 1.0f;
        [SerializeField] private float maxDuration = 3.0f;

        [Header("Damage")]
        [Tooltip("Base damage per second dealt by the flame.")]
        [SerializeField] private float damagePerSecond = 12f;

        [Tooltip("Multiplier applied to DPS when enraged.")]
        [SerializeField] private float enragedDamageMultiplier = 1.5f;

        [Tooltip("How often to tick damage (seconds).")]
        [SerializeField] private float tickInterval = 0.1f;

        [Tooltip("Which layers can be damaged by the flamethrower.")]
        [SerializeField] private LayerMask targetMask;

        [Header("Beam Geometry & Behaviour")]
        [Tooltip("World length of the prefab when root.localScale.x == 1 (collider length).")]
        [SerializeField] private float basePrefabLength = 0.65f;

        [Tooltip("Min/max flamethrower range in world units (independent of player distance).")]
        [SerializeField] private float minRange = 3.0f;
        [SerializeField] private float maxRange = 6.0f;

        [Tooltip("How fast the beam extends (scale-units per second). Set high to be almost instant.")]
        [SerializeField] private float lengthGrowSpeed = 20f;

        [Tooltip("How fast the thickness grows.")]
        [SerializeField] private float thicknessGrowSpeed = 10f;

        [Tooltip("Target thickness multiplier at full flame.")]
        [SerializeField] private float targetThicknessMul = 1.5f;

        [Header("Homing")]
        [Tooltip("Max turn rate in deg/s toward the player.")]
        [SerializeField] private float maxTurnRateDeg = 90f;

        [Header("Initial Aim Randomisation")]
        [Tooltip("Random point around player is sampled in this radius range, then aimed at.")]
        [SerializeField] private float initialAimRadiusMin = 0.5f;
        [SerializeField] private float initialAimRadiusMax = 1.5f;

        [Header("Animations (optional)")]
        [SerializeField] private string windupAnim = "CastStart";
        [SerializeField] private float windupTime = 0.3f;
        [SerializeField] private string loopAnim = "CastLoop";
        [SerializeField] private string endAnim = "Idle";

        [Header("Behaviour")]
        [Tooltip("If true, boss movement is frozen while casting.")]
        [SerializeField] private bool lockMovement = true;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null || flamethrowerPrefab == null)
                yield break;

            Transform firePoint = controller.FirePoint != null
                ? controller.FirePoint
                : controller.transform;

            bool enraged = controller.IsEnraged;

            float duration = Random.Range(minDuration, maxDuration);
            float dps = damagePerSecond * (enraged ? enragedDamageMultiplier : 1f);
            float beamRange = Random.Range(minRange, maxRange);

            // --- 0. Prep movement/anim ---
            if (lockMovement)
            {
                controller.Velocity = Vector2.zero;
                controller.VelocityOverride = Vector2.zero;
                controller.Direction = Vector2.zero;
            }

            if (controller.Animator != null && !string.IsNullOrEmpty(windupAnim))
            {
                controller.Animator.Play(windupAnim);
                controller.Animator.speed = 1f;
            }

            if (windupTime > 0f)
            {
                yield return new WaitForSeconds(windupTime);
            }

            if (controller.Animator != null && !string.IsNullOrEmpty(loopAnim))
            {
                controller.Animator.Play(loopAnim);
                controller.Animator.speed = 1f;
            }

            // --- 1. Spawn beam prefab ---
            GameObject go = Object.Instantiate(
                flamethrowerPrefab,
                firePoint.position,
                Quaternion.identity);

            FlamethrowerBeam beam = go.GetComponentInChildren<FlamethrowerBeam>();
            if (beam == null)
            {
                Debug.LogWarning("FlamethrowerPattern: prefab missing FlamethrowerBeam.", flamethrowerPrefab);
                Object.Destroy(go);
                yield break;
            }

            // --- 1.5 Compute initial aim: random point around player ---
            Vector2 originPos = firePoint.position;
            Vector2 playerPos = controller.PlayerTransform.position;

            float rMin = Mathf.Max(0f, initialAimRadiusMin);
            float rMax = Mathf.Max(rMin, initialAimRadiusMax);

            // Sample random direction + radius around player
            Vector2 randDir = Random.insideUnitCircle.normalized;
            float randRadius = Random.Range(rMin, rMax);

            Vector2 aimPoint = playerPos + randDir * randRadius;
            Vector2 initialDir = aimPoint - originPos;

            if (initialDir.sqrMagnitude < 0.0001f)
            {
                // Degenerate case: fall back to player direction
                initialDir = playerPos - originPos;
            }

            // --- 2. Configure beam behaviour (movement + DoT) ---
            beam.Configure(
                origin: firePoint,
                target: controller.PlayerTransform,
                basePrefabLength: basePrefabLength,
                desiredWorldLength: beamRange,
                targetThicknessMul: targetThicknessMul,
                lengthGrowSpeed: lengthGrowSpeed,
                thicknessGrowSpeed: thicknessGrowSpeed,
                maxTurnRateDeg: maxTurnRateDeg,
                damagePerSecond: dps,
                tickInterval: tickInterval,
                targetMask: targetMask,
                initialDirection: initialDir.normalized);

            // --- 3. Maintain for duration ---
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (lockMovement)
                {
                    controller.Velocity = Vector2.zero;
                    controller.VelocityOverride = Vector2.zero;
                    controller.Direction = Vector2.zero;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // --- 4. Cleanup ---
            if (controller.Animator != null && !string.IsNullOrEmpty(endAnim))
            {
                controller.Animator.Play(endAnim);
                controller.Animator.speed = 1f;
            }

            if (go != null)
            {
                Object.Destroy(go);
            }

            if (lockMovement)
            {
                controller.Velocity = Vector2.zero;
                controller.VelocityOverride = Vector2.zero;
                controller.Direction = Vector2.zero;
            }
        }
    }
}
