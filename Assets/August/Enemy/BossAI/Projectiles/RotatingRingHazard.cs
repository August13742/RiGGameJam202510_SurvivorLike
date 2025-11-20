using System.Collections.Generic;
using AugustsUtility.Tween;
using Survivor.Game;
using System.Collections;
using UnityEngine;
using AugustsUtility.CameraShake;

[DisallowMultipleComponent]
public sealed class RotatingRingHazard : MonoBehaviour
{
    [Header("Collision Reporters")]
    [Tooltip("Reporter attached to the OUTER ring collider (damage region).")]
    [SerializeField] private ColliderReporter2D outerReporter;

    [Tooltip("Reporter attached to the INNER safe-zone collider (child object).")]
    [SerializeField] private ColliderReporter2D innerReporter;

    [SerializeField] private ColliderReporter2D starsReporter;

    [Header("Damage")]
    [SerializeField] private float baseDamagePerTick = 5f;
    [SerializeField] private float tickInterval = 0.25f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private float starsDamage = 10f;

    [SerializeField] float starCameraShakeStrength = 1.5f;
    [SerializeField] float starCameraShakeDuration = 0.3f;

    [Header("Rings (Visual / Orbit Layers)")]
    [Tooltip("Each entry represents one visual ring/orbit around the boss.")]
    [SerializeField] private RingLayer[] rings;

    [Header("Radius Levels")]
    [Tooltip("Multipliers relative to the system's base scale. " +
             "e.g. [1.0, 0.7, 0.4] → full, medium, small.")]
    [SerializeField] private float[] radiusLevels = new float[] { 1f, 0.6f, 0.3f };

    [Header("Audio")]
    [SerializeField] private SFXResource ambientLoopSFX;
    [SerializeField] private SFXResource starCollisionSFX;

    [Tooltip("Volume at smallest radius multiplier.")]
    [SerializeField] private float ambientVolumeAtMinRadius = 0.4f;

    [Tooltip("Volume at largest radius multiplier.")]
    [SerializeField] private float ambientVolumeAtMaxRadius = 1.0f;

    [Tooltip("Pitch at smallest radius multiplier.")]
    [SerializeField] private float ambientPitchAtMinRadius = 0.9f;

    [Tooltip("Pitch at largest radius multiplier.")]
    [SerializeField] private float ambientPitchAtMaxRadius = 1.1f;

    // --- internal state ---
    private readonly Dictionary<HealthComponent, int> _outerCounts = new Dictionary<HealthComponent, int>();
    private readonly Dictionary<HealthComponent, int> _innerCounts = new Dictionary<HealthComponent, int>();

    private float _tickTimer;
    private float _effectiveDamagePerTick;
    private Vector3 _baseScale;
    private ValueTween<Vector3> _radiusTween;

    // Global spin multiplier (for patterns like "enrage → all rings spin faster")
    private float _globalSpinMultiplier = 1f;

    // NEW: handle to the ambient loop voice
    private AudioHandle _ambientHandle;

