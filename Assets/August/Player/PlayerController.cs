using UnityEngine;
using UnityEngine.InputSystem;
namespace Survivor.Control
{
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 75f;
        [SerializeField] private float friction = 35f;
        private InputSystem_Actions input;
        [SerializeField] Vector2 velocity;
        [SerializeField] Vector2 inputDirection;
        private Rigidbody2D rb;

        private void Awake()
        {
            input = new();
            rb = GetComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
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
            Vector2 targetVelocity = inputDirection.sqrMagnitude > 0f
                ? inputDirection.normalized * moveSpeed
                : Vector2.zero;

            float dv = (inputDirection.sqrMagnitude > 0f ? acceleration : friction) * Time.fixedDeltaTime;

            velocity = Vector2.MoveTowards(velocity, targetVelocity, dv);

            //transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        }
    }
}