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

        private void Awake()
        {
            input = new();
            rb = GetComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            motor = GetComponent<KinematicMotor2D>();
            statComponent = GetComponent<PlayerStatsComponent>();
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
        }

        #region Hitstop
        private bool IsFrozen = false;
        public void OnHitstopStart() { IsFrozen = true; }
        public void OnHitstopEnd() { IsFrozen = false; }
        #endregion
    }
}
