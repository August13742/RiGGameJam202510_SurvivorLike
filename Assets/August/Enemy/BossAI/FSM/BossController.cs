using AugustsUtility.CameraShake;
using Survivor.Game;
using Survivor.UI;
using Survivor.Weapon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Survivor.Enemy.FSM
{
    public enum RangeBand { OffBand, Pocket, MeleeBand }
    [RequireComponent(typeof(Rigidbody2D), typeof(HealthComponent))]
    public class BossController : MonoBehaviour
    {
        [SerializeField] private BossConfig config;

        public Animator Animator { get; private set; }
        public SpriteRenderer SR { get; private set; }
        public HealthComponent HP { get; private set; }
        public Rigidbody2D RB { get; private set; }
        public Transform PlayerTransform { get; private set; }
        public BossConfig Config => config;
        public bool AlwaysEnraged = false;
        public bool IsEnraged = false; 
        
        

        [Header("Movement")]
        [Tooltip("The current velocity of the boss, calculated via acceleration and friction.")]
        [field: SerializeField] public Vector2 Velocity { get; set; }
        [Tooltip("A forced velocity for special moves like dashes, bypassing acceleration.")]
        [field: SerializeField] public Vector2 VelocityOverride { get; set; }
        [Tooltip("The intended direction of movement, set by the current state.")]
        [field: SerializeField] public Vector2 Direction { get; set; }
        [SerializeField] private float acceleration = 75f;
        [SerializeField] private float friction = 35f;


        [Header("Component Refs")]
        public GameObject Visuals;
        [Tooltip("Optional: Assign a child object where projectiles will spawn from.")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject meleeHitbox;
        public Transform FirePoint => firePoint;
        [SerializeField] EnemyProjectile2D projectilePrefab;
        [SerializeField] float projectileDamage = 5f;
        [SerializeField] float projectileSpeed = 10f;
        [SerializeField] bool projectileIsHoming = false;
        [SerializeField] bool projectileHomeWhenEnraged = false;
        [SerializeField] float projectileHomingDuration = 0.5f;
        [Header("Behaviour Pivot")]
        [Tooltip("Local-space offset used as the logical center for distance checks and range gizmos.")]
        [SerializeField] private Vector2 behaviorPivotLocal = Vector2.zero;

        public Vector2 BehaviorPivotWorld => (Vector2)transform.TransformPoint((Vector3)behaviorPivotLocal);

        public float DistanceToPlayer()
        {
            if (!PlayerTransform) return float.PositiveInfinity;
            return Vector2.Distance(BehaviorPivotWorld, (Vector2)PlayerTransform.position);
        }

        public RangeBand GetBandToPlayer() => GetBand(DistanceToPlayer());

        // --- State Machine ---
        private IState _currentState;
        [SerializeField] String _currentStateLabel = "";
        private Dictionary<Type, IState> _states;

        // --- Cooldowns ---
        private float _globalAttackCooldownTimer;
        private Dictionary<string, float> _attackTagCooldowns = new();

        // --- Facing Logic ---
        private int _facingSign = 1;
        private float _flipTimer;

        // --- Perlin Noise ---
        private float _perlinNoiseOffsetX, _perlinNoiseOffsetY;

        [field: SerializeField] public bool IsDead { get; private set; } = false;
        private bool _deathSequenceStarted = false;
        // Enrage special-case
        [SerializeField] private bool _enrageActionPending = false;
        void Awake()
        {
            HP = GetComponent<HealthComponent>();
            Animator = GetComponent<Animator>();
            if (Animator == null) Animator = GetComponentInChildren<Animator>();
            RB = GetComponent<Rigidbody2D>();
            RB.bodyType = RigidbodyType2D.Kinematic;
            _perlinNoiseOffsetX = Random.Range(0f, 1000f);
            _perlinNoiseOffsetY = Random.Range(0f, 1000f);
            if (Visuals.TryGetComponent<AnimationEventBus>(out var bus)) bus.Fired += OnAnimEvent;

            SR = Visuals.GetComponent<SpriteRenderer>();
            IsEnraged = false;
            _enrageActionPending = false;

            if (AlwaysEnraged) Enrage();
        }

        private void OnAnimEvent(AnimationEvent e)
        {
            switch (e.stringParameter)
            {
                case "hitbox_melee_on":  ToggleMeleeHitbox(true);break ;
                case "hitbox_melee_off": ToggleMeleeHitbox(false);break;
                case "projectile":
                    SpawnProjectile(firePoint == null ? (Vector2)transform.position : (Vector2)firePoint.position);
                    break;
                case "dead": Destroy(gameObject); break;

            }

        }
        private void ToggleMeleeHitbox(bool on)
        {
            meleeHitbox?.SetActive(on);
        }
        private void InitialiseStateFactory()
        {
            _states = new Dictionary<Type, IState>
            {
                { typeof(StateIdle), new StateIdle(this) },
                { typeof(StateChase), new StateChase(this) },
                { typeof(StateAttack), new StateAttack(this) }
            };
        }

        void Start()
        {
            HP.SetMaxHP(config.MaxHealth);
            HP.ResetFull();
            HP.Damaged += OnDamaged;
            HP.Died += OnDied;
            FindPlayerTransform();
            InitialiseStateFactory();
            ChangeState(typeof(StateIdle));
        }

        void OnDamaged(float amt, Vector3 pos, bool crit)
        {
            if (crit) DamageTextManager.Instance.ShowCrit(pos, amt);
            else DamageTextManager.Instance.ShowNormal(pos, amt);

            if (HP.GetCurrentPercent() < .5f) Enrage();
            SessionManager.Instance.IncrementDamageDealt(amt);
        }

        public void Enrage()
        {
            if (IsEnraged) return;

            IsEnraged = true;
            SR.color = config.EnragedColour;

            // Queue the enrage action to be forced once,
            // as soon as it's a valid candidate.
            _enrageActionPending = true;
        }
        void OnDied()
        {
            if (_deathSequenceStarted) return;
            _deathSequenceStarted = true;

            HP.DisconnectAllSignals();
            IsDead = true;

            // Stop all attack/state coroutines running on this controller
            StopAllCoroutines();

            // Stop movement
            Velocity = Vector2.zero;
            VelocityOverride = Vector2.zero;
            Direction = Vector2.zero;

            // Kill melee hitbox so it can't still damage the player while "dead"
            ToggleMeleeHitbox(false);

            // Optional: disable collider so it no longer interacts with anything
            if (TryGetComponent<Collider2D>(out var col))
                col.enabled = false;

            // Kill FSM
            _currentState = null;

            // Force death animation once
            Animator.Play("Dead", 0, 0f);
            Animator.speed = 0.7f;

            SessionManager.Instance.IncrementEnemyDowned(1);
        }

        private void SpawnProjectile(Vector2 origin)
        {
            var go = Instantiate(projectilePrefab, origin, Quaternion.identity);

            if (!go.TryGetComponent<EnemyProjectile2D>(out var proj))
            {
                Debug.LogWarning("ProjectilePrefab missing EnemyProjectile2D.");
                Destroy(go);
                return;
            }
            Transform tgt = PlayerTransform;
            Vector2 toTarget = ((Vector2)tgt.position - origin).normalized;

            bool doHome = projectileIsHoming || (projectileHomeWhenEnraged && IsEnraged);

            proj.Fire(
                origin,
                toTarget,
                projectileSpeed,
                projectileDamage,
                life: 4f,
                PlayerTransform,
                homingOverride: doHome,
                homingSecondsOverride:projectileHomingDuration
            );

        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!col.TryGetComponent<HealthComponent>(out var target)) return;
            if (target.IsDead) return;

            float dealt = config.MeleeDamage;
            target.Damage(dealt);
            if (target.CompareTag("Player")) CameraShake2D.Shake(0.2f, 1f);
        }


        void Update()
        {
            if (PlayerTransform == null) return;
            if (IsDead) return;

            TickCooldowns(Time.deltaTime);

            Type nextStateType = _currentState?.Tick(Time.deltaTime);
            if (nextStateType != null && nextStateType != _currentState.GetType())
            {
                ChangeState(nextStateType);
            }
        }

        void FixedUpdate()
        {
            if (IsDead) return;

            Vector2 finalVelocity;
            // Priority 1: Use VelocityOverride for dashes and special moves
            if (VelocityOverride.sqrMagnitude > 0.01f)
            {
                finalVelocity = VelocityOverride;
                Velocity = finalVelocity;
            }
            else // Priority 2: Use standard acceleration/friction model
            {
                Vector2 targetVelocity = Direction * config.ChaseSpeed;
                Velocity = Vector2.MoveTowards(Velocity, targetVelocity, acceleration * Time.fixedDeltaTime);


                if (Direction.sqrMagnitude < 0.01f)
                {
                    Velocity = Vector2.MoveTowards(Velocity, Vector2.zero, friction * Time.fixedDeltaTime);
                }
                finalVelocity = Velocity;
            }

            // Apply the final calculated velocity
            RB.MovePosition(RB.position + finalVelocity * Time.fixedDeltaTime);
            UpdateFacing();
        }

        public void ChangeState(Type newStateType)
        {
            _currentState?.Exit();
            _currentState = _states[newStateType];
            ResetParameters();
            _currentState.Enter();
            _currentStateLabel = _currentState.ToString();
        }

        private void ResetParameters()
        {
            VelocityOverride = Vector2.zero;
            Direction = Vector2.zero;
            Animator.speed = 1f;
        }

        public RangeBand GetBand(float dist)
        {
            if (dist > config.AttackRange) return RangeBand.OffBand;
            if (dist > config.RangedAttackMinRange) return RangeBand.Pocket;
            return RangeBand.MeleeBand;
        }

        public bool TryBuildCandidatesForDistance(float dist, out List<ScriptableAttackDefinition> result)
        {
            result = null;
            var band = GetBand(dist);
            if (band == RangeBand.OffBand) return false;

            var list = new List<ScriptableAttackDefinition>();

            if (band == RangeBand.Pocket)
            {
                foreach (var def in config.AttackPatterns)
                {
                    if (def.Category == AttackCategory.Ranged && !IsAttackTagOnCooldown(def.CooldownTag))
                        list.Add(def);
                }
            }
            else // MeleeBand
            {
                foreach (var def in config.AttackPatterns)
                {
                    if (def.Category == AttackCategory.Melee && !IsAttackTagOnCooldown(def.CooldownTag))
                        list.Add(def);
                }
                foreach (var def in config.AttackPatterns)
                {
                    if (def.Category == AttackCategory.Ranged && !IsAttackTagOnCooldown(def.CooldownTag))
                    {
                        list.Add(new ScriptableAttackDefinition
                        {
                            Category = def.Category,
                            Pattern = def.Pattern,
                            CooldownTag = def.CooldownTag,
                            Cooldown = def.Cooldown,
                            Weight = def.Weight * Mathf.Max(0f, config.RangedInMeleeWeightMultiplier)
                        });
                    }
                }
            }

            if (list.Count == 0) return false;
            result = list;
            return true;
        }

        public ScriptableAttackDefinition ChooseWeighted(IReadOnlyList<ScriptableAttackDefinition> attacks)
        {
            // 1. Enrage guarantee: if enrage action is pending and present in candidates,
            //    force it once and consume the pending flag.
            if (IsEnraged && _enrageActionPending && config.EnrageAction != null)
            {
                for (int i = 0; i < attacks.Count; i++)
                {
                    // Match via Pattern so it also works with cloned ScriptableAttackDefinition
                    if (attacks[i].Pattern == config.EnrageAction.Pattern)
                    {
                        _enrageActionPending = false;
                        return attacks[i];
                    }
                }
            }

            // 2. Normal weighted selection
            float total = 0f;
            for (int i = 0; i < attacks.Count; i++)
                total += Mathf.Max(0f, attacks[i].Weight);

            if (total <= 0f)
                return attacks[attacks.Count - 1];

            float r = Random.value * total;
            for (int i = 0; i < attacks.Count; i++)
            {
                float w = Mathf.Max(0f, attacks[i].Weight);
                if (r < w) return attacks[i];
                r -= w;
            }

            return attacks[attacks.Count - 1];
        }

        private void UpdateFacing()
        {
            if (_flipTimer > 0) _flipTimer -= Time.fixedDeltaTime;

            float horizontalVelocity = Velocity.x;
            if (Mathf.Abs(horizontalVelocity) > Config.FacingDeadzone)
            {
                SetFacing(Mathf.Sign(horizontalVelocity));
            }
            else if (Velocity.sqrMagnitude < 0.01f)
            {
                float directionToPlayer = PlayerTransform.position.x - transform.position.x;
                if (Mathf.Abs(directionToPlayer) > 0.1f)
                {
                    SetFacing(Mathf.Sign(directionToPlayer));
                }
            }
        }

        private void SetFacing(float sign)
        {
            int newSign = (int)sign;
            if (newSign == 0 || newSign == _facingSign || _flipTimer > 0) return;

            _facingSign = newSign;

            Vector3 s = Visuals.transform.localScale;
            s.x = Mathf.Abs(s.x) * _facingSign;
            Visuals.transform.localScale = s;

            _flipTimer = Config.MinFlipInterval;
        }

        #region Cooldown Management
        private void TickCooldowns(float deltaTime)
        {
            if (_globalAttackCooldownTimer > 0)
            {
                _globalAttackCooldownTimer -= deltaTime;
            }

            List<string> keys = new(_attackTagCooldowns.Keys);
            foreach (string key in keys)
            {
                _attackTagCooldowns[key] -= deltaTime;
                if (_attackTagCooldowns[key] <= 0)
                {
                    _attackTagCooldowns.Remove(key);
                }
            }
        }

        public bool IsGlobalAttackOnCooldown() => _globalAttackCooldownTimer > 0;
        public bool IsAttackTagOnCooldown(string tag) => _attackTagCooldowns.ContainsKey(tag);

        public void StartGlobalAttackCooldown()
        {
            _globalAttackCooldownTimer = Config.GlobalAttackCooldown;
        }

        public void StartAttackTagCooldown(string tag, float duration)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                _attackTagCooldowns[tag] = duration;
            }
        }
        #endregion

        #region Helpers & Gizmos
        public float GetPerlinWanderX() => (Mathf.PerlinNoise(Time.time * 0.2f + _perlinNoiseOffsetX, 0) * 2f) - 1f;
        public float GetPerlinWanderY() => (Mathf.PerlinNoise(0, Time.time * 0.2f + _perlinNoiseOffsetY) * 2f) - 1f;

        private void FindPlayerTransform()
        {
            GameObject playerObject = SessionManager.Instance.GetPlayerReference();
            if (playerObject != null) PlayerTransform = playerObject.transform;
            else
            {
                Debug.LogError("could not find GameObject with 'Player' tag.", this);
                enabled = false;
            }
        }
        private Quaternion GetOrientationToTarget(Vector2 direction, bool forwardAxisIsRight = true)
        {
            float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.AngleAxis(forwardAxisIsRight ? ang : (ang - 90f), Vector3.forward);
        }

        void OnDrawGizmosSelected()
        {
            if (config == null) return;

            Vector3 c = (Vector3)BehaviorPivotWorld;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(c, config.EngageRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(c, config.AttackRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(c, config.RangedAttackMinRange);
        }
        #endregion
    }
}