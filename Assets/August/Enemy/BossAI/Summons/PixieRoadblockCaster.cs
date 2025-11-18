using AugustsUtility.CameraShake;
using AugustsUtility.Telegraph;
using AugustsUtility.Tween;
using Survivor.Control;
using Survivor.Enemy.FSM;
using Survivor.Game;
using Survivor.UI;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class PixieRoadblockCaster : MonoBehaviour, IHitstoppable
    {
        [Header("Ownership")]
        [SerializeField] private BossController owner;
        [SerializeField] private Transform target;  // player

        [Header("Animation")]
        [SerializeField] private string castAnim = "Attack1";
        [SerializeField] private string idleAnim = "Idle";
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer _sr;

        [Header("Facing")]
        [SerializeField] private bool useFacing = true;
        [SerializeField] private float facingDeadzone = 0.1f;     // min |dx| to consider flipping
        [SerializeField] private float minFlipInterval = 0.25f;   // seconds between flips

        public BossController Owner => owner;

        // --- Config-driven fields (assigned in Init) ---
        private float followSpeed;
        private float preferredDistance;
        private float bandHalfWidth;

        private float castMinDistance;
        private float castMaxDistance;

        private float initialDelay;
        private float castInterval;
        private float telegraphDuration;

        private float stepSize;
        private float blastRadius;

        private float inputPriority;
        private float maxAngleJitter;

        private float damage;
        private LayerMask hitMask;
        private bool showVFX;
        private GameObject vfxPrefab;
        private float cameraShakeStrength;
        private float cameraShakeDuration;
        private Color telegraphColor;

        private float lifeTime;

        private static readonly Collider2D[] Hits = new Collider2D[16];

        private bool _running;
        private bool _isCasting;
        private bool _isDying;
        private bool _isFrozen;      // hitstop flag

        private HealthComponent _hp;

        private int _facingSign = +1; // +1 = right, -1 = left
        private float _flipTimer = 0f;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (_sr == null)
                _sr = GetComponentInChildren<SpriteRenderer>();

            _hp = GetComponent<HealthComponent>();
            if (_hp != null)
            {
                _hp.Died += OnDied;
                _hp.Damaged += OnDamaged;
            }
        }
        void OnDamaged(float amt, Vector3 pos, bool crit)
        {
            if (crit) DamageTextManager.Instance.ShowCrit(pos, amt);
            else DamageTextManager.Instance.ShowNormal(pos, amt);

            SessionManager.Instance.IncrementDamageDealt(amt);
        }

        public void Init(BossController ownerController, Transform player, PixieConfig config)
        {
            owner = ownerController;
            target = player;

            // Apply config
            followSpeed = config.followSpeed;
            preferredDistance = config.preferredDistance;
            bandHalfWidth = config.bandHalfWidth;

            castMinDistance = config.castMinDistance;
            castMaxDistance = config.castMaxDistance;

            initialDelay = config.initialDelay;
            castInterval = config.castInterval;
            telegraphDuration = config.telegraphDuration;

            stepSize = config.stepSize;
            blastRadius = config.blastRadius;

            inputPriority = Mathf.Clamp01(config.inputPriority);
            maxAngleJitter = config.maxAngleJitter;

            damage = config.damage;
            hitMask = config.hitMask;
            showVFX = config.showVFX;
            vfxPrefab = config.vfxPrefab;
            cameraShakeStrength = config.cameraShakeStrength;
            cameraShakeDuration = config.cameraShakeDuration;

            telegraphColor = config.telegraphColor;
            lifeTime = config.lifeTime;

            if (_hp != null)
            {
                _hp.SetMaxHP(config.maxHP, resetCurrent: true, raiseEvent: false);
            }

            // Start in idle
            if (animator != null && !string.IsNullOrEmpty(idleAnim))
            {
                animator.Play(idleAnim, 0, 0f);
                animator.speed = 1f;
            }

            _facingSign = +1;
            if (_sr != null) _sr.flipX = false;

            if (!_running)
            {
                _running = true;
                StartCoroutine(MainLoop());
            }
        }

        private void Update()
        {
            if (target == null || _isCasting || _isDying || _isFrozen) return;

            Vector2 pos = transform.position;
            Vector2 toPlayer = (Vector2)target.position - pos;
            float dist = toPlayer.magnitude;

            // Banding: keep around preferredDistance
            float inner = preferredDistance - bandHalfWidth;
            float outer = preferredDistance + bandHalfWidth;

            Vector2 dir = (dist > 0.0001f) ? (toPlayer / dist) : Vector2.zero;

            if (dist > outer)
            {
                // too far → move closer
                pos += dir * followSpeed * Time.deltaTime;
                transform.position = pos;
            }
            else if (dist < inner)
            {
                // too close → back off
                pos -= dir * followSpeed * Time.deltaTime;
                transform.position = pos;
            }

            UpdateFacing((target.position.x - transform.position.x));
        }

        private void UpdateFacing(float dxToPlayer)
        {
            if (!useFacing || _sr == null) return;

            if (_flipTimer > 0f)
            {
                _flipTimer -= Time.deltaTime;
                if (_flipTimer < 0f) _flipTimer = 0f;
            }

            if (Mathf.Abs(dxToPlayer) < facingDeadzone) return;

            int desiredSign = dxToPlayer > 0f ? +1 : -1;
            if (desiredSign == _facingSign) return;
            if (_flipTimer > 0f) return;

            _facingSign = desiredSign;
            _sr.flipX = (_facingSign < 0);

            _flipTimer = minFlipInterval;
        }

        private IEnumerator MainLoop()
        {
            float startTime = Time.time;
            if (initialDelay > 0f)
                yield return new WaitForSeconds(initialDelay);

            while (true)
            {
                if (lifeTime > 0f && Time.time - startTime >= lifeTime)
                    break;

                if (target == null || _isDying)
                    break;

                if (_isFrozen)
                {
                    // during hitstop, do nothing this frame
                    yield return null;
                    continue;
                }

                float dist = Vector2.Distance(transform.position, target.position);

                bool inCastBand = (dist >= castMinDistance && dist <= castMaxDistance);

                if (inCastBand)
                {
                    yield return CastOnce();
                }

                if (castInterval > 0f)
                    yield return new WaitForSeconds(castInterval);
                else
                    yield return null;
            }

            StartDeathFade();
        }

        private IEnumerator CastOnce()
        {
            if (target == null || _isDying) yield break;

            _isCasting = true;

            // Switch to cast animation
            if (animator != null && !string.IsNullOrEmpty(castAnim))
            {
                animator.speed = 1f;
                animator.Play(castAnim, 0, 0f);
            }

            Vector2 playerPos = target.position;
            Vector2 pixiePos = transform.position;

            // Input direction, if any
            Vector2 inputDir = Vector2.zero;
            PlayerController pc = target.GetComponent<PlayerController>();
            if (pc != null)
            {
                inputDir = pc.InputDirection;
                if (inputDir.sqrMagnitude > 0.0001f)
                    inputDir.Normalize();
                else
                    inputDir = Vector2.zero;
            }

            // Base dir: blend between input and "player -> pixie" as fallback.
            Vector2 fallbackDir = pixiePos - playerPos;
            if (fallbackDir.sqrMagnitude < 0.0001f)
                fallbackDir = Vector2.right;
            fallbackDir.Normalize();

            Vector2 baseDir;
            if (inputDir.sqrMagnitude > 0.0001f && inputPriority > 0f)
            {
                baseDir = Vector2.Lerp(fallbackDir, inputDir, inputPriority).normalized;
            }
            else
            {
                baseDir = fallbackDir;
            }

            // Add angular jitter
            float jitter = (maxAngleJitter > 0f)
                ? Random.Range(-maxAngleJitter, maxAngleJitter)
                : 0f;

            Quaternion rot = Quaternion.Euler(0f, 0f, jitter);
            Vector2 finalDir = rot * baseDir;
            if (finalDir.sqrMagnitude < 0.0001f)
                finalDir = fallbackDir;

            finalDir.Normalize();

            // Roadblock center: fixed distance from player
            Vector2 startPos = playerPos;
            Vector2 endPos = playerPos + finalDir * stepSize;

            // Cast the telegraph + explosion along this small lerp
            yield return StartCoroutine(RiptideRoadblockRoutine(startPos, endPos));

            // Return to idle after cast
            if (animator != null && !string.IsNullOrEmpty(idleAnim) && !_isDying)
            {
                animator.Play(idleAnim, 0, 0f);
                animator.speed = 1f;
            }

            _isCasting = false;
        }

        private IEnumerator RiptideRoadblockRoutine(Vector2 startPos, Vector2 endPos)
        {
            Vector2 interpolatedPos = startPos;

            Tween.TweenValue(
                startPos, endPos, telegraphDuration,
                p => interpolatedPos = p,
                Lerp.Get<Vector2>(),
                EasingFunctions.EaseOutQuad
            );

            Telegraph.Circle(owner, () => interpolatedPos, blastRadius, telegraphDuration, telegraphColor);

            yield return new WaitForSeconds(telegraphDuration);

            if (_isDying) yield break;

            // Impact
            if (showVFX)
            {
                if (vfxPrefab != null)
                {
                    GameObject vfx = Object.Instantiate(vfxPrefab);
                    vfx.transform.position = interpolatedPos;
                }
                else
                {
                    VFX.VFXManager.Instance.ShowHitEffect(interpolatedPos);
                }
            }

            // Damage + shake
            ContactFilter2D filter = new()
            {
                useTriggers = true,
                useDepth = false
            };
            filter.SetLayerMask(hitMask);

            int hitCount = Physics2D.OverlapCircle(interpolatedPos, blastRadius, filter, Hits);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = Hits[i];
                if (col == null) continue;

                if (col.TryGetComponent<HealthComponent>(out var hp) && !hp.IsDead)
                {
                    hp.Damage(damage);
                    CameraShake2D.Shake(cameraShakeDuration, cameraShakeStrength);
                }
            }
        }

        private void StartDeathFade()
        {
            if (_isDying) return;
            _isDying = true;
            _running = false;
            _isCasting = false;
            _isFrozen = false;

            StopAllCoroutines();

            if (_sr != null)
            {
                _sr.TweenColorAlpha(0f, 0.4f, onComplete: () => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDied()
        {
            StartDeathFade();
        }

        private void OnDestroy()
        {
            if (_hp != null)
            {
                _hp.Died -= OnDied;
                _hp.Damaged -= OnDamaged;
            }
        }

        // --- IHitstoppable ---
        public void OnHitstopStart() => _isFrozen = true;
        public void OnHitstopEnd() => _isFrozen = false;

        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.position, preferredDistance - bandHalfWidth);
                Gizmos.DrawWireSphere(target.position, preferredDistance + bandHalfWidth);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(target.position, castMinDistance);
                Gizmos.DrawWireSphere(target.position, castMaxDistance);
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, preferredDistance);
        }
    }
}
