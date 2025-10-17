using Survivor.Control;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class TopDownAnimDriver2D : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    [Header("Animator Parameter Names")]
    [SerializeField] private string pMoveX = "MoveX";
    [SerializeField] private string pMoveY = "MoveY";
    [SerializeField] private string pSpeed = "MoveSpeed";
    [SerializeField] private string pLastX = "LastMoveX";
    [SerializeField] private string pLastY = "LastMoveY";

    [Header("Damping (seconds)")]
    [SerializeField] private float damp = 0.08f;

    private Animator _anim;
    private Rigidbody2D _rb;
    private Vector2 _lastDir = Vector2.down;
    private Vector2 _prevPos;

    private int _hMoveX, _hMoveY, _hSpeed, _hLastX, _hLastY;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponentInParent<Rigidbody2D>();
        controller = GetComponentInParent<PlayerController>();

        _hMoveX = Animator.StringToHash(pMoveX);
        _hMoveY = Animator.StringToHash(pMoveY);
        _hSpeed = Animator.StringToHash(pSpeed);
        _hLastX = Animator.StringToHash(pLastX);
        _hLastY = Animator.StringToHash(pLastY);

        _prevPos = _rb.position; // seed
    }

    private void LateUpdate()
    {
        Vector2 pos = _rb.position; // same as (Vector2)transform.position for a non-rotating 2D body
        float dt = Mathf.Max(Time.deltaTime, 1e-6f);
        Vector2 v = (pos - _prevPos) / dt;   // frame-accurate velocity (works with MovePosition + Interpolate)
        _prevPos = pos;
        
        Vector2 dir = controller.InputDirection;
        float speed = dir.magnitude;

        if (dir.sqrMagnitude > 1e-6f)
            _lastDir = dir;

        _anim.SetFloat(_hSpeed, speed);
        _anim.SetFloat(_hMoveX, dir.x, damp, Time.deltaTime);
        _anim.SetFloat(_hMoveY, dir.y, damp, Time.deltaTime);
        _anim.SetFloat(_hLastX, _lastDir.x, damp, Time.deltaTime);
        _anim.SetFloat(_hLastY, _lastDir.y, damp, Time.deltaTime);
    }
}
