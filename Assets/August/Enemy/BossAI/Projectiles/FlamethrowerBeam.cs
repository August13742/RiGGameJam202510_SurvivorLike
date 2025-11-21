using System.Collections.Generic;
using Survivor.Game;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FlamethrowerBeam : MonoBehaviour
{
    [Header("Runtime wiring (assigned by pattern)")]
    [SerializeField] private Transform origin;
    [SerializeField] private Transform target;
    [SerializeField] private Transform root;

    [Header("Debug")]
    [SerializeField] private float debugCurrentWorldLength;

    // --- Scale / geometry ---
    private Vector3 _baseRootScale;
    private float _basePrefabLength;          // world units when scale.x == 1

    private float _currentLenScale = 0f;    // relative to _baseRootScale.x
    private float _targetLenScale = 1f;

    private float _currentThickScale = 0f;    // relative to _baseRootScale.y
    private float _targetThickScale = 1f;

    private float _lengthGrowSpeed;           // scale-units / s
    private float _thicknessGrowSpeed;

    private float _maxTurnRateDeg;
    private bool _configured;

    // --- DoT ---
    private float _damagePerSecond;
    private float _tickInterval;
    private LayerMask _targetMask;

    private readonly Dictionary<HealthComponent, float> _nextTickTime =
        new Dictionary<HealthComponent, float>();

    private void Awake()
    {
        _baseRootScale = root.localScale;
    }

    /// <summary>
    /// One-shot configuration called by the attack pattern.
    /// Length is in world units and independent of player distance.
    /// initialDirection is the starting direction for the beam (world space).
    /// </summary>
    public void Configure(
        Transform origin,
        Transform target,
        float basePrefabLength,
        float desiredWorldLength,
        float targetThicknessMul,
        float lengthGrowSpeed,
        float thicknessGrowSpeed,
        float maxTurnRateDeg,
        float damagePerSecond,
        float tickInterval,
        LayerMask targetMask,
        Vector2 initialDirection)
    {
        this.origin = origin;
        this.target = target;
        _basePrefabLength = Mathf.Max(0.0001f, basePrefabLength);

        // WorldLength = basePrefabLength * scaleX  →  scaleX = L / basePrefabLength
        float targetLenScale = desiredWorldLength / _basePrefabLength;

        _targetLenScale = Mathf.Max(0f, targetLenScale);
        _currentLenScale = 0f;    // start from zero-length puff
        _targetThickScale = Mathf.Max(0.0001f, targetThicknessMul);
        _currentThickScale = 0f;    // start thin
        _lengthGrowSpeed = Mathf.Max(0f, lengthGrowSpeed);
        _thicknessGrowSpeed = Mathf.Max(0f, thicknessGrowSpeed);
        _maxTurnRateDeg = Mathf.Max(0f, maxTurnRateDeg);

        _damagePerSecond = Mathf.Max(0f, damagePerSecond);
        _tickInterval = Mathf.Max(0f, tickInterval);
        _targetMask = targetMask;

        if (origin != null)
        {
            root.position = origin.position;

            // Initial orientation: use provided direction if valid, otherwise origin.right
            if (initialDirection.sqrMagnitude > 0.0001f)
            {
                root.right = initialDirection.normalized;
            }
            else
            {
                root.right = origin.right;
            }
        }

        _configured = true;
    }

    private void Update()
    {
        if (!_configured || origin == null)
            return;

        // 1) Anchor root at origin
        root.position = origin.position;

        // 2) Rotate toward target (homing)
        UpdateRotation();

        // 3) Grow length & thickness (flamethrower behaviour)
        UpdateScaling();

        // 4) Apply to ROOT ONLY – child visuals will inherit rotation/position, not scale
        root.localScale = new Vector3(
            _baseRootScale.x * _currentLenScale,
            _baseRootScale.y * _currentThickScale,
            _baseRootScale.z);

        debugCurrentWorldLength = _currentLenScale * _basePrefabLength;

        // 5) Apply DoT
        UpdateDamage();
    }

    // ----------------- Rotation -----------------

    private void UpdateRotation()
    {
        if (target == null || _maxTurnRateDeg <= 0f)
            return;

        Vector2 curDir = root.right;
        Vector2 toTarget = (Vector2)target.position - (Vector2)origin.position;

        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Vector2 desiredDir = toTarget.normalized;

        float curAngle = Mathf.Atan2(curDir.y, curDir.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(desiredDir.y, desiredDir.x) * Mathf.Rad2Deg;

        float newAngle = Mathf.MoveTowardsAngle(
            curAngle,
            targetAngle,
            _maxTurnRateDeg * Time.deltaTime);

        float rad = newAngle * Mathf.Deg2Rad;
        Vector2 newDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        root.right = newDir;
    }

    // ----------------- Scaling -----------------

    private void UpdateScaling()
    {
        if (_lengthGrowSpeed > 0f)
        {
            _currentLenScale = Mathf.MoveTowards(
                _currentLenScale,
                _targetLenScale,
                _lengthGrowSpeed * Time.deltaTime);
        }
        else
        {
            _currentLenScale = _targetLenScale;
        }

        if (_thicknessGrowSpeed > 0f)
        {
            _currentThickScale = Mathf.MoveTowards(
                _currentThickScale,
                _targetThickScale,
                _thicknessGrowSpeed * Time.deltaTime);
        }
        else
        {
            _currentThickScale = _targetThickScale;
        }
    }

    // ----------------- DoT / collision -----------------

    private bool IsValidTarget(Collider2D other, out HealthComponent hp)
    {
        hp = null;

        if (((1 << other.gameObject.layer) & _targetMask) == 0)
            return false;

        if (!other.TryGetComponent(out hp))
            return false;

        if (hp.IsDead)
        {
            hp = null;
            return false;
        }

        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidTarget(other, out HealthComponent hp)) return;
        float now = Time.time;
        if (!_nextTickTime.ContainsKey(hp))
        {
            _nextTickTime.Add(hp, now); // can tick immediately
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidTarget(other, out HealthComponent hp)) return;
        _nextTickTime.Remove(hp);
    }

    private void UpdateDamage()
    {
        if (_nextTickTime.Count == 0) return;
        if (_tickInterval <= 0f || _damagePerSecond <= 0f) return;

        float now = Time.time;
        float damagePerTick = _damagePerSecond * _tickInterval;

        // Copy keys to avoid modifying the dictionary while iterating.
        var keys = new List<HealthComponent>(_nextTickTime.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            HealthComponent hp = keys[i];
            if (hp == null || hp.IsDead)
            {
                _nextTickTime.Remove(hp);
                continue;
            }

            float nextTick = _nextTickTime[hp];
            if (now >= nextTick)
            {
                hp.Damage(damagePerTick,this.transform.position);
                _nextTickTime[hp] = now + _tickInterval;
            }
        }
    }
}
