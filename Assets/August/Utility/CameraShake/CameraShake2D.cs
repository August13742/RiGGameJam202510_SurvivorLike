using System;
using System.Collections.Generic;
using UnityEngine;

namespace AugustsUtility.CameraShake
{
    /// <summary>
    /// Singleton VFXManager: accumulates camera shake jobs and applies a summed offset
    /// to the active IShakeReceiver each frame (LateUpdate).
    /// Fire-and-forget API: Shake(), ShakeLight(), ShakeHeavy().
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraShake2D : MonoBehaviour
    {
        // ---- Singleton ----
        public static CameraShake2D Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Optional
             //DontDestroyOnLoad(gameObject);
        }

        [Serializable]
        private sealed class CameraShakeJob
        {
            public float tLeft;
            public float duration;
            public float strength;

            public CameraShakeJob(float d, float s)
            {
                duration = Mathf.Max(d, 0.0001f);
                tLeft = d;
                strength = s;
            }
        }

        // ---- State ----
        [SerializeField] private float maxAccumulatedStrength = 64f; // global clamp
        private readonly List<CameraShakeJob> _jobs = new ();
        private IShakeReceiver _receiverCached;

        // ---- Receiver resolution ----
        /// <summary>
        /// Returns a valid IShakeReceiver if present and active. Caches between frames.
        /// </summary>
        private IShakeReceiver GetActiveReceiver()
        {
            // Still valid?
            if (_receiverCached is Component c && c != null && c.gameObject.activeInHierarchy)
                return _receiverCached;

            // Try: main camera�fs receiver
            Camera cam = Camera.main;
            if (cam != null)
            {
                IShakeReceiver r = cam.GetComponentInParent<IShakeReceiver>();
                if (r != null) { _receiverCached = r; return r; }
            }

            // Fallback: any receiver in scene
            if (FindAnyObjectByType<MonoBehaviour>(FindObjectsInactive.Exclude) is IShakeReceiver any) { _receiverCached = any; return any; }

            _receiverCached = null;
            return null;
        }

        public void RegisterReceiver(IShakeReceiver receiver)
        {
            _receiverCached = receiver;
        }

        // ---- Public API (fire-and-forget) ----
        public static void Shake(float duration = 0.30f, float strength = 10.0f)
        {
            if (Instance == null) return;
            Instance._jobs.Add(new CameraShakeJob(duration, strength));
        }

        public static void ShakeLight() => Shake(0.12f, 5.0f);
        public static void ShakeHeavy() => Shake(0.45f, 20.0f);

        // ---- Driver ----
        private void LateUpdate()
        {
            // Decay-only path when no receiver exists
            IShakeReceiver r = GetActiveReceiver();
            if (_jobs.Count == 0)
            {
                if (r != null) r.ClearShake();
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (r == null)
            {
                DecayJobs(dt);
                return;
            }

            // Sum decayed random directions, clamp by maxAccumulatedStrength
            Vector2 accum = Vector2.zero;
            float strengthSum = 0f;

            for (int i = 0; i < _jobs.Count; i++)
            {
                CameraShakeJob j = _jobs[i];
                j.tLeft = Mathf.Max(0f, j.tLeft - dt);
                float decay = (j.duration <= 0f) ? 0f : (j.tLeft / j.duration); // linear falloff
                if (decay <= 0f) continue;

                float s = j.strength * decay;
                strengthSum += s;

                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                accum += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * s;
            }

            if (strengthSum > maxAccumulatedStrength && accum.sqrMagnitude > 0f)
            {
                float k = maxAccumulatedStrength / strengthSum;
                accum *= k;
            }

            // Apply this frame
            r.SetShakeOffset(accum);

            // Remove finished jobs
            _jobs.RemoveAll(j => j.tLeft <= 0f);
        }

        private void DecayJobs(float dt)
        {
            for (int i = 0; i < _jobs.Count; i++)
            {
                var j = _jobs[i];
                j.tLeft = Mathf.Max(0f, j.tLeft - dt);
            }
            _jobs.RemoveAll(j => j.tLeft <= 0f);
        }
    }
}