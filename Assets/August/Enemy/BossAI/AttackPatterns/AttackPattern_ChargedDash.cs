using AugustsUtility.CameraShake;
using AugustsUtility.Telegraph;
using AugustsUtility.Tween;
using Survivor.Game;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New ChargedDashPattern", menuName = "Defs/Boss Attacks/Charged Dash")]
    public sealed class AttackPattern_ChargedDash : AttackPattern
    {
        [Header("Dash Geometry")]
        [SerializeField] private float minDashDistance = 4f;
        [SerializeField] private float maxDashDistance = 8f;
        [SerializeField] private float dashWidth = 2.0f;

        [Header("Targeting")]
        [Tooltip("How far past the player's projected position to dash, along the dash direction.")]
        [SerializeField] private float overshootPadding = 1.0f;

        [Header("Timing")]
        [SerializeField] private float telegraphDuration = 0.6f;
        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private float postDashDelay = 0.2f;

        [Header("Telegraph Visual")]
        [SerializeField] private Color telegraphColor = new Color(1f, 0.3f, 0.1f, 1f);

        [Header("Damage")]
        [SerializeField] private float dashDamage = 15f;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private float damageBoxLengthPadding = 0.2f;

        [Header("Hitstop")]
        [SerializeField] private float hitstopDuration = 0.08f;   // 0 => off
        [Header("Camera Shake")]
        [SerializeField] private float cameraShakeStrength = 5f;
        [SerializeField] private float cameraShakeDuration = 0.5f;

        [Header("Repetitions (probabilistic)")]
        [SerializeField] private bool repIsProbabilistic = true;
        [SerializeField] private int repetitions = 2;
        [SerializeField, Range(0f, 1f)] private float probDecayPerDash = 0.35f;
        [SerializeField] private int hardCap = 5;

        [Header("Enrage")]
        [SerializeField] private float enragedRateMul = 1.25f;
        [SerializeField] private float enragedDashDistanceMul = 1.25f;
        [SerializeField] private float enragedDamageMul = 1.4f;
        [SerializeField, Range(0f, 1f)] private float enragedDecayReduction = 0.5f;
        [SerializeField] private int enragedExtraFixedReps = 1;

        [Header("Animation (optional)")]
        [SerializeField] private string chargeAnim = "Charge";
        [SerializeField] private string dashAnim = "Dash";
        [SerializeField] private string endAnim = "Idle";

        private static readonly Collider2D[] _hits = new Collider2D[16];

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null)
                yield break;

            bool enraged = controller.IsEnraged;
            float rateMul = enraged ? enragedRateMul : 1f;
            float dmgMul = enraged ? enragedDamageMul : 1f;

            float telegraphTime = telegraphDuration / rateMul;
            float dashTime = dashDuration / rateMul;
            float pauseTime = postDashDelay / rateMul;

            float decay = repIsProbabilistic
                ? probDecayPerDash * (enraged ? enragedDecayReduction : 1f)
                : probDecayPerDash;

            float finalDamage = dashDamage * dmgMul;

            float minDist = Mathf.Max(0.1f, minDashDistance * (enraged ? enragedDashDistanceMul : 1f));
            float maxDist = Mathf.Max(minDist, maxDashDistance * (enraged ? enragedDashDistanceMul : 1f));

            if (repIsProbabilistic)
            {
                float p = 1f;
                int guard = 0;

                while (Random.value <= p && guard++ < hardCap)
                {
                    yield return DoOneDash(controller, minDist, maxDist, telegraphTime, dashTime, finalDamage);
                    p = Mathf.Max(0f, p - decay);

                    if (pauseTime > 0f)
                        yield return new WaitForSeconds(pauseTime);
                }
            }
            else
            {
                int reps = repetitions + (enraged ? enragedExtraFixedReps : 0);
                reps = Mathf.Max(1, reps);

                for (int i = 0; i < reps; i++)
                {
                    yield return DoOneDash(controller, minDist, maxDist, telegraphTime, dashTime, finalDamage);

                    if (i < reps - 1 && pauseTime > 0f)
                        yield return new WaitForSeconds(pauseTime);
                }
            }

            if (controller.Animator != null && !string.IsNullOrEmpty(endAnim))
            {
                controller.Animator.Play(endAnim);
                controller.Animator.speed = 1f;
            }
        }

        private IEnumerator DoOneDash(
            BossController controller,
            float minDist,
            float maxDist,
            float telegraphTime,
            float dashTime,
            float damage)
        {
            if (controller == null || controller.PlayerTransform == null)
                yield break;

            Transform bossTf = controller.transform;

            // Snapshot pivot and direction
            Vector2 pivotStart = controller.BehaviorPivotWorld;
            Vector2 toPlayer = (Vector2)controller.PlayerTransform.position - pivotStart;

            if (toPlayer.sqrMagnitude < 0.0001f)
            {
                toPlayer = bossTf.right; // fallback
            }

            Vector2 dir = toPlayer.normalized;
            float angDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Project player along dash direction and overshoot
            float distAlongDir = Vector2.Dot(toPlayer, dir);
            if (distAlongDir < 0f) distAlongDir = 0f;

            float desiredDist = distAlongDir + overshootPadding;
            float dashDist = Mathf.Clamp(desiredDist, minDist, maxDist);

            Vector2 rootStart = bossTf.position;
            Vector2 rootEnd = rootStart + dir * dashDist;

            // Telegraph lane: start at pivot, extend dashDist units
            Vector3 telegraphPos = pivotStart;
            Vector2 telegraphSize = new Vector2(dashDist, dashWidth);

            // Charge / telegraph phase
            controller.Velocity = Vector2.zero;
            controller.VelocityOverride = Vector2.zero;
            controller.Direction = Vector2.zero;

            if (controller.Animator != null && !string.IsNullOrEmpty(chargeAnim))
            {
                controller.Animator.Play(chargeAnim);
                controller.Animator.speed = 1f;
            }

            yield return Telegraph.Box(
                host: controller,
                pos: telegraphPos,
                size: telegraphSize,
                angleDeg: angDeg,
                duration: telegraphTime,
                color: telegraphColor
            );

            if (controller.Animator != null && !string.IsNullOrEmpty(dashAnim))
            {
                controller.Animator.Play(dashAnim);
                controller.Animator.speed = 1f;
            }

            controller.Velocity = Vector2.zero;
            controller.VelocityOverride = Vector2.zero;
            controller.Direction = Vector2.zero;

            yield return bossTf.TweenPosition(
                rootEnd,
                dashTime,
                EasingFunctions.EaseInOutQuint
            );

            bossTf.position = rootEnd;
            controller.Velocity = Vector2.zero;
            controller.VelocityOverride = Vector2.zero;

            // Damage along full lane between pivotStart and projected endpoint
            DoDashImpactDamage(pivotStart, dir, dashDist, damage, controller.gameObject);
        }

        private void DoDashImpactDamage(
            Vector2 pivotStart,
            Vector2 dir,
            float dashDist,
            float damage,
            GameObject bossRoot)
        {
            float length = dashDist + damageBoxLengthPadding * 2f;
            Vector2 size = new (length, dashWidth);

            Vector2 pivotEnd = pivotStart + dir * dashDist;
            Vector2 center = (pivotStart + pivotEnd) * 0.5f;
            float angDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            ContactFilter2D filter = new() { useTriggers = true, useDepth = false };
            filter.SetLayerMask(hitMask);

            int hitCount = Physics2D.OverlapBox(center, size, angDeg, filter, _hits);

            bool anyHit = false;

            for (int i = 0; i < hitCount; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                if (!col.TryGetComponent<HealthComponent>(out var hp)) continue;
                if (hp.IsDead) continue;

                anyHit = true;
                hp.Damage(damage);

                CameraShake2D.Shake(cameraShakeDuration, cameraShakeStrength);
                if (hitstopDuration > 0f)
                {
                    HitstopManager.Instance.RequestGlobal(hitstopDuration);
                }
            }

            if (anyHit && hitstopDuration > 0f && bossRoot != null)
            {
                HitstopManager.Instance.RequestGlobal(hitstopDuration);
            }
        }
    }
}
