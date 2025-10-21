using Survivor.Drop;
using System;
using UnityEngine;

namespace Survivor.Game
{
    [Serializable]
    public struct WeightedDrop
    {
        public DropItemDef Item;
        [Min(0f)] public float Weight;
    }

    [CreateAssetMenu(menuName = "Defs/Loot/Loot Table")]
    public sealed class LootTableDef : ScriptableObject
    {
        public WeightedDrop[] Entries;
    }
}
