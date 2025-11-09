using Survivor.Game;
using UnityEngine;
using Survivor.UI;
namespace Survivor.Enemy
{
    [RequireComponent(typeof(PrefabStamp), typeof(HealthComponent), typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public abstract class EnemyBase : MonoBehaviour, IPoolable, IHitstoppable
    {
        protected EnemyDef _def;
        protected PrefabStamp _stamp;
        protected HealthComponent _health;
        protected Vector2 _velocity = Vector2.zero;
        protected Rigidbody2D _rb;
        protected Transform _target;
        [SerializeField] protected LayerMask hitMask;

        [SerializeField] private bool UpdateFacing = true;
        [SerializeField] private bool FaceByVelocity = true;
        [SerializeField] private float OverlapKeepFacingRadius = 0.6f; // don't update facing when this close
        [SerializeField] private float FlipDotThreshold = 0.35f;       // hysteresis via cooldown
        [SerializeField] private float MinFlipUpdateInterval = 0.25f;        // seconds between flips

        private int _facingSign = +1;     // +1 = right, -1 = left, assumes sprite default face right
        private float _flipTimer = 0f;

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
                _health.Died -= OnDied;
                _health.Damaged -= OnDamaged;
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
        bool IsFrozen = false;
        public void OnHitstopStart() { IsFrozen = true; }
        public void OnHitstopEnd() { IsFrozen = false; }


        protected void UpdateFacingPolicy(Vector2 toTarget, float distanceToTarget)
        {
            if (!UpdateFacing || IsFrozen) return;

            _flipTimer -= Time.fixedDeltaTime;
            if (_flipTimer < 0f) _flipTimer = 0f;

            // 1) When overlapping/very close, freeze facing to avoid jitter
            if (distanceToTarget <= OverlapKeepFacingRadius) return;

            // 2) Choose an aim direction: prefer velocity when moving; otherwise, use target direction.
            Vector2 dir =
                (FaceByVelocity && _velocity.sqrMagnitude > 0.0004f) // 2cm
                    ? _velocity.normalized
                    : (toTarget.sqrMagnitude > 0.000001f ? toTarget.normalized : new Vector2(_facingSign, 0f)); //1mm threshold

            float dot = dir.x; // since dir is normalized, dot(dir, +X) == dir.x

            if (_facingSign > 0)
            {
                if (dot < -FlipDotThreshold && _flipTimer <= 0f)
                    SetFacing(-1);
            }
            else
            {
                if (dot > +FlipDotThreshold && _flipTimer <= 0f)
                    SetFacing(+1);
            }
        }

        private void SetFacing(int sign)
        {
            if (_facingSign == sign) return;
            _facingSign = sign;

            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * sign;
            transform.localScale = s;

            _flipTimer = MinFlipUpdateInterval;
        }

        // --- Public-facing movement APIs ---

        // Melee: always chase
        protected void Move()
        {
            if (IsFrozen) return;
            if (!_target || IsDead) { Stop(); return; }

            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
            float currentDistance = toTarget.magnitude;
            Vector2 dir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector2.zero;

            Vector2 targetVelocity = dir * _def.MoveSpeed;
            ApplyMove(targetVelocity);

            UpdateFacingPolicy(toTarget, currentDistance);
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

            UpdateFacingPolicy(toTarget, currentDistance);
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
            _health.DisconnectAllSignals();
            Despawned = null;
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


