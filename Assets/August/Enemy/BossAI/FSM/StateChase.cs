using UnityEngine;
using System;

namespace Survivor.Enemy.FSM
{
    public class StateChase : IState
    {
        private readonly BossController _controller;
        public StateChase(BossController context) { _controller = context; }

        public void Enter()
        {
            _controller.Animator.Play("Walk");
        }
        public Type Tick(float deltaTime)
        {
            float dist = _controller.DistanceToPlayer();
            var band = _controller.CurrentBand;

            // Decision
            if (band != RangeBand.OffBand && !_controller.IsGlobalAttackOnCooldown())
            {
                if (_controller.TryBuildCandidatesForDistance(dist, out _))
                    return typeof(StateAttack);
            }

            // Movement Intention
            if (band == RangeBand.OffBand || band == RangeBand.Pocket) // Off-band OR in Pocket but have no ranged attacks -> move closer
            {
                // Set the intention to move towards the player
                Vector2 dirToPlayer = ((Vector2)(_controller.PlayerTransform.position - _controller.transform.position)).normalized;
                _controller.Direction = dirToPlayer;
                return null;
            }
            else // In Melee band but no attacks are ready -> stop chasing and do something else
            {
                return typeof(StateIdle);
            }
        }
        public void Exit()
        {
            // Clear the movement intention when leaving the chase state
            _controller.Direction = Vector2.zero;
        }

        public override String ToString()
        {
            return "Chase";
        }
    }
}