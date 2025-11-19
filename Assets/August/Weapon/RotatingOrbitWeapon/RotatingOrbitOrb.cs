using System.Collections.Generic;
using UnityEngine;
using Survivor.Game;
using AugustsUtility.Tween;


namespace Survivor.Weapon
{
    [RequireComponent(typeof(PrefabStamp))]
    [DisallowMultipleComponent]
    public sealed class RotatingOrbitOrb : MonoBehaviour, IPoolable
    {
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Transform _visualRoot;

        // Runtime
        private Transform _pivot;
        private Vector2 _center0;
        private float _radius;
        private float _startAngRad;
        private float _totalAngleRad;
        private float _lifetime;
        private bool _followOrigin;
        private int _damage;
        private Team _team;
        private int _maxHitsPerTarget;
        private HashSet<HealthComponent> _hitSet;

        private PrefabStamp _stamp;
        private IHitEventSink _sink;
        private float _critChance = 0f;
        private float _critMul = 1f;
        private bool _critPerHit = true;

        // Tween reference to manage its lifecycle
        private ITween _orbitTween;

        private void Awake()
        {
            _stamp = GetComponent<PrefabStamp>();
        }

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
            float orbVisualScale = 1f
        )
        {
            _orbitTween?.Kill();
            _pivot = pivot;
            _radius = Mathf.Max(0.001f, radius);
            _startAngRad = startAngleRad;
            _totalAngleRad = totalAngleRad;
            _lifetime = Mathf.Max(0.01f, lifetime);
            _damage = damage;
            _team = team;
            _followOrigin = followOrigin;
            _maxHitsPerTarget = maxHitsPerTarget;

            if (_maxHitsPerTarget > 0)
                (_hitSet ??= new HashSet<HealthComponent>()).Clear();
            else
                _hitSet?.Clear();

            gameObject.layer = (team == Team.Player)
                ? LayerMask.NameToLayer("PlayerProjectile")
                : LayerMask.NameToLayer("EnemyProjectile");

            if (toggleVis)
            {
                if (_collider) _collider.enabled = true;
                if (_renderer) _renderer.enabled = true;
            }

            Transform s = _visualRoot ? _visualRoot : transform;
            s.localScale = Vector3.one * Mathf.Max(0.01f, orbVisualScale);

            _center0 = _pivot ? (Vector2)_pivot.position : (Vector2)transform.position;


            _orbitTween = Tween.TweenValue(0f, 1f, _lifetime,
                onUpdate: (progress) =>
                {
                    // If the pivot is lost while following, despawn immediately.
                    if (!_pivot && _followOrigin)
                    {
                        Despawn();
                        return;
                    }

                    float ang = _startAngRad + _totalAngleRad * progress;
                    Vector2 pos = CurrentCenter() + Polar(ang) * _radius;
                    transform.position = pos;
                },
                lerp: Lerp.Get<float>(),
                ease: EasingFunctions.EaseInOutQuint,
                onComplete: Despawn
            );
        }


        private Vector2 CurrentCenter() => _followOrigin && _pivot ? (Vector2)_pivot.position : _center0;
        private static Vector2 Polar(float ang) => new(Mathf.Cos(ang), Mathf.Sin(ang));

        private void OnTriggerEnter2D(Collider2D col)
        {
            HealthComponent target;
            target = col.GetComponent<HealthComponent>();
            if (target == null) target = col.GetComponentInParent<HealthComponent>();
            if (target == null) return; 

            if (target.IsDead) return;

            if (_maxHitsPerTarget > 0)
            {
                if (_hitSet.Contains(target)) return;
                _hitSet.Add(target);
                if (_hitSet.Count > _maxHitsPerTarget) return;
            }

            float dealt = _damage;
            bool crit = false;
            if (_critPerHit)
            {
                crit = (Random.value < _critChance);
                if (crit) dealt = Mathf.Round(dealt * _critMul * 10f) / 10f;
            }

            target.Damage(dealt, crit);
            VFX.VFXManager.Instance?.ShowHitEffect(col.gameObject.transform.position, crit);

            _sink?.OnHit(dealt, transform.position, crit);
            if (target.IsDead) _sink?.OnKill(transform.position);
        }

        private void Despawn()
        {
            if (gameObject == null) return;
            _orbitTween?.Kill();
            _orbitTween = null;

            if (_stamp.OwnerPool != null)
                _stamp.OwnerPool.Return(gameObject);
            else
                gameObject.SetActive(false);
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            _orbitTween?.Kill();
            _orbitTween = null;

            if (_collider) _collider.enabled = false;
            if (_renderer) _renderer.enabled = false;
            _pivot = null;
        }
        private void OnDestroy()
        {
            // Scene unload or manual Destroy
            if (_orbitTween != null)
            {
                _orbitTween.Kill();
                _orbitTween = null;
            }
        }
    }
}