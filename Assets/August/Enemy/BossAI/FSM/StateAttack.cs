using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Survivor.Enemy.FSM
{

	public class StateAttack : IState
	{
		private readonly BossController _controller;
		private bool _running;

		public StateAttack(BossController context) { _controller = context; }

		public void Enter()
		{
			_running = true;
			_controller.Velocity = Vector2.zero;

			float d = Vector2.Distance(_controller.transform.position, _controller.PlayerTransform.position);
			if (!_controller.TryBuildCandidatesForDistance(d, out var candidates) || _controller.IsGlobalAttackOnCooldown())
			{
				_running = false; // nothing to do, bail
				return;
			}

			var chosen = _controller.ChooseWeighted(candidates);
			_controller.StartCoroutine(AttackRoutine(chosen));
		}

		private IEnumerator AttackRoutine(ScriptableAttackDefinition attackDef)
		{
			_controller.StartAttackTagCooldown(attackDef.CooldownTag, attackDef.Cooldown);
			yield return attackDef.Pattern.Execute(_controller);
			_running = false;
		}

		public Type Tick(float deltaTime) => _running ? null : typeof(StateChase);

		public void Exit() { _controller.StartGlobalAttackCooldown(); }
		public override String ToString()
		{
			return "Attack";
		}
	}
}