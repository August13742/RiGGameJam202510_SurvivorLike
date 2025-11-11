using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New DashAttackPattern", menuName = "Defs/Boss Attacks/Dash Pattern")]
    public class DashAttackPattern : AttackPattern
    {
        [Header("Dash Properties")]
        [SerializeField] private string dashAnimationName = "Attack1";
        [SerializeField] private float telegraphTime = 0.5f;
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.4f;
        [SerializeField] private float recoveryTime = 0.2f;

        [Header("Repetitions")]
        [SerializeField] private int dashCount = 2;
        [SerializeField] private int enragedDashCount = 3;
        [SerializeField] private float healthThresholdForEnrage = 0.5f;

        public override IEnumerator Execute(BossController controller)
        {
            // Example of accessing character health - assumes a HealthComponent exists
            // float healthPercent = controller.GetComponent<HealthComponent>().GetHealthPercent();
            float healthPercent = 0.8f; // Using a placeholder for now

            int dashesToPerform = healthPercent < healthThresholdForEnrage ? enragedDashCount : dashCount;

            for (int i = 0; i < dashesToPerform; i++)
            {
                // 1. Telegraph: Warn the player
                controller.Animator.Play("Telegraph");
                Vector2 targetPos = controller.PlayerTransform.position; // Lock on to position
                yield return new WaitForSeconds(telegraphTime);

                // 2. Execute: Perform the dash
                Vector2 direction = (targetPos - (Vector2)controller.transform.position).normalized;
                controller.Velocity = direction * dashSpeed;
                controller.Animator.Play(dashAnimationName);
                yield return new WaitForSeconds(dashDuration);

                // 3. Recover: Stop and pause briefly
                controller.Velocity = Vector2.zero;
                controller.Animator.Play("Idle");
                yield return new WaitForSeconds(recoveryTime);
            }
        }
    }
}