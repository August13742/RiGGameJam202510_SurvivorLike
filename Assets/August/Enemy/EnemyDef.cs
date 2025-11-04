using Survivor.Game;
using UnityEngine;

namespace Survivor.Enemy
{
    public abstract class EnemyDef : ScriptableObject
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

        [Header("Drops")]
        [Range(0f, 1f)] public float DropChance = 0.6f;  // chance that this enemy drops anything
        public int Rolls = 1;                            // how many items to roll if it drops
        public LootTableDef LootTable;                   // weighted item selection

    }
}