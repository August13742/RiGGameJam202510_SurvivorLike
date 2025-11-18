using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles per-target hitstop AND global hitstop (with independent durations).
/// Global hitstop uses unscaled time and stacks logically: longest request wins.
/// </summary>
public sealed class HitstopManager : MonoBehaviour
{
    private static HitstopManager _instance;
    public static HitstopManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new("HitstopManager");
                _instance = go.AddComponent<HitstopManager>();
            }
            return _instance;
        }
    }

    // ---- Per-target hitstop ----
    private readonly Dictionary<GameObject, Coroutine> _activeLocal = new();

    // ---- Global hitstop ----
    private readonly List<Coroutine> _activeGlobal = new();
    private bool _isGlobalPaused = false;
    private float _savedTimeScale = 1f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    // ============================================================
    //  LOCAL HITSTOP (per object)
    // ============================================================
    public void Request(float duration, GameObject target)
    {
        if (duration <= 0f || target == null) return;

        // Refresh existing
        if (_activeLocal.TryGetValue(target, out Coroutine existing))
        {
            StopCoroutine(existing);
        }

        var c = StartCoroutine(LocalHitstopCoroutine(duration, target));
        _activeLocal[target] = c;
    }

    private IEnumerator LocalHitstopCoroutine(float duration, GameObject target)
    {
        IHitstoppable[] stoppables = target.GetComponentsInChildren<IHitstoppable>();

        foreach (var s in stoppables)
            s.OnHitstopStart();

        yield return new WaitForSecondsRealtime(duration);

        if (target != null)
        {
            foreach (var s in stoppables)
                if (s as Object != null)
                    s.OnHitstopEnd();
        }

        _activeLocal.Remove(target);
    }

    // ============================================================
    //  GLOBAL HITSTOP (true freeze-frame)
    // ============================================================
    /// <summary>
    /// Public API: triggers global hitstop for 'duration' seconds (unscaled).
    /// Multiple calls stack: global pause ends only when ALL timers finish.
    /// </summary>
    public void RequestGlobal(float duration)
    {
        if (duration <= 0f) return;

        Coroutine c = StartCoroutine(GlobalHitstopCoroutine(duration));
        _activeGlobal.Add(c);

        if (!_isGlobalPaused)
            BeginGlobalPause();
    }

    private IEnumerator GlobalHitstopCoroutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        _activeGlobal.RemoveAt(0); // remove THIS coroutine

        if (_activeGlobal.Count == 0)
            EndGlobalPause();
    }

    // ============================================================
    //  GLOBAL CONTROL
    // ============================================================
    private void BeginGlobalPause()
    {
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _isGlobalPaused = true;
    }

    private void EndGlobalPause()
    {
        Time.timeScale = _savedTimeScale;
        _isGlobalPaused = false;
    }
}
