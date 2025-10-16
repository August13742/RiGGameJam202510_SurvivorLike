using UnityEngine;

namespace Survivor.Enemy
{
    [CreateAssetMenu(menuName = "Defs/EnemyDef")]
    public sealed class EnemyDef : ScriptableObject
    {
        [Header("Prefab")]
        public GameObject Prefab;              // Must include EnemyMarker, Rigidbody2D, collider, etc.

        [Header("Stats")]
        public int BaseHP = 20;
        public float MoveSpeed = 1.5f;
        public float Acceleration = 50f;
        public int ContactDamage = 5;
        public int XPValue = 1;

        [Header("Pooling")]
        [Min(0)] public int PrewarmCount = 8;

    }
}