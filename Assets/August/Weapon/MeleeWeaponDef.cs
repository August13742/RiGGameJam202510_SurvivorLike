using UnityEngine;
namespace Survivor.Weapon
{
    [CreateAssetMenu(menuName = "Defs/Weapons/Melee")]
    public sealed class MeleeWeaponDef : WeaponDef
    {
        public GameObject HitboxPrefab;       // a trigger arc
        public float Windup = 0.08f;
        public float Active = 0.12f;
        public float Recovery = 0.12f;
        public float Knockback = 0f;
    }
}