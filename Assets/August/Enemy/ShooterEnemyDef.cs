using UnityEngine;

namespace Survivor.Enemy
{
    [CreateAssetMenu(menuName = "Defs/Enemy/Shooter")]
    public class ShooterEnemyDef : EnemyDef
    {
        [Header("Shooter Specs")]
        public float ProjectileDamage = 5f;
        public float ShootIntervalSec = 5f;
        public float PreferredDistance = 16f; // will shoot when distance is between preferred & Unsafe
        public float UnsafeDistance = 8f;
        public float ProjectileLifeTimeSec = 7f;
        public float ProjectileSpeed = 8f;


        private void OnValidate()
        {
            if (UnsafeDistance> PreferredDistance)
            {
                UnsafeDistance = PreferredDistance;
            }
        }
    }
}