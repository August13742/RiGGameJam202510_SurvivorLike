using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    public sealed class HazardZone2D : MonoBehaviour
    {
        [Header("Zone")]
        [SerializeField] private float radius = 2f;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private float lifetime = 3f;

        [Header("Damage")]
        [SerializeField] private float damagePerSecond = 5f;

        [Header("Visuals (optional)")]
        [SerializeField] private GameObject destroyVfxPrefab;

        private float _timeLeft;
        private static readonly Collider2D[] _hits = new Collider2D[8];

        public void Activate(Vector2 pos, float r, float dps, float life)
        {
            transform.position = pos;
            radius = r;
            damagePerSecond = dps;
            lifetime = life;
            _timeLeft = lifetime;
        }

        private void FixedUpdate()
        {
            _timeLeft -= Time.fixedDeltaTime;
            if (_timeLeft <= 0f)
            {
                if (destroyVfxPrefab != null)
                    Instantiate(destroyVfxPrefab, transform.position, Quaternion.identity);
                Destroy(gameObject);
                return;
            }

            float dt = Time.fixedDeltaTime;
            ContactFilter2D _filter = new() { useTriggers = true, useDepth = false };
            _filter.SetLayerMask(hitMask);
            int hitCount = Physics2D.OverlapCircle(transform.position, radius, _filter, _hits);
            for (int i = 0; i < hitCount; i++)
            {
                if (_hits[i] == null) continue;
                if (!_hits[i].TryGetComponent<HealthComponent>(out var hp)) continue;
                if (hp.IsDead) continue;

                hp.Damage(damagePerSecond * dt);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
