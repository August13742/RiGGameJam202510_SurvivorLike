using UnityEngine;

namespace Survivor.Weapon
{
    [CreateAssetMenu(menuName = "Defs/Weapons/Rotating Orbit")]
    public sealed class RotatingOrbitWeaponDef : WeaponDef
    {
        [Header("Orbit Shape")]
        [Min(0.05f)] public float Radius = 3.0f;

        [Header("Motion")]
        [Tooltip("Seconds per full revolution (360Åã).")]
        [Min(0.05f)] public float RotationTime = 1.0f;
        [Tooltip("How many full revolutions to run before despawning.")]
        [Min(0.25f)] public float Revolutions = 3.0f;
        [Tooltip("If true, orbit tracks the owner while spinning.")]
        public bool FollowOrigin = true;

        [Header("Prefab")]
        [Tooltip("Prefab with RotatingOrbitOrb + Collider2D(isTrigger).")]
        public GameObject OrbPrefab;

        [Header("Collision")]
        [Tooltip("Optional: limit re-hits per target within a single orb lifetime. 0 = unlimited.")]
        [Min(0)] public int MaxHitsPerTarget = 0; // 0 = unlimited

        [Header("Visuals (optional)")]
        [Tooltip("Enable/disable collider+renderer while active/inactive.")]
        public bool ToggleRendererAndCollider = true;
    }
}
