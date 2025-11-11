using Survivor.Game;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New DashAttackPattern", menuName = "Defs/Boss Attacks/Dash Pattern")]
    public class DashAttackPattern : AttackPattern
    {
        [Header("Dash Properties")]
        [SerializeField] private string dashAnimationName = "RapidRotate";
        [SerializeField] private float telegraphTime = 0.5f;
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.4f;
        [SerializeField] private float recoveryTime = 0.2f;

        [Header("Repetitions (fixed)")]
        [SerializeField] private int dashCount = 2;
        [SerializeField] private int enragedDashCount = 3;

        [Header("Repetitions (probabilistic)")]
        [SerializeField] private bool repIsProbabilistic = false;
        [SerializeField, Range(0f, 1f)] private float probDecayPerDash = 0.25f; // p starts at 1 and decays by this each dash
        [SerializeField] private int hardCap = 32; // safety

        [Header("Enrage")]
        [SerializeField, Range(0f, 1f)] private float healthThresholdForEnrage = 0.5f;
        [SerializeField] private float enrageSpeedMul = 1.15f;  // faster dash
        [SerializeField] private float enrageRateMul = 1.10f;   // faster telegraph/recovery cadence

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null)
                yield break;

            // Health snapshot
            float hp = 1f;
            var hc = controller.GetComponent<HealthComponent>();
            if (hc != null) hp = hc.GetCurrentPercent();
            bool enraged = hp <= healthThresholdForEnrage;

            // Enrage scalars
            float speed = enraged ? dashSpeed * enrageSpeedMul : dashSpeed;
            float rateMul = enraged ? enrageRateMul : 1f;

            if (repIsProbabilistic)
            {
                yield return ExecuteProbabilistic(controller, speed, rateMul);
            }
            else
            {
                int reps = Mathf.Max(0, enraged ? enragedDashCount : dashCount);
                for (int i = 0; i < reps; i++)
                    yield return DoOneDash(controller, speed, rateMul);
            }
        }

        private IEnumerator ExecuteProbabilistic(BossController controller, float speed, float rateMul)
        {
            float p = 1f;
            int guard = 0;

            while (Random.value <= p && guard++ < hardCap)
            {
                yield return DoOneDash(controller, speed, rateMul);
                p = Mathf.Max(0f, p - probDecayPerDash);
            }
        }

        private IEnumerator DoOneDash(BossController controller, float speed, float rateMul)
        {
            // 1) Telegraph
            if (controller.Animator != null)
                controller.Animator.Play("Attack1");
            Vector2 targetPos = controller.PlayerTransform.position; // lock target position at telegraph start
            yield return new WaitForSeconds(telegraphTime / rateMul);

            // 2) Dash
            Vector2 dir = ((Vector2)targetPos - (Vector2)controller.transform.position).normalized;
            controller.Velocity = dir * speed;
            if (controller.Animator != null && !string.IsNullOrEmpty(dashAnimationName))
                controller.Animator.Play(dashAnimationName);
            yield return new WaitForSeconds(dashDuration / rateMul);

            // 3) Recover
            controller.Velocity = Vector2.zero;
            if (controller.Animator != null)
                controller.Animator.Play("Idle");
            yield return new WaitForSeconds(recoveryTime / rateMul);
        }
    }
}
