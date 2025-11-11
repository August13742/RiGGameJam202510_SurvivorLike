using Survivor.Weapon;
using UnityEngine;
namespace Survivor.Enemy
{

    public class BulletEnemy : EnemyBase
    {
        [SerializeField] private GameObject BulletPrefab;
        [SerializeField] private ShooterEnemyDef def;
        private float _shootTimer = 0f;
        private GameObject _projectileRoot;
        void Start()
        {
            _target = Game.SessionManager.Instance.GetPlayerReference().transform;
            _shootTimer = 0;
            _projectileRoot = GameObject.FindGameObjectWithTag("EnemyProjectiles");
        }

        private void FixedUpdate()
        {
            if (!_target || IsDead) return;

            Move(out bool CanShoot,
                preferredDistance: def.PreferredDistance, 
                unsafeDistance:def.UnsafeDistance);

            _shootTimer -= Time.fixedDeltaTime;

            if(_shootTimer <= 0f && CanShoot)
            {
                TryShootPlayer();
                _shootTimer = def.ShootIntervalSec;

            }
        }

        private void TryShootPlayer()
        {
            _animator.Play("Shoot");
        }

        private void ShootPlayer()
        {
            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
            Vector2 dir = toTarget.normalized;

            GameObject go = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
            if(_projectileRoot != null) go.transform.SetParent(_projectileRoot.transform);
            go.layer = LayerMask.NameToLayer("EnemyProjectile");

            var projectile = go.GetComponent<EnemyProjectile>();
            projectile.Fire(transform.position, dir, def.ProjectileSpeed, def.ProjectileDamage, def.ProjectileLifeTimeSec);

        }
    }
}