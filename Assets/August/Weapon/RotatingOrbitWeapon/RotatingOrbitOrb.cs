using System.Collections.Generic;
using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    [RequireComponent(typeof(PrefabStamp), typeof(Collider2D), typeof(Renderer))]
    [DisallowMultipleComponent]
    public sealed class RotatingOrbitOrb : MonoBehaviour, IPoolable
    {
        [SerializeField] private Collider2D _col;        // isTrigger = true
        [SerializeField] private Renderer _renderer;     // visuals root
        [SerializeField] private Transform _visualRoot;  // optional

        // Runtime
        private Transform _pivot;
        private Vector2 _center0;
        private float _radius;
        private float _startAngRad;
        private float _totalAngleRad;
        private float _lifetime;
        private float _t;
        private bool _followOrigin;
        private int _damage;
        private Team _team;
        private ObjectPool _pool;
        private int _maxHitsPerTarget;
        private HashSet<HealthComponent> _hitSet;
        private AnimationCurve _motionCurve;

        private IHitEventSink _sink;
        private float _critChance = 0f;
        private float _critMul = 1f;
        private bool _critPerHit = true;

        public void SetPool(ObjectPool p) => _pool = p;
        public void SetHitSink(IHitEventSink sink) => _sink = sink;
        public void ConfigureCrit(float chance, float mul, bool perHit)
        {
            _critChance = Mathf.Clamp01(chance);
            _critMul = Mathf.Max(1f, mul);
            _critPerHit = perHit;
        }

        public void Arm(
            Transform pivot,
            float radius,
            float startAngleRad,
            float totalAngleRad,
            float lifetime,
            int damage,
            Team team,
            bool followOrigin,
            bool toggleVis,
            int maxHitsPerTarget,
            float orbVisualScale = 1f,
            AnimationCurve motionCurve = null
        )
        {
            _pivot = pivot;
            _radius = Mathf.Max(0.001f, radius);
            _startAngRad = startAngleRad;
            _totalAngleRad = totalAngleRad;
            _lifetime = Mathf.Max(0.01f, lifetime);
            _t = 0f;
            _damage = damage;
            _team = team;
            _followOrigin = followOrigin;
            _maxHitsPerTarget = maxHitsPerTarget;
            _motionCurve = motionCurve;

            if (_maxHitsPerTarget > 0)
                (_hitSet ??= new HashSet<HealthComponent>()).Clear();
            else
                _hitSet?.Clear();

            gameObject.layer = (_team == Team.Player)
                ? LayerMask.NameToLayer("PlayerProjectile")
                : LayerMask.NameToLayer("EnemyProjectile");

            if (toggleVis)
            {
                if (_col) _col.enabled = true;
                if (_renderer) _renderer.enabled = true;
            }

            Transform s = _visualRoot ? _visualRoot : transform;
            s.localScale = Vector3.one * Mathf.Max(0.01f, orbVisualScale);

            _center0 = _pivot ? (Vector2)_pivot.position : (Vector2)transform.position;

            Vector2 pos = CurrentCenter() + Polar(_startAngRad) * _radius;
            transform.position = pos;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (_t >= _lifetime || (!_pivot && _followOrigin))
            {
                Despawn();
                return;
            }

            float u = Mathf.Clamp01(_t / _lifetime);
            float k = EvaluateMotion(u);
            float ang = _startAngRad + _totalAngleRad * k;

            Vector2 pos = CurrentCenter() + Polar(ang) * _radius;
            transform.position = pos;
        }

        private Vector2 CurrentCenter() => _followOrigin && _pivot ? (Vector2)_pivot.position : _center0;
        private static Vector2 Polar(float ang) => new(Mathf.Cos(ang), Mathf.Sin(ang));

        private float EvaluateMotion(float u01)
        {
            if (_motionCurve != null) return Mathf.Clamp01(_motionCurve.Evaluate(u01));
            return CurveUtility.EaseInOutQuint(u01);
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

            float dealt = _damage;
            bool crit = false;
            if (_critPerHit)
            {
                crit = (Random.value < _critChance);
                if (crit) dealt = Mathf.Round(dealt * _critMul * 10f) / 10f;
            }

            hp.Damage(dealt,crit);

            _sink?.OnHit(dealt, transform.position, crit);
            if (hp.IsDead) _sink?.OnKill(transform.position);
        }

        private void Despawn()
        {
            if (_pool != null) _pool.Return(gameObject);
            else gameObject.SetActive(false);
        }

        public void OnSpawned() { }
        public void OnDespawned()
        {
            if (_col) _col.enabled = false;
            if (_renderer) _renderer.enabled = false;
            _pivot = null;
        }
    }
}
