using Survivor.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Survivor.Control
{
    [RequireComponent(typeof(Rigidbody2D),typeof(KinematicMotor2D),typeof(PlayerStatsComponent))]
    public sealed class PlayerController : MonoBehaviour, IHitstoppable
    {
        [SerializeField] private float moveSpeed=> stats.MoveSpeed;
        [SerializeField] private float acceleration = 75f;
        [SerializeField] private float friction = 35f;
        private InputSystem_Actions input;
        [SerializeField] Vector2 velocity;
        [SerializeField] Vector2 inputDirection;
        public Vector2 InputDirection => inputDirection;
        public Vector2 CurrentVelocity => velocity;

        private Rigidbody2D rb;
        private KinematicMotor2D motor;
        private PlayerStatsComponent statComponent;
        private EffectivePlayerStats stats;

        private void Awake()
        {
            input = new();
            rb = GetComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            motor = GetComponent<KinematicMotor2D>();
            statComponent = GetComponent<PlayerStatsComponent>();
            stats = statComponent.EffectiveStats;
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
            if (IsFrozen) return;
            Vector2 targetVelocity = inputDirection.sqrMagnitude > 0f
                ? inputDirection.normalized * moveSpeed
                : Vector2.zero;

            float dv = (inputDirection.sqrMagnitude > 0f ? acceleration : friction) * Time.fixedDeltaTime;

            velocity = Vector2.MoveTowards(velocity, targetVelocity, dv);

            motor.Move(velocity * Time.fixedDeltaTime);
            //transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
            //rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }
        #region Hitstop
        bool IsFrozen = false;
        public void OnHitstopStart() { IsFrozen = true; }
        public void OnHitstopEnd() { IsFrozen = false; }
        #endregion
    }
}