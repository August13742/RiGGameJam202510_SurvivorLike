using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A singleton manager for creating "hitstop" effects.
/// </summary>
public class HitstopManager : MonoBehaviour
{
    // ---- Singleton Pattern ----
    private static HitstopManager _instance;
    public static HitstopManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new ("HitstopManager");
                _instance = go.AddComponent<HitstopManager>();
            }
            return _instance;
        }
    }

    private readonly Dictionary<GameObject, Coroutine> _activeHitstops = new Dictionary<GameObject, Coroutine>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    /// <summary>
    /// Requests a hitstop effect on a specific target GameObject and its children.
    /// </summary>
    /// <param name="duration">The length of the freeze in seconds.</param>
    /// <param name="target">The root GameObject to freeze.</param>
    public void Request(float duration, GameObject target)
    {
        if (duration <= 0.0f || target == null) return;

        // If this target is already in hitstop, stop the old coroutine to start a new one.
        // This allows new hits to "refresh" the hitstop duration.
        if (_activeHitstops.TryGetValue(target, out Coroutine existingCoroutine))
        {
            StopCoroutine(existingCoroutine);
        }

        _activeHitstops[target] = StartCoroutine(HitstopCoroutine(duration, target));
    }

    private IEnumerator HitstopCoroutine(float duration, GameObject target)
    {
        // Find all components on the target and its children that can be stopped.
        IHitstoppable[] stoppables = target.GetComponentsInChildren<IHitstoppable>();

        // Start hitstop on all of them.
        foreach (var stoppable in stoppables)
        {
            stoppable.OnHitstopStart();
        }

        // Wait for the duration. Use unscaled time so global timeScale changes don't affect it.
        yield return new WaitForSecondsRealtime(duration);

        // End hitstop on all of them, checking if the object was destroyed in the meantime.
        if (target != null)
        {
            foreach (var stoppable in stoppables)
            {
                // The component reference could be null if its GameObject was destroyed
                if (stoppable as Object != null)
                {
                    stoppable.OnHitstopEnd();
                }
            }
        }

        // Clean up the entry from our tracking dictionary.
        _activeHitstops.Remove(target);
    }
}