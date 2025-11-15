using System;
using UnityEngine;
using UnityEngine.InputSystem.XR;
namespace Survivor.Enemy.FSM
{
	public class StateIdle : IState
	{
		private readonly BossController _controller;
		public StateIdle(BossController c) { _controller = c; }

		public void Enter() 
		{
            _controller.Animator.Play("Idle"); 
		}

		
		public Type Tick(float deltaTime)
		{

            float dist = Vector2.Distance(_controller.transform.position, _controller.PlayerTransform.position);
            var band = _controller.CurrentBand;

            // If we can play now, do it
            if (band != RangeBand.OffBand && !_controller.IsGlobalAttackOnCooldown())
			{
				if (_controller.TryBuildCandidatesForDistance(dist, out var _))
					return typeof(StateAttack);
			}

			// Off band?  chase
			if (band == RangeBand.OffBand) return typeof(StateChase);

			// Pocket and dry? keep chasing into melee (to unlock more plays)
			if (band == RangeBand.Pocket) return typeof(StateChase);

			// Melee and still dry �� wander
			Vector2 wander = new Vector2(_controller.GetPerlinWanderX(), _controller.GetPerlinWanderY()).normalized;
			_controller.Velocity = wander * _controller.Config.IdleWanderSpeed;
			return null;
		}

		public void Exit() { _controller.Velocity = Vector2.zero; }
		public override String ToString()
		{
			return "Idle";
		}
	}
}