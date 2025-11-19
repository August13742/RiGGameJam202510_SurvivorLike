using Survivor.Game;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New SweepingBarrage",
        menuName = "Defs/Boss Attacks/Sweeping Barrage")]
    public sealed class AttackPattern_SweepingBarrage : AttackPattern
    {
        [Header("Projectile Config")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float speed = 14f;
        [SerializeField] private float projectileLife = 3.0f;

        [Header("SFX")]
        [SerializeField] private SFXResource fireSFX;

        [Header("Sweep Geometry")]
        [Tooltip("Total arc width in degrees to cover.")]
        [SerializeField] private float sweepWidthDegrees = 45f;

        [Tooltip("Offset from the player direction to start the sweep. e.g. -22.5 starts at the left edge.")]
        [SerializeField] private float startOffsetDegrees = -22.5f;

        [Tooltip("If true, the start offset is randomized between left/right mirror.")]
        [SerializeField] private bool randomStartSide = true;

        [Header("Density")]
        [SerializeField] private int shotsPerSweep = 10;
        [SerializeField] private int sweepRepetitions = 1;
        [SerializeField] private bool pingPong = true; // If true, sweeps back and forth. If false, resets to start.

        [Header("Timing")]
        [SerializeField] private float delayBetweenShots = 0.05f;
        [SerializeField] private float delayBetweenSweeps = 0.2f;
        [SerializeField] private float initialWindup = 0.2f;

        [Header("Homing Options")]
        [SerializeField] private bool projectilesHome = false;
        [SerializeField] private float homingDuration = 1.0f;

        [Header("Enrage")]
        [SerializeField] private float enrageSpeedMul = 1.2f;  // Projectile speed
        [SerializeField] private float enrageRateMul = 1.5f;   // Fire rate
        [SerializeField] private int enrageExtraReps = 1;

        [Header("Probabilistic Repetitions")]
        [SerializeField] private bool repIsProbabilistic = false;
        [SerializeField, Range(0f, 1f)] private float probDecayPerSweep = 0.25f;
        [SerializeField, Range(0f, 1f)] private float enrageDecayReduction = 0.5f; // 50% less decay when enraged

        [Header("Animation")]
        [SerializeField] private string animName = "Barrage";

        public override IEnumerator Execute(BossController controller)
        {
            if (projectilePrefab == null || controller.PlayerTransform == null)
                yield break;

            // 1. Setup & Enrage calc
            bool enraged = controller.IsEnraged;
            float rateMul = enraged ? enrageRateMul : 1f;
            float spdMul = enraged ? enrageSpeedMul : 1f;

            // This is the "max" number of sweeps; with probabilistic enabled it becomes a hard cap.
            int totalReps = sweepRepetitions + (enraged ? enrageExtraReps : 0);
            totalReps = Mathf.Max(1, totalReps);

            float actualShotDelay = delayBetweenShots / rateMul;
            float actualSweepDelay = delayBetweenSweeps / rateMul;

            if (controller.Animator != null && !string.IsNullOrEmpty(animName))
            {
                controller.Animator.Play(animName);
            }

            // 2. Windup / Initial Aim Snapshot
            Vector2 pivot = controller.transform.position;
            Vector2 toPlayer = ((Vector2)controller.PlayerTransform.position - pivot).normalized;
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            // Decide once per pattern whether we flip to the mirrored side.
            bool flipStartSide = randomStartSide && Random.value > 0.5f;

            yield return new WaitForSeconds(initialWindup / rateMul);

            // 3. Execution Loop
            if (repIsProbabilistic)
            {
                yield return ExecuteProbabilisticSweeps(
                    controller,
                    baseAngle,
                    flipStartSide,
                    enraged,
                    totalReps,
                    actualShotDelay,
                    actualSweepDelay,
                    spdMul
                );
            }
            else
            {
                for (int r = 0; r < totalReps; r++)
                {
                    // Ping-pong: even reps go A->B, odd reps go B->A.
                    bool isReverse = pingPong && (r % 2 != 0);

                    float angleA = baseAngle + startOffsetDegrees;
                    float angleB = baseAngle + startOffsetDegrees + sweepWidthDegrees;

                    if (flipStartSide)
                    {
                        angleA = baseAngle - startOffsetDegrees;
                        angleB = baseAngle - startOffsetDegrees - sweepWidthDegrees;
                    }

                    float startAngle = isReverse ? angleB : angleA;
                    float endAngle = isReverse ? angleA : angleB;

                    yield return FireSweep(controller, startAngle, endAngle, shotsPerSweep, actualShotDelay, spdMul);

                    if (r < totalReps - 1)
                        yield return new WaitForSeconds(actualSweepDelay);
                }
            }
        }

        private IEnumerator ExecuteProbabilisticSweeps(
            BossController controller,
            float baseAngle,
            bool flipStartSide,
            bool enraged,
            int maxReps,
            float shotDelay,
            float sweepDelay,
            float speedMul)
        {
            float p = 1f;  // start: always do at least one sweep
            int rep = 0;
            int guard = 0;
            const int hardCap = 32;

            while (rep < maxReps && guard++ < hardCap && Random.value <= p)
            {
                bool isReverse = pingPong && (rep % 2 != 0);

                float angleA = baseAngle + startOffsetDegrees;
                float angleB = baseAngle + startOffsetDegrees + sweepWidthDegrees;

                if (flipStartSide)
                {
                    angleA = baseAngle - startOffsetDegrees;
                    angleB = baseAngle - startOffsetDegrees - sweepWidthDegrees;
                }

                float startAngle = isReverse ? angleB : angleA;
                float endAngle = isReverse ? angleA : angleB;

                yield return FireSweep(controller, startAngle, endAngle, shotsPerSweep, shotDelay, speedMul);

                rep++;

                // p decays each sweep, reduced decay when enraged
                float decay = probDecayPerSweep * (enraged ? enrageDecayReduction : 1f);
                p = Mathf.Max(0f, p - decay);

                if (rep < maxReps)
                    yield return new WaitForSeconds(sweepDelay);
            }
        }

        private IEnumerator FireSweep(
            BossController controller,
            float startAngleDeg,
            float endAngleDeg,
            int count,
            float delay,
            float speedMul)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0.5f : (float)i / (count - 1);
                float currentDeg = Mathf.Lerp(startAngleDeg, endAngleDeg, t);

                Vector2 fireDir = DegreeToVector2(currentDeg);
                SpawnProjectile(controller, fireDir, speedMul);

                if (i < count - 1)
                    yield return new WaitForSeconds(delay);
            }
        }

        private void SpawnProjectile(BossController controller, Vector2 dir, float speedMultiplier)
        {

            Vector2 origin = controller.FirePoint == null? controller.transform.position:controller.FirePoint.position;
            AugustsUtility.AudioSystem.AudioManager.Instance?.PlaySFX(fireSFX);

            var go = Object.Instantiate(projectilePrefab, origin, Quaternion.identity);

            var bullet = go.GetComponent<Weapon.EnemyProjectile2D>();
            if (bullet != null)
            {
                bullet.Fire(
                    pos: origin,
                    dir: dir,
                    spd: speed * speedMultiplier,
                    dmg: damage,
                    life: projectileLife,
                    target: controller.PlayerTransform,
                    homingOverride: projectilesHome,
                    homingSecondsOverride: homingDuration
                );
            }
            else
            {
                Debug.LogWarning("SweepingBarrage projectilePrefab missing EnemyProjectile2D.");
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
