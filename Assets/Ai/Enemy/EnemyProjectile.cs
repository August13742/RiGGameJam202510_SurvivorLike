using UnityEngine;
using Survivor.Game;

namespace Survivor.Weapon
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        public float Damage { get; private set; }
        public float Speed { get; private set; }

        private enum ForwardAxis { Right, Up }
        [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.Right;

        private Vector2 _dir;
        private float _lifeTime;


        public void Fire(Vector2 p, Vector2 direction, float speed, float dmg, float time)
        {
            transform.position = p;
            _dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            Speed = speed;
            Damage = dmg;
            _lifeTime = time;

            float ang = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(
                (forwardAxis == ForwardAxis.Right) ? ang : (ang - 90f),
                Vector3.forward
            );
        }

        private void FixedUpdate()
        {
            _lifeTime -= Time.fixedDeltaTime;
            if (_lifeTime <= 0f) { Destroy(this.gameObject); return; }

            transform.position += (Vector3)(Speed * Time.fixedDeltaTime * _dir);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!col.TryGetComponent<HealthComponent>(out var target)) return;
            if (target.IsDead) return;

            float dealt = Damage;

   

            target.Damage(dealt);
        }

    }
}
