using UnityEngine;

namespace Survivor.Weapon
{
    public sealed class WeaponStats
    {
        public float CooldownMul = 1f;   // e.g., 0.85 after upgrades
        public float DamageMul = 1f;     // e.g., 1.2
        public float AreaMul = 1f;       // e.g., 1.3
        public int ProjectilesAdd = 0;
        public int PierceAdd = 0;
        public float SpeedMul = 1f;
    }

}
