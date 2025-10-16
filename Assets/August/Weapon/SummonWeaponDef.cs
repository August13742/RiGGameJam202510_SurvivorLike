using UnityEngine;
namespace Survivor.Weapon
{

    [CreateAssetMenu(menuName = "Defs/Weapons/Summon")]
    public sealed class SummonWeaponDef : WeaponDef
    {
        public GameObject MinionPrefab;
        public int MaxMinions = 6;
        public float ReSummonCooldown = 2f;
    }
}