using Survivor.Game;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New Pattern_GapclostAttack", menuName = "Defs/Boss Attacks/Pattern_GapcloseAttack")]
    public class AttackPattern_GapcloseAttack : AttackPattern
    {
        [Header("Audio")]
        [SerializeField] private SFXResource dashSFX;     
        [SerializeField] private SFXResource attackSFX; 


        [Header("Dash Properties")]
        [SerializeField] private string gapCloseAnimationName = "Walk";
        [SerializeField] private float gapCloseAnimationSpeed = 1f;
        [SerializeField] private string attackAnimationName = "Attack1";
        [SerializeField] private float attackAnimationSpeed = 1f;
        [SerializeField] private string optionalAlternativeAnimName = "";
        [SerializeField] private float dashSpeedTarget = 15f;

        [Header("Enrage")]
        [SerializeField] private float enrageSpeedMul = 1.2f;  // faster das
        bool _enraged = false;
        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null)
                yield break;

            Transform bossTf = controller.transform;
            _enraged = controller.IsEnraged;

            // Apply enrage speed multiplier
            float currentDashSpeed = _enraged ? dashSpeedTarget * enrageSpeedMul : dashSpeedTarget;

            // --- Gap Closing
            controller.Animator.Play(gapCloseAnimationName);
            controller.Animator.speed = gapCloseAnimationSpeed * (_enraged ? enrageSpeedMul : 1f);
            AudioManager.Instance?.PlaySFX(dashSFX, bossTf.position,bossTf);

            while (!InMeleeRange(controller) && controller.PlayerTransform != null)
            {
                // Move towards the player
                Vector2 direction = (controller.PlayerTransform.position - controller.transform.position).normalized;
                controller.VelocityOverride = direction * currentDashSpeed;
                yield return null; // Wait for the next frame
            }

            // --- Attack Phase
            controller.VelocityOverride = Vector2.zero; // Stop movement
            controller.Animator.speed = attackAnimationSpeed * (_enraged ? enrageSpeedMul : 1f);

            if (optionalAlternativeAnimName != "")
            {
                controller.Animator.Play(Random.value > 0.5f ? attackAnimationName : optionalAlternativeAnimName);
            }
            else controller.Animator.Play(attackAnimationName);
            AudioManager.Instance?.PlaySFX(attackSFX, bossTf.position, bossTf);


            yield return new WaitForSeconds(1f);
        }

        private bool InMeleeRange(BossController controller)
        {
            return controller.GetBandToPlayer() == RangeBand.MeleeBand;
        }

    }

    
}

