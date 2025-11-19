using AugustsUtility.Telegraph;
using Survivor.Game;
using UnityEngine;

namespace Survivor.Weapon
{
    public class HazardZone2D : MonoBehaviour
    {
        [Header("Zone")]
        [SerializeField] private float radius = 2f;
        [SerializeField] protected LayerMask hitMask;

        [Header("Damage")]
        [SerializeField] private float damagePerSecond = 5f;

        [Header("Visuals (optional)")]
        [SerializeField] private GameObject destroyVfxPrefab;

        [Header("Animation (optional)")]
        [SerializeField] private Animator animator;
        [SerializeField] private string spawnStateName = "Spawn";
        [SerializeField] private string loopStateName = "Loop";
        [Tooltip("If true, local scale will be adjusted based on radius/baseRadius.")]
        [SerializeField] private bool scaleWithRadius = true;
        [Tooltip("Radius that corresponds to localScale = Vector3.one.")]
        [SerializeField] private float baseRadius = 1f;

        [Header("Telegraph (lifetime indicator)")]
        [Tooltip("If true, the hazard spawns its own telegraph matching its active lifetime.")]
        [SerializeField] private bool spawnTelegraphOnActivate = true;
        [SerializeField] private Color hazardTelegraphColor = new Color(1f, 0.3f, 1f, 0.8f);
        [Tooltip("Multiplier applied to radius to get telegraph radius.")]
        [SerializeField] private float telegraphRadiusMultiplier = 1f;

        [SerializeField] private float _timeLeft;
        [SerializeField] private float _lifetime;   // purely runtime, comes from Activate(...)
        private static readonly Collider2D[] _hits = new Collider2D[8];

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void Activate(Vector2 pos, float r, float dps, float life)
        {
            transform.position = pos;
            radius = r;
            damagePerSecond = dps;
            _lifetime = Mathf.Max(0.01f, life);
            _timeLeft = _lifetime;

            // Optional scale matching
            if (scaleWithRadius && baseRadius > 0f)
            {
                float scale = radius / baseRadius;
                transform.localScale = new Vector3(scale, scale, 1f);
            }

            // Animation starts only after meteor lands
            if (animator != null)
            {
                if (!string.IsNullOrEmpty(spawnStateName))
                {
                    animator.Play(spawnStateName, 0, 0f);
                }
                else if (!string.IsNullOrEmpty(loopStateName))
                {
                    animator.Play(loopStateName, 0, 0f);
                }
            }

            // Lifetime telegraph ring owned by the hazard itself
            if (spawnTelegraphOnActivate)
            {
                float telRadius = radius * Mathf.Max(0f, telegraphRadiusMultiplier);
                float telDuration = _lifetime; 

                Telegraph.Circle(
                    host: this,                  
                    pos: transform.position,
                    radius: telRadius,
                    duration: telDuration,
                    color: hazardTelegraphColor
                );
            }
        }

        protected virtual void FixedUpdate()
        {
            _timeLeft -= Time.fixedDeltaTime;
            if (_timeLeft <= 0f)
            {
                if (destroyVfxPrefab != null)
                    Instantiate(destroyVfxPrefab, transform.position, Quaternion.identity);
                Destroy(gameObject);
                return;
            }

            float dt = Time.fixedDeltaTime;
            ContactFilter2D _filter = new() { useTriggers = true, useDepth = false };
            _filter.SetLayerMask(hitMask);
            int hitCount = Physics2D.OverlapCircle(transform.position, radius, _filter, _hits);
            for (int i = 0; i < hitCount; i++)
            {
                if (_hits[i] == null) continue;
                if (!_hits[i].TryGetComponent<HealthComponent>(out var hp)) continue;
                if (hp.IsDead) continue;

                hp.Damage(damagePerSecond * dt);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
