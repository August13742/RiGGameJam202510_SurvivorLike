using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    public sealed class Projectile : MonoBehaviour
    {
        public int Damage { get; private set; }
        public float Speed { get; private set; }
        public int Pierce { get; private set; }

        [Header("Collision")]
        [SerializeField] private LayerMask hitMask = ~0;


        [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.Right;

        private Vector2 _dir;
        private float _lifeTime;
        private ObjectPool _pool;

        private enum ForwardAxis { Right, Up }

        public void SetPool(ObjectPool pool) { _pool = pool; }

        public void Fire(Vector2 p, Vector2 direction, float speed, int dmg, int pierce, float time)
        {
            transform.position = p;
            _dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            Speed = speed;
            Damage = dmg;
            Pierce = pierce;
            _lifeTime = time;

            // Rotate sprite to face travel direction
            float ang = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            if (forwardAxis == ForwardAxis.Right)
                transform.rotation = Quaternion.AngleAxis(ang, Vector3.forward);
            else // Up
                transform.rotation = Quaternion.AngleAxis(ang - 90f, Vector3.forward);

        }

        private void FixedUpdate()
        {
            _lifeTime -= Time.fixedDeltaTime;
            if (_lifeTime <= 0f) { Despawn(); return; }

            transform.position += (Vector3)(Speed * Time.fixedDeltaTime * _dir);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            // Layer mask filter
            if ((hitMask.value & (1 << col.gameObject.layer)) == 0) return;

            if (!col.TryGetComponent<HealthComponent>(out var target)) return;

            target.Damage(Damage);
            if (Pierce > 0) { Pierce--; }
            else { Despawn(); }
        }

        private void Despawn()
        {
            if (_pool != null) _pool.Return(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
