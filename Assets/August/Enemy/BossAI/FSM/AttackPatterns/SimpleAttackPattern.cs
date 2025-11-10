using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New Simple Attack", menuName = "Defs/Boss Attacks/Simple Animation Pattern")]
    public class SimpleAnimationPattern : AttackPattern
    {
        public string animationName;
        public float animationDuration = 1.0f;
        public List<string> CallbackTags = new();

        public override IEnumerator Execute(BossController controller)
        {
            // Tell the animator to play the clip
            controller.Animator.Play(animationName);

            // Wait for the duration of the animation before finishing
            yield return new WaitForSeconds(animationDuration);
        }
    }
}