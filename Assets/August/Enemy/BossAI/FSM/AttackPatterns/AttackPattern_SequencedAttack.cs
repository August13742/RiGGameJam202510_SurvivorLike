using Survivor.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New SequencedAttackPattern", menuName = "Defs/Boss Attacks/Sequenced Attack Pattern")]
    public class AttackPattern_SequencedAttack : AttackPattern
    {
        [Header("Attack Sequence")]
        [Tooltip("The series of attacks to perform in order. The last element is the potential finisher.")]
        [SerializeField] private List<AttackStep> attackSequence;

        [Header("Stochastic Finisher")]
        [Tooltip("The probability (0 to 1) that the final attack in the sequence will be executed.")]
        [SerializeField, Range(0f, 1f)] private float finalAttackChance = 0.75f;

        [Header("Enrage")]
        [Tooltip("Overall speed multiplier for animation speeds and break times when enraged.")]
        [SerializeField] private float enrageRateMultiplier = 1.25f;
        [Tooltip("The probability (0 to 1) of executing the final attack when enraged.")]
        [SerializeField, Range(0f, 1f)] private float enragedFinalAttackChance = 1.0f;

        private bool _enraged = false;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null || attackSequence.Count == 0)
            {
                Debug.LogWarning("SequencedAttack is missing its controller or has an empty sequence.");
                yield break;
            }

            _enraged = controller.IsEnraged;
            // Set current parameters based on enraged status
            float currentRate = _enraged ? enrageRateMultiplier : 1f;
            float currentFinalChance = _enraged ? enragedFinalAttackChance : finalAttackChance;

            // --- 2. Execute the Attack Sequence
            for (int i = 0; i < attackSequence.Count; i++)
            {
                // If this is the last step, check the stochastic condition
                if (i == attackSequence.Count - 1)
                {
                    if (Random.value > currentFinalChance)
                    {
                        break; // Roll failed, so we break out of the loop and end the combo early
                    }
                }

                // Perform the current attack step
                yield return DoOneStep(controller, attackSequence[i], currentRate);
            }

            // --- 3. Cleanup 
            if (controller.Animator != null)
            {
                controller.Animator.Play("Idle");
                controller.Animator.speed = 1f;
            }
        }

        /// <summary>
        /// Executes a single step of the attack sequence.
        /// </summary>
        private IEnumerator DoOneStep(BossController controller, AttackStep step, float rateMultiplier)
        {
            // --- Active Phase
            if (controller.Animator != null && !string.IsNullOrEmpty(step.animationName))
            {
                controller.Animator.Play(step.animationName);
                controller.Animator.speed = step.animationSpeedScale * rateMultiplier;
            }
            yield return new WaitForSeconds(step.activeDuration / rateMultiplier);

            // --- Recovery/Break Phase
            // Transition to Idle to prevent the last attack frame from sticking during the pause.
            if (controller.Animator != null)
            {
                controller.Animator.Play("Idle");
                controller.Animator.speed = 1f;
            }
            yield return new WaitForSeconds(step.breakTimeAfter / rateMultiplier);
        }
    }
    [System.Serializable]
    public class AttackStep
    {
        [Tooltip("The name of the animation state to play for this step.")]
        public string animationName = "Attack1";

        [Tooltip("The speed multiplier for this specific animation clip.")]
        public float animationSpeedScale = 1f;

        [Tooltip("The duration of the active attack phase (e.g., when hitboxes are on).")]
        public float activeDuration = 0.5f;

        [Tooltip("The recovery or pause time after this step before the next one begins.")]
        public float breakTimeAfter = 0.2f;
    }
}