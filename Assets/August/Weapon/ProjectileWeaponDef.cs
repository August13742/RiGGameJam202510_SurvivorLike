using UnityEngine;
namespace Survivor.Weapon
{ 

    [CreateAssetMenu(menuName = "Defs/Weapons/Projectile")]
    public sealed class ProjectileWeaponDef : WeaponDef
    {
        public GameObject ProjectilePrefab;
        public float ProjectileSpeed = 8f;
        public int Pierce = 0;
        public float Lifetime = 4f;
        public float SpreadDeg = 6f;          // for multishot fan
    }
}