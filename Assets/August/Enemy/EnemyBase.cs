using Survivor.Game;
using UnityEngine;
using Survivor.UI;
namespace Survivor.Enemy
{
    [RequireComponent(typeof(PrefabStamp), typeof(HealthComponent), typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public abstract class EnemyBase : MonoBehaviour, IPoolable
    {
        protected EnemyDef _def;
        protected PrefabStamp _stamp;
        protected HealthComponent _health;
        protected Vector2 _velocity = Vector2.zero;
        protected Rigidbody2D _rb;
        protected Transform _target;
        [SerializeField] protected LayerMask hitMask;
        public bool IsDead => _health != null && _health.IsDead;
        public System.Action<EnemyBase> Despawned;

        protected virtual void Awake()
        {
            _stamp = GetComponent<PrefabStamp>();
            _health = GetComponent<HealthComponent>();
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        protected virtual void OnEnable()
        {
            if (_health != null) _health.Died += OnDied;
            if (_health != null) _health.Damaged += OnDamaged;
        }

        protected virtual void OnDisable()
        {
            if (_health != null) _health.Died -= OnDied;
            if (_health != null) _health.Damaged -= OnDamaged;
        }
        protected virtual void OnDamaged(int amt, Vector3 pos, bool crit)
        {
            if (crit) DamageTextManager.Instance.ShowCrit(pos, amt);
            else DamageTextManager.Instance.ShowNormal(pos, amt);
        }
        private void OnTriggerEnter2D(Collider2D col)
        {
            // Layer mask filter
            if ((hitMask.value & (1 << col.gameObject.layer)) == 0) return;

            if (!col.TryGetComponent<HealthComponent>(out var target)) return;

            target.Damage(_def.ContactDamage);

        }

        protected void Move()
        {
            if (!_target || IsDead) { _velocity = Vector2.zero; return; }
            Vector2 dir = ((Vector2)_target.position - (Vector2)transform.position).normalized;
            Vector2 targetVelocity = dir * _def.MoveSpeed;


            _velocity = Vector2.MoveTowards(_velocity, targetVelocity, _def.Acceleration * Time.fixedDeltaTime);
            _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
        }
        // Spawner calls once per spawn (after Rent)
        public virtual void InitFrom(EnemyDef def)
        {
            _def = def;
            // Drive HP from def; also reset to full for a fresh spawn.
            _health.SetMaxHP(Mathf.Max(1, def.BaseHP), resetCurrent: true, raiseEvent: true);
        }

        public void Damage(int amount) => _health.Damage(amount);
        public void Heal(int amount) => _health.Heal(amount);
        public float HealthPercent() => _health.GetCurrentPercent();

        // --- IPoolable ---
        public virtual void OnSpawned()
        {
            // Reset transient state. HP already reset by InitFrom or HealthComponent.
        }

        public virtual void OnDespawned()
        {
            // Clear vfx/sfx/trails
        }

        // Death pipeline -> return to owning pool
        protected virtual void OnDied()
        {
            Die();
        }

        protected void Die()
        {

            if(LootManager.Instance != null) LootManager.Instance.SpawnLoot(_def, transform.position);


            Despawned?.Invoke(this);
            if (_stamp!=null && (_stamp.OwnerPool != null)) _stamp.OwnerPool.Return(gameObject);
            else gameObject.SetActive(false);
        }
    }
}   


