using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New SequencedAttackPattern", menuName = "Defs/Boss Attacks/Sequenced Attack Pattern")]
    public class AttackPattern_SequencedAttack : AttackPattern
    {
        [SerializeField] SFXResource dashSFX;
        [Header("Attack Sequence")]
        [Tooltip("The series of attacks to perform in order. The last element is the potential finisher.")]
        [SerializeField] private List<AttackStep> attackSequence;

        [Header("Stochastic Finisher")]
        [Tooltip("The probability (0 to 1) that the final attack in the sequence will be executed.")]
        [SerializeField, Range(0f, 1f)] private float finalAttackChance = 0.75f;

        [Header("Gap Closing Properties")]
        [Tooltip("Animation to play while closing the distance to the player.")]
        [SerializeField] private string gapCloseAnimationName = "Walk";
        [Tooltip("Speed multiplier for the gap-closing animation.")]
        [SerializeField] private float gapCloseAnimationSpeed = 1f;
        [Tooltip("The movement speed of the boss when dashing towards the player.")]
        [SerializeField] private float dashSpeedTarget = 15f;

        [Header("Enrage")]
        [Tooltip("Overall speed multiplier for animation speeds, break times, and dash speed when enraged.")]
        [SerializeField] private float enrageRateMultiplier = 1.25f;
        [Tooltip("The probability (0 to 1) of executing the final attack when enraged.")]
        [SerializeField, Range(0f, 1f)] private float enragedFinalAttackChance = 1.0f;
        [SerializeField] private float enragedGapcloseRateMultiplier = 2f;
        private bool _enraged = false;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null || attackSequence.Count == 0)
            {
                Debug.LogWarning("SequencedAttack is missing its controller or has an empty sequence.");
                yield break;
            }

            _enraged = controller.IsEnraged;
            float currentRate = _enraged ? enrageRateMultiplier : 1f;
            float currentFinalChance = _enraged ? enragedFinalAttackChance : finalAttackChance;

            // --- Execute the Attack Sequence ---
            for (int i = 0; i < attackSequence.Count; i++)
            {
                AttackStep currentStep = attackSequence[i];

                // --- 1. Positioning Phase (Optional Gap-Close) ---
                // Check if this step requires being in a specific range and allows for repositioning.
                if (currentStep.requiresRangeCheck && currentStep.canGapClose)
                {
                    // If we are not in the desired band, attempt to close the gap.
                    if (controller.GetBandToPlayer() != currentStep.requiredBand)
                    {
                        // Probabilistic check to see if we should execute the gap-close.
                        if (Random.value <= (currentStep.gapCloseChance * (_enraged ? enragedGapcloseRateMultiplier : 1f)))
                        {
                            yield return DoGapClose(controller, currentStep.requiredBand, currentRate);
                        }
                    }
                }

                // --- 2. Attack Phase ---
                // If this is the last step, check the stochastic finisher condition.
                if (i == attackSequence.Count - 1)
                {
                    if (Random.value > currentFinalChance)
                    {
                        break; // Roll failed, so we break out of the loop and end the combo early.
                    }
                }

                // Perform the current attack step.
                yield return DoOneStep(controller, currentStep, currentRate);
            }

            // --- 3. Cleanup ---
            if (controller.Animator != null)
            {
                controller.Animator.Play("Idle");
                controller.Animator.speed = 1f;
            }
        }

        /// <summary>
        /// Executes a gap-closing maneuver to get into the desired range band.
        /// </summary>
        private IEnumerator DoGapClose(BossController controller, RangeBand targetBand, float rateMultiplier)
        {
            float currentDashSpeed = dashSpeedTarget * rateMultiplier;
            Transform bossTf = controller.transform;
            AudioManager.Instance?.PlaySFX(dashSFX, bossTf.position, bossTf);
            // Play gap-closing animation.
            if (controller.Animator != null && !string.IsNullOrEmpty(gapCloseAnimationName))
            {
                controller.Animator.Play(gapCloseAnimationName);
                controller.Animator.speed = gapCloseAnimationSpeed * rateMultiplier;
            }

            // Move towards the player until the target range is reached.
            while (controller.GetBandToPlayer() != targetBand && controller.PlayerTransform != null)
            {
                Vector2 direction = (controller.PlayerTransform.position - controller.transform.position).normalized;
                controller.VelocityOverride = direction * currentDashSpeed;
                yield return null; // Wait for the next frame.
            }

            // Stop movement once in range.
            controller.VelocityOverride = Vector2.zero;
        }

        /// <summary>
        /// Executes a single step of the attack sequence.
        /// </summary>
        private IEnumerator DoOneStep(BossController controller, AttackStep step, float rateMultiplier)
        {
            // --- Active Phase ---
            if (controller.Animator != null && !string.IsNullOrEmpty(step.animationName))
            {
                controller.Animator.Play(step.animationName);
                controller.Animator.speed = step.animationSpeedScale * rateMultiplier;
            }

            Transform bossTf = controller.transform;
            AudioManager.Instance?.PlaySFX(step.attackSFX, bossTf.position, bossTf);

            yield return new WaitForSeconds(step.activeDuration / rateMultiplier);

        }
    }

    [System.Serializable]
    public class AttackStep
    {
        [Header("Attack Properties")]
        [Tooltip("The name of the animation state to play for this step.")]
        public string animationName = "Attack1";

        [Tooltip("The speed multiplier for this specific animation clip.")]
        public float animationSpeedScale = 1f;

        public SFXResource attackSFX;

        [Tooltip("The duration of the active attack phase (e.g., when hitboxes are on).")]
        public float activeDuration = 0.5f;

        [Tooltip("The recovery or pause time after this step before the next one begins.")]
        public float breakTimeAfter = 0.2f;

        [Header("Positioning Logic")]
        [Tooltip("If true, the boss will check if it is in the 'Required Band' before executing this attack.")]
        public bool requiresRangeCheck = false;

        [Tooltip("The range band required to perform this attack. Only evaluated if 'Requires Range Check' is true.")]
        public RangeBand requiredBand = RangeBand.MeleeBand;

        [Tooltip("If true and the boss is outside the required band, it can attempt to dash into range.")]
        public bool canGapClose = false;

        [Tooltip("The probability (0 to 1) of performing a gap close if not in range. Only used if 'Can Gap Close' is true.")]
        [Range(0f, 1f)]
        public float gapCloseChance = 0.8f;
    }
}