using System;
using AugustsUtility.Tween;
using Survivor.Game;
using UnityEngine;

namespace Survivor.Drop
{
    [DisallowMultipleComponent]
    public sealed class DropItem_FullHeal : DropItemBase
    {

        [SerializeField] private float healAmount = 9999f;

        [Header("Spawn FX")]
        [SerializeField] private float spawnHeight = 6f;
        [SerializeField] private float fallDuration = 0.35f;
        [SerializeField, Min(0f)] private float groundJitterRadius = 0.5f;

        [Header("Impact FX")]
        [SerializeField] private float impactScaleMultiplier = 1.2f;
        [SerializeField] private float impactScaleDuration = 0.12f;

        private Vector3 _groundPos;
        private Vector3 _baseScale;

        public override void OnSpawned()
        {
            base.OnSpawned();

            _baseScale = transform.localScale;

            // slightly randomise around current position so multiple don't stack perfectly
            Vector2 jitter = (groundJitterRadius > 0f)
                ? UnityEngine.Random.insideUnitCircle * groundJitterRadius
                : Vector2.zero;

            _groundPos = transform.position + (Vector3)jitter;

            // disable pickup while falling
            if (_collider) _collider.enabled = false;

            // start above ground, then fall
            transform.position = _groundPos + Vector3.up * spawnHeight;

            

            transform.TweenPosition(
                _groundPos,
                fallDuration,
                EasingFunctions.EaseOutSmooth,
                onComplete: OnLanded);
        }

        private void OnLanded()
        {
            // enable pickup once we land
            if (_collider) _collider.enabled = true;

            // little squash/pop
            transform.localScale = _baseScale * impactScaleMultiplier;
            transform.TweenLocalScale(_baseScale, impactScaleDuration, null);
        }

        protected override void Apply(GameObject player)
        {
            var sm = SessionManager.Instance;
            if (sm != null)
            {
                sm.RestorePlayerHealth(healAmount);
            }
        }
    }
}
