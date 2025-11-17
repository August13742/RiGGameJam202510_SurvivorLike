using Survivor.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Survivor.Control
{
    [RequireComponent(typeof(Rigidbody2D), typeof(KinematicMotor2D), typeof(PlayerStatsComponent))]
    public sealed class PlayerController : MonoBehaviour, IHitstoppable
    {
        [SerializeField] private float acceleration = 75f;
        [SerializeField] private float friction = 35f;

        private InputSystem_Actions input;
        [SerializeField] private Vector2 velocity;
        [SerializeField] private Vector2 inputDirection;

        // --- External displacement (per-frame), e.g. boss pulls/knockbacks ---
        [SerializeField] private Vector2 externalDisplacement;   // debug-visible
        public void AddExternalDisplacement(Vector2 delta)
        {
            externalDisplacement += delta;
        }

        public Vector2 InputDirection => inputDirection;
        public Vector2 CurrentVelocity => velocity;

        private Rigidbody2D rb;
        private KinematicMotor2D motor;
        private PlayerStatsComponent statComponent;
        private EffectivePlayerStats Stats => statComponent.EffectiveStats;

        private float MoveSpeed => Stats.MoveSpeed;

        // ---------- Unstuck / emergency rescue ----------
        [Header("Unstuck Detection")]
        [SerializeField] private float stuckMoveThreshold = 0.01f;      // min distance to consider "actually moved"
        [SerializeField] private float stuckCheckInterval = 0.25f;      // seconds between position samples
        [SerializeField] private float stuckTimeBeforeUnstuck = 1.0f;   // how long of "trying but not moving" before rescue

        [Header("Unstuck Placement")]
        [SerializeField] private float unstuckProbeRadius = 1f;       // radius of overlap check
        [SerializeField] private float unstuckSearchRadius = 2.0f;      // how far around to search
        [SerializeField] private int unstuckRays = 8;                   // number of directions (8 = N/NE/E/... etc)
        [SerializeField] private LayerMask unstuckObstaclesMask;        // if zero, we’ll default to motor.collisionMask

        private Vector2 lastStuckCheckPosition;
        private float stuckCheckTimer;
        private float stuckAccumulatedTime;

        private void Awake()
        {
            input = new();
            rb = GetComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            motor = GetComponent<KinematicMotor2D>();
            statComponent = GetComponent<PlayerStatsComponent>();

            lastStuckCheckPosition = rb.position;

            // If not set in inspector, default to whatever the motor collides with.
            if (unstuckObstaclesMask == 0)
            {
                unstuckObstaclesMask = motor.collisionMask;
            }
        }

        private void OnEnable()
        {
            input.Player.Enable();
            input.Player.Move.performed += OnMovePerformed;
            input.Player.Move.canceled += OnMoveCanceled;
        }

        private void OnDisable()
        {
            input.Player.Move.performed -= OnMovePerformed;
            input.Player.Move.canceled -= OnMoveCanceled;
            input.Player.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            inputDirection = ctx.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            inputDirection = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (IsFrozen)
            {
                // Still apply external displacement
                if (externalDisplacement != Vector2.zero)
                    motor.Move(externalDisplacement);

                externalDisplacement = Vector2.zero;
                return;
            }

            Vector2 targetVelocity = inputDirection.sqrMagnitude > 0f
                ? inputDirection.normalized * MoveSpeed
                : Vector2.zero;

            float dv = (inputDirection.sqrMagnitude > 0f ? acceleration : friction) * Time.fixedDeltaTime;
            velocity = Vector2.MoveTowards(velocity, targetVelocity, dv);

            Vector2 delta = velocity * Time.fixedDeltaTime;

            // Inject external displacement
            delta += externalDisplacement;

            motor.Move(delta);
            externalDisplacement = Vector2.zero; // consumed this frame

            UpdateStuckDetection(Time.fixedDeltaTime);
        }

        // ---------- Stuck detection / rescue ----------

        private void UpdateStuckDetection(float dt)
        {
            // No intention to move -> do not consider it being "stuck"
            if (inputDirection.sqrMagnitude < 0.01f)
            {
                stuckAccumulatedTime = 0f;
                stuckCheckTimer = 0f;
                lastStuckCheckPosition = rb.position;
                return;
            }

            stuckCheckTimer += dt;
            if (stuckCheckTimer < stuckCheckInterval)
                return;

            stuckCheckTimer = 0f;

            Vector2 currentPos = rb.position;
            float dist = (currentPos - lastStuckCheckPosition).sqrMagnitude;

            if (dist < stuckMoveThreshold * stuckMoveThreshold)
            {
                stuckAccumulatedTime += stuckCheckInterval;

                if (stuckAccumulatedTime >= stuckTimeBeforeUnstuck)
                {
                    if (TryUnstuck(currentPos))
                    {
                        // reset timers after successful rescue
                        stuckAccumulatedTime = 0f;
                        stuckCheckTimer = 0f;
                        lastStuckCheckPosition = rb.position;
                    }
                }
            }
            else
            {
                // We did move -> reset
                stuckAccumulatedTime = 0f;
                lastStuckCheckPosition = currentPos;
            }
        }

        private bool TryUnstuck(Vector2 currentPos)
        {
            // Reset velocity – we don’t want to immediately slam back into the same wall.
            velocity = Vector2.zero;
            externalDisplacement = Vector2.zero;
            Debug.Log($"[PlayerController] Trying to Unstuck");
            // Check current position first – if for some reason Overlap says it's free, just reset timers.
            if (IsPositionFree(currentPos))
            {
                return true;
            }

            // Sample around in a circle
            int rays = Mathf.Max(4, unstuckRays);
            float maxR = Mathf.Max(unstuckSearchRadius, unstuckProbeRadius * 1.1f);

            for (int ring = 1; ring <= 3; ring++)
            {
                float r = maxR * (ring / 3f);
                for (int i = 0; i < rays; i++)
                {
                    float angle = (Mathf.PI * 2f * i) / rays;
                    Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 candidate = currentPos + dir * r;

                    if (IsPositionFree(candidate))
                    {
                        rb.position = candidate;
                        lastStuckCheckPosition = candidate;
                        Debug.Log($"[PlayerController] Unstuck to {candidate}");
                        return true;
                    }
                }
            }

            // No valid position found. We tried.
            Debug.LogWarning("[PlayerController] Tried to unstuck, but found no free position.");
            return false;
        }

        private bool IsPositionFree(Vector2 pos)
        {
            // Approximate the player collider with a circle.
            // swap to OverlapBox with collider extents for tighter fit
            Collider2D hit = Physics2D.OverlapCircle(pos, unstuckProbeRadius, unstuckObstaclesMask);
            return hit == null;
        }

        #region Hitstop
        private bool IsFrozen = false;
        public void OnHitstopStart() { IsFrozen = true; }
        public void OnHitstopEnd() { IsFrozen = false; }
        #endregion
    }
}
