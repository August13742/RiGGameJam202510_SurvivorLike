using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    public sealed class EnemyProjectile2D : MonoBehaviour
    {
        [Header("Damage / Motion")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float damage = 5f;

        [Header("Hit Sweep")]
        [SerializeField] private LayerMask hitMask;                  // e.g., Player


        [Header("Homing (optional)")]
        [SerializeField] private bool homing = false;
        [SerializeField] private float homingDuration = 1.0f;        // seconds of steering
        [SerializeField] private float maxTurnDegPerSec = 360f;      // clamp angular speed

        [Header("Facing")]
        [SerializeField] private bool forwardAxisIsRight = true;     // else Up

        private Vector2 _dir = Vector2.right;
        private float _timeLeft;
        private float _homingLeft;
        private Transform _homingTarget; // player

        // --- Public fire API ---
        /// <summary>
        /// If homingOverride is null => use prefab 'homing'.
        /// If non-null => force that value.
        /// If homingSecondsOverride is null => use prefab 'homingDuration'.
        /// If <= 0 => disables homing.
        /// </summary>
        public void Fire(
            Vector2 pos, Vector2 dir, float spd, float dmg, float life,
            Transform target = null,
            bool? homingOverride = null,
            float? homingSecondsOverride = null)
        {
            transform.position = pos;
            _dir = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.right;

            speed = spd;
            damage = dmg;
            _timeLeft = life;

            // --- Effective homing decision
            bool effectiveHoming = homingOverride ?? homing;                // prefab default unless overridden
            float effectiveSeconds = homingSecondsOverride ?? homingDuration;

            if (!effectiveHoming || target == null || effectiveSeconds <= 0f)
            {
                _homingLeft = 0f;
                _homingTarget = null;
            }
            else
            {
                _homingLeft = effectiveSeconds;
                _homingTarget = target;
            }

            OrientTo(_dir);
        }

        private void FixedUpdate()
        {
            // lifetime
            _timeLeft -= Time.fixedDeltaTime;
            if (_timeLeft <= 0f) { Destroy(gameObject); return; }

            // homing window
            if (_homingLeft > 0f && _homingTarget != null)
            {
                _homingLeft -= Time.fixedDeltaTime;
                Vector2 desired = ((Vector2)_homingTarget.position - (Vector2)transform.position);
                if (desired.sqrMagnitude > 1e-6f)
                {
                    float maxDeg = maxTurnDegPerSec * Time.fixedDeltaTime;
                    float currentAngle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
                    float targetAngle = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
                    float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxDeg);
                    _dir = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad));
                    OrientTo(_dir);
                }
            }

            // sweep
            float step = speed * Time.fixedDeltaTime;
            Vector2 p = transform.position;


            // free flight
            transform.position = p + _dir * step;
        }
        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!col.TryGetComponent<HealthComponent>(out var target)) return;
            if (target.IsDead) return;

            float dealt = damage;


            target.Damage(dealt);
            Destroy(gameObject);
        }

        private void OrientTo(Vector2 direction)
        {
            float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(forwardAxisIsRight ? ang : (ang - 90f), Vector3.forward);
        }

        private void OnBecameInvisible()   // safety for strays
        {
            Destroy(gameObject);
        }
    }
}
