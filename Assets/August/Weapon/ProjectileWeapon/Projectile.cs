using UnityEngine;
using Survivor.Game;
using Survivor.VFX;

namespace Survivor.Weapon
{
    [RequireComponent(typeof(PrefabStamp))]
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        public float Damage { get; private set; }
        public float Speed { get; private set; }
        public int Pierce { get; private set; }

        [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.Right;

        [Header("Reselect Target On Hit")]
        [Tooltip("If true, after each hit (while pierce remains), the projectile will pick a new target and re-aim.")]
        [SerializeField] private bool reselectTargetOnHit = false;
        [SerializeField] private float reselectSearchRadius = 8f;
        [SerializeField] private LayerMask reselectSearchMask;

        [Header("Damage Decay On Reselect (Multiplicative)")]
        [Tooltip("Damage scale applied each time the projectile successfully reselects a new target. 1 = no change, 0.8 = -20% per hop.")]
        [SerializeField, Range(0f, 2f)]
        private float reselectDamageScalePerHit = 0.85f;

        [Tooltip("Minimum damage as a fraction of the initial damage. 0.2 = clamp to 20% of original.")]
        [SerializeField, Range(0f, 1f)]
        private float reselectMinDamageFraction = 0.2f;

        private PrefabStamp _stamp;
        private Vector2 _dir;
        [SerializeField] private float _lifeTime;
        [SerializeField] private bool _isAlive;
        private Vector3 _baseScale;

        private IHitEventSink _sink;
        private float _critChance = 0f;
        private float _critMul = 1f;
        private bool _critPerHit = true;

        private float _initialDamage;

        private enum ForwardAxis { Right, Up }

        public void SetHitSink(IHitEventSink sink) { _sink = sink; }

        public void ConfigureCrit(float chance, float mul, bool perHit)
        {
            _critChance = Mathf.Clamp01(chance);
            _critMul = Mathf.Max(1f, mul);
            _critPerHit = perHit;
        }

        public void Fire(
            Vector2 p,
            Vector2 direction,
            float speed,
            float dmg,
            int pierce,
            float time,
            float size)
        {
            transform.position = p;
            _dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            Speed = speed;
            Damage = dmg;
            Pierce = pierce;
            _lifeTime = time;
            _isAlive = true;

            _initialDamage = dmg;

            transform.localScale = _baseScale * size;

            float ang = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(
                (forwardAxis == ForwardAxis.Right) ? ang : (ang - 90f),
                Vector3.forward
            );
        }

        private void Awake()
        {
            _stamp = GetComponent<PrefabStamp>();
            _baseScale = transform.localScale;
            _isAlive = false;
        }

        private void FixedUpdate()
        {
            if (!_isAlive) return;

            _lifeTime -= Time.fixedDeltaTime;
            if (_lifeTime <= 0f)
            {
                Despawn();
                return;
            }

            transform.position += (Vector3)(Speed * Time.fixedDeltaTime * _dir);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!_isAlive) return;

            HealthComponent target = col.GetComponent<HealthComponent>();
            if (target == null) target = col.GetComponentInParent<HealthComponent>();
            if (target == null) return;
            if (target.IsDead) return;

            float dealt = Damage;
            bool crit = false;

            if (_critPerHit)
            {
                crit = (Random.value < _critChance);
                if (crit)
                {
                    dealt = Mathf.Round(dealt * _critMul * 10f) / 10f;
                }
            }

            target.Damage(dealt, transform.position, crit);
            _sink?.OnHit(dealt, transform.position, crit);

            VFXManager.Instance?.ShowHitEffect(col.transform.position, crit);

            if (target.IsDead)
            {
                _sink?.OnKill(transform.position);
            }

            // Consume pierce / decide despawn
            if (Pierce > 0)
            {
                Pierce--;

                if (reselectTargetOnHit)
                {
                    TryReselectTarget(target);
                }
            }
            else
            {
                Despawn();
            }
        }

        private void TryReselectTarget(HealthComponent lastHitTarget)
        {
            Vector2 center = transform.position;

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                center,
                reselectSearchRadius,
                reselectSearchMask
            );

            if (hits == null || hits.Length == 0)
                return;

            HealthComponent best = null;
            float bestSqrDist = float.PositiveInfinity;

            foreach (var c in hits)
            {
                if (c == null) continue;

                HealthComponent hc = c.GetComponent<HealthComponent>();
                if (hc == null) hc = c.GetComponentInParent<HealthComponent>();
                if (hc == null) continue;
                if (hc.IsDead) continue;
                if (hc == lastHitTarget) continue;

                float sqr = ((Vector2)hc.transform.position - center).sqrMagnitude;
                if (sqr < bestSqrDist)
                {
                    bestSqrDist = sqr;
                    best = hc;
                }
            }

            if (best == null) return;

            // Re-aim
            Vector2 dir = (best.transform.position - transform.position);
            if (dir.sqrMagnitude < 1e-6f) return;

            _dir = dir.normalized;

            float ang = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(
                (forwardAxis == ForwardAxis.Right) ? ang : (ang - 90f),
                Vector3.forward
            );

            ApplyReselectDamageDecay();
        }

        private void ApplyReselectDamageDecay()
        {
            if (reselectDamageScalePerHit <= 0f)
                return; // 0 or negative means "broken config", effectively freeze or worse; just bail.

            Damage *= reselectDamageScalePerHit;

            float minDamage = _initialDamage * reselectMinDamageFraction;
            if (Damage < minDamage)
                Damage = minDamage;
        }

        private void Despawn()
        {
            if (!_isAlive) return;

            _isAlive = false;
            if (_stamp.OwnerPool != null)
            {
                _stamp.OwnerPool.Return(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
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