    public float BaseDamagePerTick => baseDamagePerTick;
    public float EffectiveDamagePerTick
    {
        get => _effectiveDamagePerTick;
        set => _effectiveDamagePerTick = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Public accessor so attack patterns can query the radius multiplier for a level.
    /// Uses the same clamping & safety as the internal method.
    /// </summary>
    public float GetRadiusMultiplierForLevel(int levelIndex)
    {
        return GetMultiplierForLevel(levelIndex);
    }

    public Vector3 BaseScale => _baseScale;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _effectiveDamagePerTick = baseDamagePerTick;
        _globalSpinMultiplier = 1f;

        if (outerReporter == null)
        {
            outerReporter = GetComponent<ColliderReporter2D>();
        }

        if (innerReporter == null)
        {
            // Inner safe-zone reporter expected somewhere in children.
            innerReporter = GetComponentInChildren<ColliderReporter2D>();
        }

        if (outerReporter != null)
        {
            outerReporter.TriggerEnter += OnOuterEnter;
            outerReporter.TriggerExit += OnOuterExit;
        }
        if(starsReporter != null)
        {
            starsReporter.TriggerEnter += OnStarsEnter;
        }
        else
        {
            Debug.LogWarning("RotatingRingHazard: outerReporter not assigned.", this);
        }

        if (innerReporter != null)
        {
            innerReporter.TriggerEnter += OnInnerEnter;
            innerReporter.TriggerExit += OnInnerExit;
        }
        else
        {
            Debug.LogWarning("RotatingRingHazard: innerReporter not assigned.", this);
        }

        _tickTimer = tickInterval;
    }
    private void OnEnable()
    {
        _ambientHandle = AudioManager.Instance.PlaySFX(ambientLoopSFX, transform.position, transform);

    }
    private void OnDestroy()
    {
        if (outerReporter != null)
        {
            outerReporter.TriggerEnter -= OnOuterEnter;
            outerReporter.TriggerExit -= OnOuterExit;
        }

        if (innerReporter != null)
        {
            innerReporter.TriggerEnter -= OnInnerEnter;
            innerReporter.TriggerExit -= OnInnerExit;
        }
        if (starsReporter != null)
        {
            starsReporter.TriggerEnter -= OnStarsEnter;
        }

        if (_ambientHandle.IsValid)
        {
            _ambientHandle.Stop();
            _ambientHandle = AudioHandle.Invalid;
        }
    }

