using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A singleton manager for creating "hitstop" or "hit lag" effects.
/// It supports both global (time scale-based) and targeted (component-based) freezes.
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
                // In case the manager wasn't in the scene, create it.
                GameObject go = new GameObject("HitstopManager");
                _instance = go.AddComponent<HitstopManager>();
            }
            return _instance;
        }
    }
    private readonly List<GameObject> _scratch = new(8);

    // ---- Configuration ----
    [Tooltip("GameObjects with this tag will be exempt from targeted freezes.")]
    [SerializeField]
    private string exemptTag = "HitstopExempt";

    // ---- State ----
    private Coroutine _globalHitstopCoroutine;
    private readonly Dictionary<GameObject, FreezeState> _frozenSubtrees = new Dictionary<GameObject, FreezeState>();
    private readonly Dictionary<GameObject, Coroutine> _unfreezeCoroutines = new Dictionary<GameObject, Coroutine>();

    #region Unity Lifecycle
    private void Awake()
    {
        // Enforce the singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            // (Optional) Keep the manager alive across scene loads.
            // DontDestroyOnLoad(gameObject);
        }
    }
    #endregion

    #region Data Transfer Objects (DTOs)
    /// <summary>
    /// Stores the state of all frozen components for a specific GameObject hierarchy.
    /// </summary>
    private class FreezeState
    {
        public float Deadline;
        public List<ComponentSnapshot> Snapshots = new List<ComponentSnapshot>();

        public FreezeState(float deadline)
        {
            Deadline = deadline;
        }
    }

    /// <summary>
    /// Abstract base class for capturing the state of a single component.
    /// </summary>
    private abstract class ComponentSnapshot
    {
        protected Component TargetComponent;
        public abstract void Unfreeze();

        protected bool IsComponentValid() => TargetComponent != null && TargetComponent.gameObject.activeInHierarchy;
    }

    // Specific snapshot implementations for different component types
    private class RigidbodySnapshot : ComponentSnapshot
    {
        private readonly Vector3 _velocity;
        private readonly Vector3 _angularVelocity;
        private readonly bool _wasKinematic;

        public RigidbodySnapshot(Rigidbody rb)
        {
            TargetComponent = rb;
            _velocity = rb.linearVelocity;
            _angularVelocity = rb.angularVelocity;
            _wasKinematic = rb.isKinematic;

            // Freeze
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        public override void Unfreeze()
        {
            if (!IsComponentValid()) return;
            var rb = (Rigidbody)TargetComponent;
            rb.isKinematic = _wasKinematic;
            rb.linearVelocity = _velocity;
            rb.angularVelocity = _angularVelocity;
        }
    }

    private class Rigidbody2DSnapshot : ComponentSnapshot
    {
        private readonly Vector2 _velocity;
        private readonly float _angularVelocity;
        private readonly bool _wasKinematic;

        public Rigidbody2DSnapshot(Rigidbody2D rb)
        {
            TargetComponent = rb;
            _velocity = rb.linearVelocity;
            _angularVelocity = rb.angularVelocity;
            _wasKinematic = rb.bodyType == RigidbodyType2D.Kinematic;

            // Freeze
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public override void Unfreeze()
        {
            if (!IsComponentValid()) return;
            var rb = (Rigidbody2D)TargetComponent;
            if (_wasKinematic) rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = _velocity;
            rb.angularVelocity = _angularVelocity;
        }
    }

    private class AnimatorSnapshot : ComponentSnapshot
    {
        private readonly float _originalSpeed;

        public AnimatorSnapshot(Animator animator)
        {
            TargetComponent = animator;
            _originalSpeed = animator.speed;

            // Freeze
            animator.speed = 0f;
        }

        public override void Unfreeze()
        {
            if (IsComponentValid())
            {
                ((Animator)TargetComponent).speed = _originalSpeed;
            }
        }
    }

    private class MonoBehaviourSnapshot : ComponentSnapshot
    {
        private readonly bool _wasEnabled;

        public MonoBehaviourSnapshot(MonoBehaviour script)
        {
            TargetComponent = script;
            _wasEnabled = script.enabled;

            // Freeze
            script.enabled = false;
        }

        public override void Unfreeze()
        {
            if (IsComponentValid())
            {
                ((MonoBehaviour)TargetComponent).enabled = _wasEnabled;
            }
        }
    }


    #endregion

    #region Public API
    

    /// <summary>
    /// Requests a hitstop effect.
    /// </summary>
    /// <param name="duration">The length of the freeze in seconds.</param>
    /// <param name="targets">An array of root GameObjects to freeze. If empty, triggers a GLOBAL hitstop.</param>
    /// <param name="timeScale">For global hitstop, the time scale to apply (0 for full freeze, >0 for slow-mo).</param>
    public void Request(float duration, IList<GameObject> targets = null, float timeScale = 0.0f)
    {
        if (duration <= 0.0f) return;

        bool isGlobal = targets == null || targets.Count == 0;

        if (isGlobal)
        {
            // --- Global Hitstop ---
            if (_globalHitstopCoroutine != null)
            {
                // If a new global request comes in, stop the old one and start a new one.
                // You could also choose to extend the duration instead.
                StopCoroutine(_globalHitstopCoroutine);
            }
            _globalHitstopCoroutine = StartCoroutine(GlobalHitstopCoroutine(duration, timeScale));
        }
        else
        {
            // --- Targeted Hitstop ---
            float deadline = Time.unscaledTime + duration;

            foreach (var root in targets)
            {
                if (root == null) continue;

                if (_frozenSubtrees.TryGetValue(root, out var state))
                {
                    // If this subtree is already frozen, just extend its deadline.
                    state.Deadline = Mathf.Max(state.Deadline, deadline);
                }
                else
                {
                    // Otherwise, freeze it for the first time.
                    _freezeSubtree(root, deadline);
                }

                // (Re)start the unfreeze coroutine for this root.
                if (_unfreezeCoroutines.TryGetValue(root, out var runningCoroutine))
                {
                    StopCoroutine(runningCoroutine);
                }
                _unfreezeCoroutines[root] = StartCoroutine(UnfreezeSubtreeCoroutine(root, duration));
            }
        }
    }
    

    public void Request(float duration, GameObject target)
    {
        _scratch.Clear(); _scratch.Add(target);
        Request(duration, _scratch);
    }

    #endregion

    #region Internal Logic

    // ---- Global Pause Handling ----
    private IEnumerator GlobalHitstopCoroutine(float duration, float timeScale)
    {
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
        _globalHitstopCoroutine = null;
    }

    // ---- Subtree Freeze/Unfreeze Handling ----
    private void _freezeSubtree(GameObject root, float deadline)
    {
        var freezeState = new FreezeState(deadline);
        _collectAndFreezeRecursive(root, freezeState);
        if (freezeState.Snapshots.Count > 0)
        {
            _frozenSubtrees[root] = freezeState;
        }
    }

    private void _collectAndFreezeRecursive(GameObject target, FreezeState state)
    {
        // Nodes with the exempt tag are skipped entirely, along with their children.
        if (target.CompareTag(exemptTag))
        {
            return;
        }

        // --- Freeze Components on the current GameObject ---

        // Freeze all MonoBehaviours (scripts) that aren't this manager
        foreach (var script in target.GetComponents<MonoBehaviour>())
        {
            if (script != this && script.enabled) // Don't disable ourselves!
            {
                state.Snapshots.Add(new MonoBehaviourSnapshot(script));
            }
        }

        // Freeze Animator
        if (target.TryGetComponent<Animator>(out var animator) && animator.enabled)
        {
            state.Snapshots.Add(new AnimatorSnapshot(animator));
        }

        // Freeze Physics
        if (target.TryGetComponent<Rigidbody>(out var rb))
        {
            state.Snapshots.Add(new RigidbodySnapshot(rb));
        }
        if (target.TryGetComponent<Rigidbody2D>(out var rb2d))
        {
            state.Snapshots.Add(new Rigidbody2DSnapshot(rb2d));
        }

        // NOTE: ParticleSystems and other components can be added here if needed.
        // For example:
        // if (target.TryGetComponent<ParticleSystem>(out var ps) && ps.isPlaying) { /* Save state and call ps.Pause() */ }

        // --- Recurse to Children ---
        foreach (Transform child in target.transform)
        {
            _collectAndFreezeRecursive(child.gameObject, state);
        }
    }

    private IEnumerator UnfreezeSubtreeCoroutine(GameObject root, float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        // Additional check to ensure we don't unfreeze prematurely if the deadline was extended.
        if (_frozenSubtrees.TryGetValue(root, out var state) && Time.unscaledTime >= state.Deadline)
        {
            _unfreezeSubtree(root);
        }
    }

    private void _unfreezeSubtree(GameObject root)
    {
        if (!_frozenSubtrees.TryGetValue(root, out var state))
        {
            return;
        }

        // Restore state in reverse order to avoid dependency issues.
        for (int i = state.Snapshots.Count - 1; i >= 0; i--)
        {
            state.Snapshots[i].Unfreeze();
        }

        _frozenSubtrees.Remove(root);
        _unfreezeCoroutines.Remove(root);
    }
    #endregion
}