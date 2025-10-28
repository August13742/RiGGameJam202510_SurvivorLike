using UnityEngine;

namespace Survivor.Weapon
{
    public enum TargetMode { SelfCentered, Nearest, RandomK }
    public abstract class WeaponDef : ScriptableObject
    {
        
        public string Id;
        public Sprite Icon;
        public int BaseDamage = 5;
        public float BaseCooldown = 0.5f;
        public TargetMode TargetingMode;
        public float AreaScale = 1f;          // used by melee/beam
        public int Projectiles = 1;           // used by projectile/summon
    }

}
