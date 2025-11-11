using UnityEngine;
using System;
namespace Survivor.Enemy.FSM
{
	public class StateChase : IState
	{
		private readonly BossController _controller;
		public StateChase(BossController context) { _controller = context; }

		public void Enter() { _controller.Animator.Play("Walk"); }

		public Type Tick(float deltaTime)
		{
			float dist = Vector2.Distance(_controller.transform.position, _controller.PlayerTransform.position);
			var band = _controller.GetBand(dist);

			// Attack if in-band + candidates + GlobalCD ready
			if (band != RangeBand.OffBand && !_controller.IsGlobalAttackOnCooldown())
			{
                if (_controller.TryBuildCandidatesForDistance(dist, out _))
					return typeof(StateAttack);
			}

			// Movement policy
			if (band == RangeBand.OffBand || band == RangeBand.Pocket) // Off band OR Pocket but no ranged play -> nudge toward melee
            {
				Vector2 dir = ((Vector2)(_controller.PlayerTransform.position - _controller.transform.position)).normalized;
				_controller.Velocity = dir * _controller.Config.ChaseSpeed;
				return null;
			}
			else // Melee band but dry -> wander instead of face-tanking nothing
			{
				return typeof(StateIdle);
			}
		}

		public void Exit() { _controller.Velocity = Vector2.zero; }

		public override String ToString()
		{
			return "Chase";
		}
	}
}