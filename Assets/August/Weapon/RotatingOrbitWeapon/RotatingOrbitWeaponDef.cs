using UnityEngine;

namespace Survivor.Weapon
{
    [CreateAssetMenu(menuName = "Defs/Weapons/Rotating Orbit")]
    public sealed class RotatingOrbitWeaponDef : WeaponDef
    {
        [Header("Orbit Shape (base)")]
        [Min(0.05f)] public float Radius = 3.0f;

        [Header("Area Scaling Split")]
        [Tooltip("Portion of (AreaMul) applied to orbÅfs local scale. 0..1")]
        [Range(0f, 1f)] public float OrbAreaBias = 0.8f;
        [Tooltip("Portion of (AreaMul) applied to orbit radius (uses sqrt for tame growth). 0..1")]
        [Range(0f, 1f)] public float RadiusAreaBias = 0.2f;
        [Tooltip("Clamp for orb local scale after area biasing.")]
        public Vector2 OrbScaleClamp = new (0.6f, 3.0f);
        [Tooltip("Clamp for radius multiplier after area biasing.")]
        public Vector2 RadiusMulClamp = new (0.6f, 2.5f);

        [Header("Motion")]
        [Tooltip("Seconds per full revolution (360Åã).")]
        [Min(0.05f)] public float RotationTime = 1.0f;
        [Tooltip("How many full revolutions to run before despawning.")]
        [Min(0.25f)] public float Revolutions = 3.0f;
        [Tooltip("If true, orbit tracks the owner while spinning.")]
        public bool FollowOrigin = true;
        public bool Clockwise = false;

        [Tooltip("ProgressÅ®angle profile (0..1). If null, uses easeInOutCirc.")]
        public AnimationCurve MotionCurve = null;

        [Header("Prefab")]
        [Tooltip("Prefab with RotatingOrbitOrb + Collider2D(isTrigger).")]
        public GameObject OrbPrefab;

        [Header("Collision")]
        [Tooltip("Optional: limit re-hits per target within a single orb lifetime. 0 = unlimited.")]
        [Min(0)] public int MaxHitsPerTarget = 0; // 0 = unlimited

        [Header("Visuals (optional)")]
        [Tooltip("Enable/disable collider+renderer while active/inactive.")]
        public bool ToggleRendererAndCollider = true;

        private void OnValidate()
        {
            if (OrbScaleClamp.x <= 0f) OrbScaleClamp.x = 0.01f;
            if (OrbScaleClamp.y < OrbScaleClamp.x) OrbScaleClamp.y = OrbScaleClamp.x;
            if (RadiusMulClamp.x <= 0f) RadiusMulClamp.x = 0.01f;
            if (RadiusMulClamp.y < RadiusMulClamp.x) RadiusMulClamp.y = RadiusMulClamp.x;
        }
    }
}
