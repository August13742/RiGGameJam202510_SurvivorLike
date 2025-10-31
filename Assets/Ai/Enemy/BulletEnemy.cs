using UnityEngine;
namespace Survivor.Enemy
{

    public class BulletEnemy : EnemyBase
    {
        [SerializeField] private GameObject BulletPrefab;
        private float _shootCooldown = 7f;
        private float _shootTimer;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _target = GameObject.FindWithTag("Player")?.transform;

            _shootTimer = 0;
        }

        private void FixedUpdate()
        {
            Move();

            if (!_target || IsDead) return;

            _shootTimer -= Time.fixedDeltaTime;

            if(_shootTimer <= 0f)
            {
                ShootPlayer();
                _shootTimer = _shootCooldown;
            }
        }

        private void ShootPlayer()
        {
            Instantiate (BulletPrefab , transform.position, Quaternion.identity);
        }
    }
}