using System.Collections;
using AugustsUtility.Telegraph;
using AugustsUtility.AudioSystem;
using Survivor.Control;
using Survivor.Game;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New RepulseShootPattern",
        menuName = "Defs/Boss Attacks/Sweep Repulse Then Shoot")]
    public sealed class AttackPattern_SweepingRepulseThenShoot : AttackPattern
    {
        [Header("SFX")]
        [SerializeField] private SFXResource repulseSFX;
        [SerializeField] private SFXResource shootSFX;

        [Header("Sweep Geometry")]
        [SerializeField] private float sweepArcDegrees = 60f;
        [SerializeField] private int steps = 6;
        [Tooltip("If true, aims the center of the arc at the player. If false, aims randomly once at start.")]
        [SerializeField] private bool aimAtPlayer = true;
        [Tooltip("If true, alternate left→right / right→left per cycle.")]
        [SerializeField] private bool pingPong = true;

        [Header("Phase 1: Repulse Box (visual + gating)")]
        [SerializeField] private Vector2 boxSize = new Vector2(10f, 2f);
        [SerializeField] private float telegraphDuration = 0.2f;
        [SerializeField] private float delayBetweenRepulses = 0.1f;
        [SerializeField] private Color repulseColor = new(0.5f, 0.8f, 1f, 0.5f);
        [SerializeField] private LayerMask pushMask;

        [Header("Repulse Logic (directional)")]
        [Tooltip("Maximum distance the player can be moved in a single repulse.")]
        [SerializeField] private float maxTravel = 6f;


        [Header("Phase 2: Projectile")]
        [SerializeField] private float delayBeforeShooting = 0.5f;
        [SerializeField] private float delayBetweenShots = 0.15f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float projectileDamage = 10f;
        [SerializeField] private float projectileLife = 5.0f;
        [SerializeField] private bool projectileHoming = false;
        [SerializeField] private float projectileHomingDuration = 1.5f;

        [Header("Probabilistic Repetition")]
        [Tooltip("If true, uses probability decay. If false, runs exactly once.")]
        [SerializeField] private bool isProbabilistic = true;
        [SerializeField] private int hardCap = 5;
        [SerializeField, Range(0f, 1f)] private float probDecay = 0.3f;
        [SerializeField] private float delayBetweenCycles = 0.8f;

        [Header("Enrage")]
        [SerializeField] private float enrageRateMul = 1.25f;
        [SerializeField] private float enrageDecayReduction = 0.5f; // Less decay = more spam when angry

        private static readonly Collider2D[] _hits = new Collider2D[16];

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || projectilePrefab == null)
                yield break;

            bool enraged = controller.IsEnraged;
            float rateMul = enraged ? enrageRateMul : 1f;

            float decay = isProbabilistic
                ? probDecay * (enraged ? enrageDecayReduction : 1f)
                : 1.0f; // non-probabilistic: one and done

            float p = 1.0f;
            int cycleCount = 0;

            // Snapshot base aim once, like SweepingBarrage, so the sector is stable.
            float baseAngle = 0f;
            if (aimAtPlayer && controller.PlayerTransform != null)
            {
                Vector2 diff = controller.PlayerTransform.position - controller.transform.position;
                baseAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            }
            else
            {
                baseAngle = Random.Range(0f, 360f);
            }

            // Main Loop: Repulse Sweep -> Shoot Sweep -> Repeat?
            while (Random.value <= p && cycleCount < hardCap)
            {
                bool isReverse = pingPong && (cycleCount % 2 != 0);

                // 1. Calculate Angles for this Cycle
                float[] angles = CalculateSweepAngles(baseAngle, sweepArcDegrees, steps, isReverse);

                // 2. Phase 1: The Repulse Barrage
                yield return ExecuteRepulsePhase(controller, angles, rateMul);

                // 3. Inter-Phase Delay
                yield return new WaitForSeconds(delayBeforeShooting / rateMul);

                // 4. Phase 2: Shooting Barrage (Follow-up)
                yield return ExecuteShootPhase(controller, angles, rateMul);

                // 5. Check Repeat
                p -= decay;
                cycleCount++;

                if (p > 0f && cycleCount < hardCap)
                {
                    yield return new WaitForSeconds(delayBetweenCycles / rateMul);
                }
            }
        }

        private IEnumerator ExecuteRepulsePhase(BossController controller, float[] angles, float rateMul)
        {
            float tDuration = telegraphDuration / rateMul;
            float stepDelay = delayBetweenRepulses / rateMul;
            Vector2 pivot = controller.transform.position;

            for (int i = 0; i < angles.Length; i++)
            {
                float angle = angles[i];
                Vector2 dir = DegreeToVector2(angle);
                Vector2 boxCenter = pivot + dir * (boxSize.x * 0.5f);

                // Visual Telegraph: sweeping wall segment
                Telegraph.Box(
                    host: controller,
                    pos: pivot,
                    size: boxSize,
                    angleDeg: angle,
                    duration: tDuration,
                    color: repulseColor
                );

                if (stepDelay > 0f)
                    yield return new WaitForSeconds(stepDelay);

                // Apply directional impulse, clamped to outer face of the box
                ApplyPushWithHelper(controller, pivot, boxCenter, dir, angle);

                AudioManager.Instance?.PlaySFX(repulseSFX);
            }
        }

        private void ApplyPushWithHelper(
            BossController controller,
            Vector2 pivot,
            Vector2 boxCenter,
            Vector2 dir,
            float angleDeg)
        {
            ContactFilter2D filter = new()
            {
                useTriggers = true,
                useDepth = false
            };
            filter.SetLayerMask(pushMask);

            int count = Physics2D.OverlapBox(boxCenter, boxSize, angleDeg, filter, _hits);
            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (col == null) continue;

                if (col.TryGetComponent<PlayerController>(out var pc))
                {
                    // Project the player onto the sweep direction (relative to pivot).
                    Vector2 playerPos = pc.transform.position;
                    float t = Vector2.Dot(playerPos - pivot, dir); // coordinate along dir

                    // Box goes from pivot (inner face) to pivot + dir * boxSize.x (outer face)
                    float maxT = boxSize.x;
                    float remaining = maxT - t;

                    // Behind outer face or numerically degenerate → no push.
                    if (remaining <= 0.01f)
                        continue;

                    float travel = Mathf.Min(maxTravel, remaining);
                    if (travel <= 0f)
                        continue;

                    DirectionalDisplacementUtility.ApplyDirectionalImpulse(
                        pc,
                        dir,
                        travel,
                        useDistanceGate: false
                    );
                }
            }
        }

        private IEnumerator ExecuteShootPhase(BossController controller, float[] angles, float rateMul)
        {
            float shotDelay = delayBetweenShots / rateMul;
            Vector2 pivot = controller.transform.position;

            for (int i = 0; i < angles.Length; i++)
            {
                float angle = angles[i];
                Vector2 dir = DegreeToVector2(angle);

                SpawnProjectile(controller, pivot, dir);
                AudioManager.Instance?.PlaySFX(shootSFX);

                if (shotDelay > 0f)
                    yield return new WaitForSeconds(shotDelay);
            }
        }

        private float[] CalculateSweepAngles(float baseAngle, float arc, int stepCount, bool reverse)
        {
            if (stepCount <= 0)
                stepCount = 1;

            float[] results = new float[stepCount];

            float startAngle = baseAngle - (arc * 0.5f);
            float stepSize = stepCount > 1 ? arc / (stepCount - 1) : 0f;

            for (int i = 0; i < stepCount; i++)
            {
                int index = reverse ? (stepCount - 1 - i) : i;
                results[i] = startAngle + (index * stepSize);
            }

            return results;
        }

        private void SpawnProjectile(BossController controller, Vector2 origin, Vector2 dir)
        {
            var go = Object.Instantiate(projectilePrefab, origin, Quaternion.identity);
            var bullet = go.GetComponent<Weapon.EnemyProjectile2D>();
            if (bullet != null)
            {
                bullet.Fire(
                    pos: origin,
                    dir: dir,
                    spd: projectileSpeed,
                    dmg: projectileDamage,
                    life: projectileLife,
                    target: controller.PlayerTransform,
                    homingOverride: projectileHoming,
                    homingSecondsOverride: projectileHomingDuration
                );
            }
            else
            {
                Debug.LogWarning("SweepingRepulseThenShoot projectilePrefab missing EnemyProjectile2D.");
                Object.Destroy(go);
            }
        }

        private static Vector2 DegreeToVector2(float degree)
        {
            float rad = degree * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
