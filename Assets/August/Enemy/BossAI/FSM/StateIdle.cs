using UnityEngine;
using System;
namespace Survivor.Enemy.FSM
{
	public class StateIdle : IState
	{
		private readonly BossController _c;
		public StateIdle(BossController c) { _c = c; }

		public void Enter() 
		{
            _c.Animator.Play("Idle"); 
			HandleIfDead();
		}

		void HandleIfDead()
		{
            if (_c.IsDead) { _c.Animator.Play("Dead"); _c.Animator.speed = 0.7f; return; }
        }
		public Type Tick(float deltaTime)
		{
			HandleIfDead();

            float dist = Vector2.Distance(_c.transform.position, _c.PlayerTransform.position);
			var band = _c.GetBand(dist);

			// If we can play now, do it
			if (band != RangeBand.OffBand && !_c.IsGlobalAttackOnCooldown())
			{
				if (_c.TryBuildCandidatesForDistance(dist, out var _))
					return typeof(StateAttack);
			}

			// Off band?  chase
			if (band == RangeBand.OffBand) return typeof(StateChase);

			// Pocket and dry? keep chasing into melee (to unlock more plays)
			if (band == RangeBand.Pocket) return typeof(StateChase);

			// Melee and still dry �� wander
			Vector2 wander = new Vector2(_c.GetPerlinWanderX(), _c.GetPerlinWanderY()).normalized;
			_c.Velocity = wander * _c.Config.IdleWanderSpeed;
			return null;
		}

		public void Exit() { _c.Velocity = Vector2.zero; }
		public override String ToString()
		{
			return "Idle";
		}
	}
}