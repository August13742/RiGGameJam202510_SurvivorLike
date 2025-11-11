using UnityEngine;

namespace Survivor.Drop
{
    [CreateAssetMenu(menuName = "Defs/Loot/Drop Item")]
    public sealed class DropItemDef : ScriptableObject
    {
        [Header("Prefab (must have DropItemBase + PrefabStamp)")]
        public DropItemBase Prefab;

        [Header("Amount (stack)")]
        public int MinAmount = 1;
        public int MaxAmount = 1;

        [Header("Pooling")]
        [Min(0)] public int PrewarmCount = 8;
    }
}