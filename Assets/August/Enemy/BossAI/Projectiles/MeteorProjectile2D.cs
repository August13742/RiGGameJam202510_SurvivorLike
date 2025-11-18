using Survivor.Game;
using UnityEngine;

namespace Survivor.Weapon
{
    public sealed class MeteorProjectile2D : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private float speed = 18f;
        [SerializeField] private float maxLifetime = 5f;

        [Header("Impact")]
        [SerializeField] private float impactRadius = 1.5f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private GameObject impactVfxPrefab;

        private Vector2 _targetPos;
        private bool _active;
        private float _lifeLeft;

        private static readonly Collider2D[] _hits = new Collider2D[8];

        /// <summary>
        /// Launch a meteor from (impactPos + spawnOffset) towards impactPos.
        /// </summary>
        public void Launch(Vector2 impactPos, Vector2 spawnOffset, float spd, float dmg, float radius)
        {
            _targetPos = impactPos;
            transform.position = impactPos + spawnOffset;

            speed = spd;
            damage = dmg;
            impactRadius = radius;

            _lifeLeft = maxLifetime;
            _active = true;
        }

        private void FixedUpdate()
        {
            if (!_active) return;

            _lifeLeft -= Time.fixedDeltaTime;
            if (_lifeLeft <= 0f)
            {
                Explode();
                return;
            }

            Vector2 current = transform.position;
            Vector2 toTarget = _targetPos - current;
            float dist = toTarget.magnitude;

            if (dist <= speed * Time.fixedDeltaTime)
            {
                transform.position = _targetPos;
                Explode();
                return;
            }

            Vector2 dir = toTarget / dist;
            transform.position = current + dir * (speed * Time.fixedDeltaTime);
        }

        private void Explode()
        {
            if (!_active) return;
            _active = false;
            ContactFilter2D _filter = new () { useTriggers = true, useDepth = false };
            _filter.SetLayerMask(hitMask);
            int hitCount = Physics2D.OverlapCircle(_targetPos, impactRadius, _filter, _hits);

            for (int i = 0; i < hitCount; i++)
            {
                if (_hits[i] == null) continue;
                if (!_hits[i].TryGetComponent<HealthComponent>(out var hp)) continue;
                if (hp.IsDead) continue;

                hp.Damage(damage);
            }

            // VFX
            if (impactVfxPrefab != null)
            {
                Instantiate(impactVfxPrefab, _targetPos, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_targetPos, impactRadius);
        }
    }
}
