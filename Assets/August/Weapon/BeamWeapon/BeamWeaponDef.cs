using UnityEngine;
namespace Survivor.Weapon
{

    [CreateAssetMenu(menuName = "Defs/Weapons/Beam")]
    public sealed class BeamWeaponDef : WeaponDef
    {
        public GameObject BeamPrefab;
        [Header("Beam Shape")]
        [Min(0.1f)] public float BeamLength = 6f;
        [Min(0.05f)] public float BeamWidth = 0.35f;

        [Header("Lifecycle")]
        [Min(0.05f)] public float Duration = 1f;
        [Min(1)] public int TicksPerSecond = 5;   // ticks per second

        [Header("Visuals")]
        public Material BeamMaterial;           // tileable texture, unlit/emissive
        [Min(0f)] public float UVScrollRate = 6f; // units/sec along beam
        public AnimationCurve AlphaOverLife = AnimationCurve.EaseInOut(0, 1, 1, 0);

        public float SpreadDeg = 5f;

        public bool FollowOrigion = false;
        public bool FollowDirection = false;
    }
}