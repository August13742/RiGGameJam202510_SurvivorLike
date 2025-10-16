using UnityEngine;
namespace Survivor.Weapon
{

    [CreateAssetMenu(menuName = "Defs/Weapons/Beam")]
    public sealed class BeamWeaponDef : WeaponDef
    {
        public float TickDamageInterval = 0.1f;
        public float Range = 9f;
        public LayerMask HitMask = ~0;
    }
}