    private void Update()
    {
        // --- Spin each ring independently ---
        if (rings != null && rings.Length > 0)
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < rings.Length; i++)
            {
                RingLayer layer = rings[i];
                if (layer == null || layer.Transform == null) continue;

                float spin = layer.BaseSpinSpeed * layer.SpinMultiplier * _globalSpinMultiplier;
                if (Mathf.Abs(spin) > Mathf.Epsilon)
                {
                    layer.Transform.Rotate(0f, 0f, spin * dt, Space.Self);
                }
            }
        }

        // --- Damage ticks ---
        if (tickInterval <= 0f) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer > 0f) return;

        _tickTimer += tickInterval;
        if (_outerCounts.Count == 0) return;

        // We are only reading; dictionary not modified in this loop.
        foreach (KeyValuePair<HealthComponent, int> kv in _outerCounts)
        {
            HealthComponent hp = kv.Key;
            if (hp == null || hp.IsDead) continue;

            // If also inside inner safe zone → no damage.
            if (_innerCounts.ContainsKey(hp)) continue;

            hp.Damage(_effectiveDamagePerTick,this.transform.position);
        }

        UpdateAmbientAudioFromRadius();
    }

    private void UpdateAmbientAudioFromRadius()
    {
        if (!_ambientHandle.IsValid) return;

        // Estimate current radius multiplier as scale.x relative to base scale.x
        float currentMul = 1f;
        if (_baseScale.x > Mathf.Epsilon)
        {
            currentMul = transform.localScale.x / _baseScale.x;
        }

        // Derive min/max multipliers from radiusLevels
        float minMul = float.MaxValue;
        float maxMul = 0f;

        if (radiusLevels != null && radiusLevels.Length > 0)
        {
            for (int i = 0; i < radiusLevels.Length; i++)
            {
                float v = radiusLevels[i];
                if (v <= 0f) continue;
                if (v < minMul) minMul = v;
                if (v > maxMul) maxMul = v;
            }
        }

        // Fallback if radiusLevels not configured
        if (minMul == float.MaxValue || maxMul <= 0f)
        {
            minMul = 1f;
            maxMul = 1f;
        }

        // Map currentMul into [0,1] over [minMul, maxMul]
        float t = Mathf.InverseLerp(minMul, maxMul, currentMul);

        // Volume & pitch interpolation
        float vol = Mathf.Lerp(ambientVolumeAtMinRadius, ambientVolumeAtMaxRadius, t);
        float pitch = Mathf.Lerp(ambientPitchAtMinRadius, ambientPitchAtMaxRadius, t);

        _ambientHandle.SetVolume(vol);
        _ambientHandle.SetPitch(pitch);
    }

    /// <summary>
    /// Fire-and-forget version of PulseToLevelAndBack.
    /// Expands to targetLevel, holds, then returns to base (level 0),
    /// without the caller needing to yield on it.
    /// </summary>
    public IEnumerator PulseToLevelAndBack(
        int targetLevel,
        float shrinkDuration,
        float holdDuration,
        float expandDuration,
        System.Func<float, float> easeShrink = null,
        System.Func<float, float> easeExpand = null)
    {
        // shrink
        yield return TweenToLevelRoutine(targetLevel, shrinkDuration, easeShrink);

        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }

        // expand back to level 0 (assumed base)
        yield return TweenToLevelRoutine(0, expandDuration, easeExpand);
    }
    /// <summary>
    /// Fire-and-forget version of PulseToLevelAndBack.
    /// Expands to targetLevel, holds, then returns to base (level 0),
    /// without the caller needing to yield on it.
    /// </summary>
    public void PlayPulseToLevelAndBack(
        int targetLevel,
        float shrinkDuration,
        float holdDuration,
        float expandDuration,
        System.Func<float, float> easeShrink = null,
        System.Func<float, float> easeExpand = null)
    {
        StartCoroutine(PulseToLevelAndBack(
            targetLevel,
            shrinkDuration,
            holdDuration,
            expandDuration,
            easeShrink,
            easeExpand));
    }




    #region Collision / Target bookkeeping

    private bool IsValidTarget(Collider2D other, out HealthComponent hp)
    {
        hp = null;

        if (((1 << other.gameObject.layer) & targetMask) == 0)
        {
            return false;
        }

        if (!other.TryGetComponent(out hp))
        {
            return false;
        }

        if (hp.IsDead)
        {
            hp = null;
            return false;
        }

        return true;
    }

    private void OnOuterEnter(Collider2D self, Collider2D other)
    {
        if (!IsValidTarget(other, out HealthComponent hp)) return;

        if (_outerCounts.TryGetValue(hp, out int count))
        {
            _outerCounts[hp] = count + 1;
        }
        else
        {
            _outerCounts.Add(hp, 1);
        }
    }

    private void OnOuterExit(Collider2D self, Collider2D other)
    {
        if (!IsValidTarget(other, out HealthComponent hp)) return;
        if (!_outerCounts.TryGetValue(hp, out int count)) return;

        count--;
        if (count <= 0)
        {
            _outerCounts.Remove(hp);
        }
        else
        {
            _outerCounts[hp] = count;
        }
    }

    private void OnInnerEnter(Collider2D self, Collider2D other)
    {
        if (!IsValidTarget(other, out HealthComponent hp)) return;

        if (_innerCounts.TryGetValue(hp, out int count))
        {
            _innerCounts[hp] = count + 1;
        }
        else
        {
            _innerCounts.Add(hp, 1);
        }
    }

    private void OnInnerExit(Collider2D self, Collider2D other)
    {
        if (!IsValidTarget(other, out HealthComponent hp)) return;
        if (!_innerCounts.TryGetValue(hp, out int count)) return;

        count--;
        if (count <= 0)
        {
            _innerCounts.Remove(hp);
        }
        else
        {
            _innerCounts[hp] = count;
        }
    }
    
    private void OnStarsEnter(Collider2D self, Collider2D other)
    {
        if (!IsValidTarget(other, out HealthComponent hp)) return;
        hp.Damage(starsDamage,this.transform.position);
        AudioManager.Instance?.PlaySFX(starCollisionSFX, transform.position);
        CameraShake2D.Shake(starCameraShakeDuration, starCameraShakeStrength);
    }

    #endregion

    #region Radius levels / scale tween API (system-wide)

    private int ClampLevelIndex(int levelIndex)
    {
        if (radiusLevels == null || radiusLevels.Length == 0)
        {
            // fallback: pretend we have only level 0 = 1.0
            return 0;
        }

        return Mathf.Clamp(levelIndex, 0, radiusLevels.Length - 1);
    }

    private float GetMultiplierForLevel(int levelIndex)
    {
        if (radiusLevels == null || radiusLevels.Length == 0)
            return 1f;

        int idx = ClampLevelIndex(levelIndex);
        float mul = radiusLevels[idx];

        // Safety: avoid negative or zero unless you intentionally put it there.
        if (mul <= 0f) mul = 0.0001f;

        return mul;
    }

    private Vector3 GetScaleForLevel(int levelIndex)
    {
        float mul = GetMultiplierForLevel(levelIndex);
        return _baseScale * mul;
    }

    /// <summary>
    /// Instant snap to one of the radius levels (no tween).
    /// </summary>
    public void SnapToLevel(int levelIndex)
    {
        transform.localScale = GetScaleForLevel(levelIndex);
    }

    /// <summary>
    /// Tween the entire ring system to a given level using your Tween system.
    /// Returns the tween so callers can chain/kill/yield.
    /// </summary>
    public ValueTween<Vector3> TweenToLevel(int levelIndex, float duration,
    System.Func<float, float> ease = null,
    System.Action onComplete = null)
    {
        Vector3 targetScale = GetScaleForLevel(levelIndex);

        // Kill any existing radius tween so we don't have two writers.
        if (_radiusTween != null && _radiusTween.IsActive)
        {
            _radiusTween.Kill();
            _radiusTween = null;
        }

        _radiusTween = transform.TweenLocalScale(targetScale, duration, ease, () =>
        {
            onComplete?.Invoke();
            // tween is done; clear handle
            _radiusTween = null;
        });

        return _radiusTween;
    }


    /// <summary>
    /// Coroutine wrapper for TweenToLevel so attack patterns can do:
    /// yield return ring.TweenToLevelRoutine(2, 0.4f, EasingFunctions.EaseInOutQuad);
    /// </summary>
    public System.Collections.IEnumerator TweenToLevelRoutine(int levelIndex, float duration,
        System.Func<float, float> ease = null)
    {
        ValueTween<Vector3> tween = TweenToLevel(levelIndex, duration, ease);
        if (tween != null)
        {
            yield return tween; // waits while IsActive
        }
    }


    #endregion

    #region Damage / spin multipliers

    public void SetDamageMultiplier(float multiplier)
    {
        _effectiveDamagePerTick = baseDamagePerTick * Mathf.Max(0f, multiplier);
    }

    public void ResetDamageMultiplier()
    {
        _effectiveDamagePerTick = baseDamagePerTick;
    }

    /// <summary>
    /// Global spin multiplier applied on top of per-ring settings.
    /// Used by attack patterns (e.g. enrage, expand pulse).
    /// </summary>
    public void SetSpinMultiplier(float multiplier)
    {
        _globalSpinMultiplier = multiplier;
    }

    public void ResetSpinMultiplier()
    {
        _globalSpinMultiplier = 1f;
    }

    /// <summary>
    /// Per-ring spin multiplier override (e.g. only speed up outer ring).
    /// </summary>
    public void SetRingSpinMultiplier(int ringIndex, float multiplier)
    {
        if (rings == null || ringIndex < 0 || ringIndex >= rings.Length) return;
        rings[ringIndex].SpinMultiplier = multiplier;
    }

    public void ResetRingSpinMultiplier(int ringIndex)
    {
        if (rings == null || ringIndex < 0 || ringIndex >= rings.Length) return;
        rings[ringIndex].SpinMultiplier = 1f;
    }

    #endregion
}

[System.Serializable]
public sealed class RingLayer
{
    [Tooltip("Root transform for this visual ring/orbit.")]
    public Transform Transform;

    [Tooltip("Base spin speed in degrees per second (Z axis). Sign controls direction.")]
    public float BaseSpinSpeed = 120f;

    [Tooltip("Per-ring multiplier applied on top of BaseSpinSpeed and the hazard's global spin multiplier.")]
    public float SpinMultiplier = 1f;
}
