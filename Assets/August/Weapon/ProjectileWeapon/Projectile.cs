using UnityEngine;
using Survivor.Game;
using Survivor.VFX;
using System.Collections.Generic;

namespace Survivor.Weapon
{
    [RequireComponent(typeof(PrefabStamp),typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour,IPoolable
    {
        public float Damage { get; private set; }
        public float Speed { get; private set; }
        public int Pierce { get; private set; }

        [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.Right;

        private PrefabStamp _stamp;
        private Vector2 _dir;
        [SerializeField] private float _lifeTime;
        [SerializeField] private bool _isAlive;
        private Vector3 _baseScale;

        private IHitEventSink _sink;
        private float _critChance = 0f;
        private float _critMul = 1f;
        private bool _critPerHit = true;


        private enum ForwardAxis { Right, Up }

        public void SetHitSink(IHitEventSink sink) { _sink = sink; }
        public void ConfigureCrit(float chance, float mul, bool perHit)
        {
            _critChance = Mathf.Clamp01(chance);
            _critMul = Mathf.Max(1f, mul);
            _critPerHit = perHit;
        }

        public void Fire(Vector2 p, Vector2 direction, float speed, float dmg, int pierce, float time,float size)
        {
            transform.position = p;
            _dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            Speed = speed;
            Damage = dmg;
            Pierce = pierce;
            _lifeTime = time;
            _isAlive = true;

            transform.localScale = transform.localScale = _baseScale * size;

            float ang = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(
                (forwardAxis == ForwardAxis.Right) ? ang : (ang - 90f),
                Vector3.forward
            );
        }
        void Awake()
        {
            _stamp = GetComponent<PrefabStamp>();
            _baseScale = transform.localScale;
            _isAlive = false;
        }

        private void FixedUpdate()
        {
            if (!_isAlive) return;
            _lifeTime -= Time.fixedDeltaTime;
            if (_lifeTime <= 0f) { Despawn();  return; }

            transform.position += (Vector3)(Speed * Time.fixedDeltaTime * _dir);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!_isAlive) return;
            HealthComponent target;
            target = col.GetComponent<HealthComponent>();
            if (target == null) target = col.GetComponentInParent<HealthComponent>();
            if (target == null) return;
            if (target.IsDead) return;

            float dealt = Damage;
            bool crit = false;

            if (_critPerHit)
            {
                crit = (Random.value < _critChance);
                if (crit) dealt = Mathf.Round(dealt * _critMul * 10f) / 10f;
            }

            target.Damage(dealt,crit);

            _sink?.OnHit(dealt, transform.position, crit);

            VFXManager.Instance?.ShowHitEffect(col.gameObject.transform.position, crit);
            if (target.IsDead) _sink?.OnKill(transform.position);

            if (Pierce > 0) { Pierce--; } else {Despawn(); return; }
        }

        private void Despawn()
        {
            if (!_isAlive) return;
            _isAlive = false;
            if (_stamp.OwnerPool != null) _stamp.OwnerPool.Return(gameObject);
            else gameObject.SetActive(false);
        }
        void IPoolable.OnDespawned()
        {
        }
        void IPoolable.OnSpawned()
        {
            _lifeTime = float.PositiveInfinity;
            Pierce = int.MaxValue;
            _isAlive = true;
        }
    }
}
