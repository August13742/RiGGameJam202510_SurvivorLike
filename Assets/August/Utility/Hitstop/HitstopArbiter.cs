using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A single component that acts as an arbiter for all hitstop-related logic on a GameObject.
/// On Awake, it discovers all relevant "pausable" components (Animators, Rigidbodies, etc.)
/// and manages their state when hitstop is requested.
/// </summary>
[DisallowMultipleComponent]
public class HitstopArbiter : MonoBehaviour, IHitstoppable
{
    // A list of pure C# objects that know how to freeze/unfreeze a specific component.
    private readonly List<ISnapshot> _snapshots = new();

    #region Unity Lifecycle
    private void Start()
    {
        FindAndCachePausableComponents();
    }
    #endregion

    #region IHitstoppable Implementation
    public void OnHitstopStart()
    {
        // Tell all cached snapshots to freeze their target component.
        foreach (var snapshot in _snapshots)
        {
            snapshot.Freeze();
        }
    }

    public void OnHitstopEnd()
    {
        // Tell all cached snapshots to unfreeze.
        if (this != null)
        {
            foreach (var snapshot in _snapshots)
            {
                snapshot.Unfreeze();
            }
        }
    }

    #endregion

    /// <summary>
    /// Scans this GameObject for components that we want to control during hitstop
    /// and creates corresponding snapshot handlers for them.
    /// </summary>
    private void FindAndCachePausableComponents()
    {
        foreach (var component in GetComponents<Component>())
        {
            switch (component)
            {
                case Animator animator:
                    _snapshots.Add(new AnimatorSnapshot(animator));
                    break;

                case Rigidbody2D rb2d:
                    _snapshots.Add(new Rigidbody2DSnapshot(rb2d));
                    break;

                case ParticleSystem ps:
                    _snapshots.Add(new ParticleSystemSnapshot(ps));
                    break;

                    // Add more cases...
            }
        }
    }

    #region Internal Snapshot Classes

    private interface ISnapshot
    {
        void Freeze();
        void Unfreeze();
    }

    private class AnimatorSnapshot : ISnapshot
    {
        private readonly Animator _target;
        private float _originalSpeed;
        private bool _isFrozen = false; // State flag to prevent corruption

        public AnimatorSnapshot(Animator target) { _target = target; }

        public void Freeze()
        {
            if (_target == null || _isFrozen) return;

            _isFrozen = true;
            _originalSpeed = _target.speed;
            _target.speed = 0f;
        }

        public void Unfreeze()
        {
            if (_target == null || !_isFrozen) return;

            _isFrozen = false;
            _target.speed = _originalSpeed;
        }
    }

    private class Rigidbody2DSnapshot : ISnapshot
    {
        private readonly Rigidbody2D _target;
        private Vector2 _velocity;
        private float _angularVelocity;
        private RigidbodyType2D _originalBodyType;
        private bool _wasFrozen = false;

        public Rigidbody2DSnapshot(Rigidbody2D target) { _target = target; }

        public void Freeze()
        {
            if (_target == null || _wasFrozen) return;

            _originalBodyType = _target.bodyType;
            if (_originalBodyType != RigidbodyType2D.Dynamic) return;

            _wasFrozen = true;
            _velocity = _target.linearVelocity;
            _angularVelocity = _target.angularVelocity;
            _target.bodyType = RigidbodyType2D.Static;
        }

        public void Unfreeze()
        {
            if (_target == null || !_wasFrozen) return;

            _wasFrozen = false;
            _target.bodyType = _originalBodyType;

            // Restore velocity only if it was originally dynamic
            if (_originalBodyType == RigidbodyType2D.Dynamic)
            {
                _target.linearVelocity = _velocity;
                _target.angularVelocity = _angularVelocity;
            }
        }
    }

    private class ParticleSystemSnapshot : ISnapshot
    {
        private readonly ParticleSystem _target;
        private bool _wasPlaying;
        private bool _isFrozen = false;

        public ParticleSystemSnapshot(ParticleSystem target) { _target = target; }

        public void Freeze()
        {
            if (_target == null || _isFrozen) return;

            _isFrozen = true;
            _wasPlaying = _target.isPlaying;
            if (_wasPlaying) _target.Pause();
        }

        public void Unfreeze()
        {
            if (_target == null || !_isFrozen) return;

            _isFrozen = false;
            if (_wasPlaying) _target.Play();
        }
    }

#endregion
}