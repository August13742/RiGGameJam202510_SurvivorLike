using System.Collections.Generic;
using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    
    [DisallowMultipleComponent]
    public sealed class RotatingOrbitOrb : MonoBehaviour, IPoolable
    {
        [SerializeField] private Collider2D _col;         // isTrigger = true
        [SerializeField] private Renderer _renderer;      // visual

        // Runtime
        private Transform _pivot;
        private float _radius;
        private float _angRad;            // current angle (radians)
        private float _angVelRad;         // rad/sec
        private float _tLeft;
        private bool _followOrigin;
        private int _damage;
        private Team _team;
        private ObjectPool _pool;
        private int _maxHitsPerTarget;

        // Optional per-life hit tracking
        private HashSet<HealthComponent> _hitSet;

        public void SetPool(ObjectPool p) => _pool = p;

        public void Arm(
            Transform pivot, float radius, float startAngleRad,
            float angVelRad, float lifetime,
            int damage, Team team, bool followOrigin,
            bool toggleVis, int maxHitsPerTarget)
        {
            _pivot = pivot;
            _radius = radius;
            _angRad = startAngleRad;
            _angVelRad = angVelRad;
            _tLeft = lifetime;
            _damage = damage;
            _team = team;
            _followOrigin = followOrigin;
            _maxHitsPerTarget = maxHitsPerTarget;

            if (_maxHitsPerTarget > 0)
                (_hitSet ??= new HashSet<HealthComponent>()).Clear();
            else
                _hitSet?.Clear();

            // Team layer selection
            gameObject.layer = (_team == Team.Player)
                ? LayerMask.NameToLayer("PlayerProjectile")
                : LayerMask.NameToLayer("EnemyProjectile");

            if (toggleVis)
            {
                if (_col) _col.enabled = true;
                if (_renderer) _renderer.enabled = true;
            }

            // Snap immediately to start position
            if (_pivot)
            {
                Vector2 offset = new(Mathf.Cos(_angRad) * _radius, Mathf.Sin(_angRad) * _radius);
                transform.position = (Vector2)_pivot.position + offset;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _tLeft -= dt;
            if (_tLeft <= 0f || !_pivot)
            {
                Despawn();
                return;
            }

            _angRad += _angVelRad * dt; // positive = CCW
            Vector2 center = _followOrigin && _pivot ? (Vector2)_pivot.position : (Vector2)transform.position - new Vector2(Mathf.Cos(_angRad) * _radius, Mathf.Sin(_angRad) * _radius);
            // compute new position from current center
            Vector2 newPos = (_followOrigin && _pivot)
                ? (Vector2)_pivot.position + new Vector2(Mathf.Cos(_angRad) * _radius, Mathf.Sin(_angRad) * _radius)
                : (Vector2)transform.position; // fallback if pivot missing (already handled above)
            transform.position = newPos;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other || !other.TryGetComponent<HealthComponent>(out var hp)) return;
            if (hp.IsDead) return;

            if (_maxHitsPerTarget > 0)
            {
                if (_hitSet.Contains(hp)) return;
                _hitSet.Add(hp);
                if (_hitSet.Count > _maxHitsPerTarget) return;
            }

            hp.Damage(_damage);
        }

        private void Despawn()
        {
            if (_pool != null) _pool.Return(gameObject);
            else gameObject.SetActive(false);
        }

        public void OnSpawned()
        {
            // no-op
        }

        public void OnDespawned()
        {
            // Vis off to avoid stray hits while pooled
            if (_col) _col.enabled = false;
            if (_renderer) _renderer.enabled = false;
            _pivot = null;
        }

    }
}