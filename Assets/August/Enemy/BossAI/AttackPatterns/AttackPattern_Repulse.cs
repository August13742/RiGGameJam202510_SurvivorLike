using System.Collections;
using AugustsUtility.Telegraph;
using Survivor.Control;
using Survivor.Game;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New RepulsePattern",
        menuName = "Defs/Boss Attacks/Repulse")]
    public sealed class AttackPattern_Repulse : AttackPattern
    {
        [Header("Repulse Parameters")]
        [Tooltip("Radius within which the player is affected.")]
        [SerializeField] private float effectRadius = 10f;
        [SerializeField,Range(0,1f)] private float chanceToBeAttractInstead = 0.5f;

        [Tooltip("Maximum distance the player can be pushed in a single repulse.")]
        [SerializeField] private float maxTravel = 6f;

        [Tooltip("Delay between telegraph start and the actual repulse impulse.")]
        [SerializeField] private float delay = 0.5f;

        [Header("Telegraph")]
        [SerializeField] private Color telegraphColor = new(1f, 0.6f, 0.4f, 1f);

        [Header("Enrage Scaling")]
        [SerializeField] private bool onlyWhenEnraged = false;
        [SerializeField] private float enrageRadiusMul = 1.2f;
        [SerializeField] private float enrageTravelMul = 1.4f;
        [SerializeField] private float enrageRateMul = 1.2f;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null)
                yield break;

            bool enraged = controller.IsEnraged;
            if (onlyWhenEnraged && !enraged)
                yield break;

            float rateMul = enraged ? enrageRateMul : 1f;
            float invRate = 1f / Mathf.Max(rateMul, 0.01f);

            float radius = effectRadius * (enraged ? enrageRadiusMul : 1f);
            float travel = maxTravel * (enraged ? enrageTravelMul : 1f);
            float actualDelay = delay * invRate;

            Vector3 center = controller.transform.position;

            // Telegraph circle
            Telegraph.Circle(
                host: controller,
                pos: center,
                radius: radius,
                duration: actualDelay,
                color: telegraphColor
            );

            if (actualDelay > 0f)
                yield return new WaitForSeconds(actualDelay);

            // Do the repulse or attract
            var playerTf = controller.PlayerTransform;
            var pc = playerTf.GetComponent<PlayerController>();
            if (pc != null)
            {
                bool isAttract = Random.value < chanceToBeAttractInstead;
                RadialDisplacementUtility.ApplyRadialImpulse(
                    pc,
                    sourceWorldPos: center,
                    radius: radius,
                    maxTravel: travel,
                    pull: isAttract // push away if false, pull in if true
                );
            }

        }
    }
}