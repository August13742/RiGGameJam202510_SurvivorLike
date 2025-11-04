using Survivor.Game;
using Survivor.Weapon;
using UnityEngine;
namespace Survivor.Enemy
{

    public class BulletEnemy : EnemyBase
    {
        [SerializeField] private GameObject BulletPrefab;
        [SerializeField] private new ShooterEnemyDef _def;
        private float _shootTimer = 0f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _target = SessionManager.Instance.GetPlayerReference().transform;
            _shootTimer = 0;
        }

        private void FixedUpdate()
        {
            if (!_target || IsDead) return;

            Move(out bool CanShoot,
                preferredDistance:_def.PreferredDistance, 
                unsafeDistance:_def.UnsafeDistance);

            _shootTimer -= Time.fixedDeltaTime;

            if(_shootTimer <= 0f && CanShoot)
            {
                ShootPlayer();
                _shootTimer = _def.ShootIntervalSec;

            }
        }

        private void ShootPlayer()
        {
            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
            Vector2 dir = toTarget.normalized;

            GameObject go = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
            go.layer = LayerMask.NameToLayer("EnemyProjectile");

            var projectile = go.GetComponent<EnemyProjectile>();
            projectile.Fire(transform.position, dir, _def.ProjectileSpeed, _def.ProjectileDamage, _def.ProjectileLifeTimeSec);

        }
    }
}