using System.Collections;
using AugustsUtility.Tween;
using Survivor.Game;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New RingSustainPulsePattern",
        menuName = "Defs/Boss Attacks/Ring Sustain Pulse (FireAndForget)")]
    public sealed class AttackPattern_RingSustainPulse : AttackPattern
    {
        [Header("Normal Pulse Settings")]
        [Tooltip("Radius level to pulse to (see RotatingRingHazard.radiusLevels; 0 is usually 'base').")]
        [SerializeField] private int targetRadiusLevel = 1;

        [Tooltip("Duration to expand from current scale to targetRadiusLevel.")]
        [SerializeField] private float expandDuration = 0.3f;

        [Tooltip("Time to remain at targetRadiusLevel before shrinking back to base.")]
        [SerializeField] private float sustainDuration = 2.0f;

        [Tooltip("Duration to shrink back to base level (0).")]
        [SerializeField] private float returnDuration = 0.4f;

        [Header("Easing")]
        [SerializeField] private bool useEaseInOut = true;

        [Header("Enrage Overrides")]
        [SerializeField] private bool useEnragedOverrides = true;

        [Tooltip("Radius level when enraged; if < 0, reuse normal targetRadiusLevel.")]
        [SerializeField] private int enragedTargetRadiusLevel = 0;

        [SerializeField] private float enragedExpandDuration = 0.25f;
        [SerializeField] private float enragedSustainDuration = 3.0f;
        [SerializeField] private float enragedReturnDuration = 0.35f;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null)
            {
                yield break;
            }

            RotatingRingHazard ring = FindRing(controller);
            if (ring == null)
            {
                // No ring, nothing to do.
                yield break;
            }

            bool enraged = controller.IsEnraged && useEnragedOverrides;

            int level = enraged && enragedTargetRadiusLevel >= 0
                ? enragedTargetRadiusLevel
                : targetRadiusLevel;

            float expand = enraged ? enragedExpandDuration : expandDuration;
            float sustain = enraged ? enragedSustainDuration : sustainDuration;
            float ret = enraged ? enragedReturnDuration : returnDuration;

            System.Func<float, float> easeShrink;
            System.Func<float, float> easeExpand;

            if (useEaseInOut)
            {
                // expand = ease-out, return = ease-in, or vice versa; pick taste.
                easeShrink = EasingFunctions.EaseInQuad;
                easeExpand = EasingFunctions.EaseOutQuad;
            }
            else
            {
                easeShrink = null;
                easeExpand = null;
            }

            // Fire-and-forget: ring handles expand -> sustain -> return asynchronously.
            ring.PlayPulseToLevelAndBack(
                level,
                expand,
                sustain,
                ret,
                easeShrink,
                easeExpand);

            // Immediately end this pattern; boss can move on to next action.
            yield break;
        }

        private RotatingRingHazard FindRing(BossController controller)
        {
            RotatingRingHazard ring = null;

            if (controller.Visuals != null)
            {
                ring = controller.Visuals.GetComponentInChildren<RotatingRingHazard>();
            }

            if (ring == null)
            {
                ring = controller.GetComponentInChildren<RotatingRingHazard>();
            }

            return ring;
        }
    }
}
