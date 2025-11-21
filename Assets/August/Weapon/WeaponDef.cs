using UnityEngine;

namespace Survivor.Weapon
{
    public enum TargetMode { SelfCentered, Nearest, RandomK }
    public abstract class WeaponDef : ScriptableObject
    {
        [Header("Runtime")]
        public GameObject RuntimePrefab;  // must contain a WeaponBase<ThisDef> component

        public string Id;
        public Sprite Icon;
        public SFXResource fireSFX;
        public SFXResource equipSFX;
        public int BaseDamage = 5;
        public float BaseCooldown = 0.5f;
        public TargetMode TargetingMode;
        public float AreaScale = 1f;          // used by melee/beam
        public int Projectiles = 1;           // used by projectile/summon

        [Header("Crit Stats")]
        [Range(0f, 1f)] public float BaseCritChance = 0.05f;      // 5% default
        [Min(1f)] public float BaseCritMultiplier = 1.5f;         // 1.5× default
    }
}