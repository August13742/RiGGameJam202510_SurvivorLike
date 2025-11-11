using Survivor.Game;
using Survivor.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Survivor.Enemy.FSM
{
    public enum RangeBand { OffBand, Pocket, MeleeBand }
    [RequireComponent(typeof(Animator),typeof(Rigidbody2D),typeof(HealthComponent))]
    public class BossController : MonoBehaviour
    {
        [SerializeField] private BossConfig config;

        public Animator Animator { get; private set; }

        public SpriteRenderer SR { get; private set; }
        public HealthComponent HP { get; private set; }
        public Rigidbody2D RB { get; private set; }
        public Transform PlayerTransform { get; private set; }
        public BossConfig Config => config;
        [field: SerializeField] public Vector2 Velocity { get; set; }
        [Header("Component Refs")]
        [Tooltip("Optional: Assign a child object where projectiles will spawn from.")]
        [SerializeField] private Transform firePoint;
        public Transform FirePoint => firePoint;

        // --- State Machine ---
        private IState _currentState;
        [SerializeField] String _currentStateLabel = ""; //inspector debug use only
        private Dictionary<Type, IState> _states;

        // --- Cooldowns ---
        private float _globalAttackCooldownTimer;
        private Dictionary<string, float> _attackTagCooldowns = new ();

        // --- Facing Logic ---
        private int _facingSign = 1;
        private float _flipTimer;

        // --- Perlin Noise ---
        private float _perlinNoiseOffsetX, _perlinNoiseOffsetY;

        bool isDead = false;
        void Awake()
        {
            HP = GetComponent<HealthComponent>();
            SR = GetComponent<SpriteRenderer>();
            Animator = GetComponent<Animator>();
            RB = GetComponent<Rigidbody2D>();
            RB.bodyType = RigidbodyType2D.Kinematic;
            _perlinNoiseOffsetX = Random.Range(0f, 1000f);
            _perlinNoiseOffsetY = Random.Range(0f, 1000f); 
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

            if (HP.GetCurrentPercent() < .5f) SR.color = config.EnragedColour;
            SessionManager.Instance.IncrementDamageDealt(amt);

        }

        void OnDied()
        {

            //if (LootManager.Instance != null) LootManager.Instance.SpawnLoot(_def, transform.position);
            HP.DisconnectAllSignals();
            isDead = true;

            SessionManager.Instance.IncrementEnemyDowned(1);

            StartCoroutine(Die());
            
        }
        IEnumerator Die()
        {
            Animator.Play("Dead");
            yield return new WaitForSeconds(3f);
            Destroy(gameObject);
        }


        void Update()
        {
            if (PlayerTransform == null || isDead) return;

            TickCooldowns(Time.deltaTime);

            Type nextStateType = _currentState?.Tick(Time.deltaTime);
            if (nextStateType != null && nextStateType != _currentState.GetType())
            {
                ChangeState(nextStateType);
            }
        }

        void FixedUpdate()
        {
            if (isDead) return;
            //transform.position += (Vector3)(Velocity * Time.fixedDeltaTime);
            RB.MovePosition(RB.position + Velocity * Time.fixedDeltaTime);
            UpdateFacing();
        }

        public void ChangeState(Type newStateType)
        {
            _currentState?.Exit();
            _currentState = _states[newStateType];
            _currentState.Enter();
            _currentStateLabel = _currentState.ToString();
        }
        public RangeBand GetBand(float dist)
        {
            if (dist > config.AttackRange) return RangeBand.OffBand;
            if (dist > config.RangedAttackMinRange) return RangeBand.Pocket;
            return RangeBand.MeleeBand;
        }

        /// <summary>
        /// Build attack candidates for the current distance band.
        /// - OffBand: no attacks (force chase)
        /// - Pocket:  ranged only
        /// - Melee:   melee + ranged(with weight penalty)
        /// Returns true if any candidate exists.
        /// </summary>
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
                // Primary: melee
                foreach (var def in config.AttackPatterns)
                {
                    if (def.Category == AttackCategory.Melee && !IsAttackTagOnCooldown(def.CooldownTag))
                        list.Add(def);
                }
                // Secondary: ranged with weight penalty
                foreach (var def in config.AttackPatterns)
                {
                    if (def.Category == AttackCategory.Ranged && !IsAttackTagOnCooldown(def.CooldownTag))
                    {
                        // clone a lightweight shadow entry with reduced weight
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
            float total = 0f;
            for (int i = 0; i < attacks.Count; i++) total += Mathf.Max(0f, attacks[i].Weight);
            if (total <= 0f) return attacks[attacks.Count - 1];

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
            else if (Velocity.sqrMagnitude < 0.01f) // When still, face player
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

            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * _facingSign;
            transform.localScale = s;

            _flipTimer = Config.MinFlipInterval;
        }

        #region Cooldown Management
        private void TickCooldowns(float deltaTime)
        {
            if (_globalAttackCooldownTimer > 0)
            {
                _globalAttackCooldownTimer -= deltaTime;
            }

            // C# doesn't allow modifying dictionary while iterating, so we copy keys
            List<string> keys = new (_attackTagCooldowns.Keys);
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

        void OnDrawGizmosSelected()
        {
            if (config == null) return;

            // Yellow: The range at which the boss wakes up and starts chasing.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, config.EngageRange);

            // Red: The maximum range for any attack. The boss tries to stay within this circle.
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, config.AttackRange);

            // Cyan: The "pocket" for ranged attacks. Inside this circle, the boss will switch to melee.
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, config.RangedAttackMinRange);
        }
        #endregion
    }
}