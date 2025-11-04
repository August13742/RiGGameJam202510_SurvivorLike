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
            _health.ResetFull();
        }

        protected virtual void OnEnable()
        {
            if (_health != null)
            {
                _health.Died += OnDied;
                _health.Damaged += OnDamaged;
                
            }
        }

        protected virtual void OnDisable()
        {
            if (_health != null)
            {
                if (_health != null) _health.Died -= OnDied;
                if (_health != null) _health.Damaged -= OnDamaged;
            }
        }
        protected virtual void OnDamaged(float amt, Vector3 pos, bool crit)
        {
            if (crit) DamageTextManager.Instance.ShowCrit(pos, amt);
            else DamageTextManager.Instance.ShowNormal(pos, amt);

            SessionManager.Instance.IncrementDamageDealt(amt);
        }
        private void OnTriggerEnter2D(Collider2D col)
        {
            // Layer mask filter
            if ((hitMask.value & (1 << col.gameObject.layer)) == 0) return;

            if (!col.TryGetComponent<HealthComponent>(out var target)) return;

            target.Damage(_def.ContactDamage);

        }

        // --- Public-facing movement APIs ---

        // Melee: always chase
        protected void Move()
        {
            if (!_target || IsDead) { Stop(); return; }

            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
            Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;

            Vector2 targetVelocity = dir * _def.MoveSpeed;
            ApplyMove(targetVelocity);
        }

        // Ranged: hold a distance band; canShoot is true only when in band
        protected void Move(out bool canShoot, float preferredDistance, float unsafeDistance)
        {
            canShoot = false;

            if (!_target || IsDead) { Stop(); return; }

            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
            float currentDistance = toTarget.magnitude;
            Vector2 dir = currentDistance > 0.0001f ? toTarget / currentDistance : Vector2.zero;

            Vector2 targetVelocity;

            if (currentDistance < unsafeDistance)         // too close → retreat
                targetVelocity = -dir * _def.MoveSpeed;
            else if (currentDistance > preferredDistance) // too far → approach
                targetVelocity = dir * _def.MoveSpeed;
            else                                          // in band → hold & shoot
            {
                targetVelocity = Vector2.zero;
                canShoot = true;
            }

            ApplyMove(targetVelocity);
        }

        // --- Internals ---

        private void ApplyMove(Vector2 targetVelocity)
        {
            float dt = Time.fixedDeltaTime;
            _velocity = Vector2.MoveTowards(_velocity, targetVelocity, _def.Acceleration * dt);
            _rb.MovePosition(_rb.position + _velocity * dt);
        }

        private void Stop()
        {
            _velocity = Vector2.zero;
        }

        // Spawner calls once per spawn (after Rent)
        public virtual void InitFrom(EnemyDef def)
        {
            _def = def;
            // Drive HP from def; also reset to full for a fresh spawn.
            _health.SetMaxHP(Mathf.Max(1, def.BaseHP), resetCurrent: true, raiseEvent: true);
        }

        public void Damage(float amount) => _health.Damage(amount);
        public void Heal(float amount) => _health.Heal(amount);
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
            _health.DisconnectAllSignals();

            Despawned?.Invoke(this);
            SessionManager.Instance.IncrementEnemyDowned(1);

            if (_stamp!=null && (_stamp.OwnerPool != null)) _stamp.OwnerPool.Return(gameObject);
            else gameObject.SetActive(false);
        }
    }
}   


