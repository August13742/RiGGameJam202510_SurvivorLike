using AugustsUtility.Telegraph;
using Survivor.Game;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New RiptideWavesPattern", menuName = "Defs/Boss Attacks/Riptide Waves")]
    public sealed class AttackPattern_RiptideWaves : AttackPattern
    {
        [Header("Geometry")]
        [SerializeField] private float segmentSpacing = 3f;    // distance between each wave
        [SerializeField] private float maxDistance = 20f;      // clamp for safety
        [SerializeField] private float blastRadius = 1.8f;

        [Header("Timing")]
        [SerializeField] private float waveInterval = 0.4f;    // delay between waves
        [SerializeField] private float telegraphDuration = 0.6f;

        [Header("Damage")]
        [SerializeField] private float damage = 8f;
        [SerializeField] private LayerMask hitMask;

        [Header("Telegraph Visual")]
        [SerializeField] private Color telegraphColor = Color.cyan;

        [Header("Channeling")]
        [SerializeField] private string channelAnim = "Cast";
        [Tooltip("Minimum time the boss stays in channel animation while waves are being spawned.")]
        [SerializeField] private float channelDuration = 1.0f;

        [Header("Enrage")]
        [SerializeField] private float enragedRateMul = 1.25f;
        [SerializeField] private float enragedDamageMul = 1.3f;

        private static readonly Collider2D[] _hits = new Collider2D[16];

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null)
                yield break;

            bool enraged = controller.IsEnraged;
            float rateMul = enraged ? enragedRateMul : 1f;
            float dmg = damage * (enraged ? enragedDamageMul : 1f);

            // Use the *behavior pivot* for distances, same as your range gizmos
            Vector2 start = controller.BehaviorPivotWorld;
            Vector2 end = controller.PlayerTransform.position;
            Vector2 delta = end - start;
            float dist = Mathf.Min(delta.magnitude, maxDistance);

            if (dist < 0.1f)
                yield break;

            int waveCount = Mathf.Max(1, Mathf.FloorToInt(dist / segmentSpacing));

            // Start all waves as independent coroutines.
            for (int i = 0; i < waveCount; i++)
            {
                float delay = (waveInterval / rateMul) * i;
                controller.StartCoroutine(
                    RiptideWaveRoutine(
                        controller,
                        waveIndex: i,
                        delay: delay,
                        telegraphTime: telegraphDuration / rateMul,
                        radius: blastRadius,
                        damage: dmg
                    )
                );
            }

            // Boss channels for a fixed time, but does NOT wait for all waves to finish.
            float channel = channelDuration / rateMul;
            if (channel > 0f && controller.Animator != null && !string.IsNullOrEmpty(channelAnim))
            {
                controller.VelocityOverride = Vector2.zero;
                controller.Animator.Play(channelAnim);
                controller.Animator.speed = rateMul;

                yield return new WaitForSeconds(channel);

                controller.Animator.Play("Idle");
                controller.Animator.speed = 1f;
                controller.VelocityOverride = Vector2.zero;
            }
            // After this, state machine is free to pick the next state/attack
        }

        private IEnumerator RiptideWaveRoutine(
            BossController controller,
            int waveIndex,
            float delay,
            float telegraphTime,
            float radius,
            float damage)
        {
            if (controller == null)
                yield break;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (controller == null || controller.PlayerTransform == null)
                yield break;

            // --- Continuous-ish aiming: recompute line at time of launch ---
            Vector2 bossPos = controller.BehaviorPivotWorld;
            Vector2 playerPos = controller.PlayerTransform.position;
            Vector2 toPlayer = playerPos - bossPos;

            float dist = Mathf.Min(toPlayer.magnitude, maxDistance);
            if (dist < 0.1f)
                yield break;

            Vector2 dir = toPlayer / dist;

            float d = Mathf.Min(segmentSpacing * (waveIndex + 1), dist);
            Vector2 basePos = bossPos + dir * d;

            // Telegraph at basePos
            yield return Telegraph.Circle(
                host: controller,
                pos: basePos,
                radius: radius,
                duration: telegraphTime,
                color: telegraphColor
            );

            // --- Explosion time: recompute homed position along the same line ---
            if (controller == null)
                yield break;

            Vector2 explodePos = basePos;

            if (controller.PlayerTransform != null)
            {
                Vector2 newBossPos = controller.BehaviorPivotWorld;
                Vector2 lineDir = (basePos - newBossPos).sqrMagnitude > 1e-6f
                    ? (basePos - newBossPos).normalized
                    : Vector2.right;

                Vector2 toPlayerNow = (Vector2)controller.PlayerTransform.position - newBossPos;
                float proj = Vector2.Dot(toPlayerNow, lineDir);
                if (proj > 0f)
                {
                    float clampedProj = Mathf.Min(proj, maxDistance);
                    explodePos = newBossPos + lineDir * clampedProj;
                }
            }

            // --- Explosion damage (independent of boss state) ---
            ContactFilter2D filter = new ContactFilter2D { useTriggers = true, useDepth = false };
            filter.SetLayerMask(hitMask);

            int hitCount = Physics2D.OverlapCircle(explodePos, radius, filter, _hits);
            for (int i = 0; i < hitCount; i++)
            {
                if (_hits[i] == null) continue;
                if (!_hits[i].TryGetComponent<HealthComponent>(out var hp)) continue;
                if (hp.IsDead) continue;

                hp.Damage(damage);
            }
        }
    }
}
