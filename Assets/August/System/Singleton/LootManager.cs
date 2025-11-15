using Survivor.Drop;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Game
{
    public sealed class LootManager : MonoBehaviour
    {
        public static LootManager Instance { get; private set; }

        [Header("Optional preload (tables to prewarm)")]
        [SerializeField] private LootTableDef[] preloadTables;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadiusMin = 0.2f;
        [SerializeField] private float spawnRadiusMax = 0.8f;
        
        private Transform poolRoot;

        private readonly Dictionary<DropItemDef, ObjectPool<DropItemBase>> _pools = new();
        private System.Random _rng;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            GameObject pools = GameObject.FindWithTag("PoolRoot");
            if (!pools)
            {
                pools = new GameObject("ObjectPools");
            }

            poolRoot = new GameObject("LootPool").transform;
            poolRoot.tag = "LootPool";
            poolRoot.parent = pools.transform;


            _rng = new System.Random(13742);
            PrewarmFromTables(preloadTables);
        }

        private void PrewarmFromTables(LootTableDef[] tables)
        {
            if (tables == null) return;
            foreach (var t in tables)
            {
                if (!t || t.Entries == null) continue;
                foreach (var e in t.Entries)
                {
                    if (!e.Item || !e.Item.Prefab) continue;
                    GetOrCreatePool(e.Item); // creates & prewarms
                }
            }
        }

        private ObjectPool<DropItemBase> GetOrCreatePool(DropItemDef def)
        {
            if (_pools.TryGetValue(def, out var p)) return p;
            ObjectPool<DropItemBase> pool = new (def.Prefab, Mathf.Max(0, def.PrewarmCount), poolRoot);
            _pools.Add(def, pool);
            return pool;
        }

        public void SpawnLoot(Enemy.EnemyDef enemyDef, Vector2 at)
        {
            if (!enemyDef || enemyDef.LootTable == null || enemyDef.LootTable.Entries == null || enemyDef.LootTable.Entries.Length == 0)
                return;

            // Roll global chance
            if (_rng.NextDouble() > enemyDef.DropChance) return;

            for (int r = 0; r < Mathf.Max(1, enemyDef.Rolls); r++)
            {
                var def = Sample(enemyDef.LootTable);
                if (def == null || def.Prefab == null) continue;

                var pool = GetOrCreatePool(def);
                
                // Apply random offset within a circular radius
                Vector2 spawnPosition = at + GetRandomOffset();
                var go = pool.Rent(spawnPosition, Quaternion.identity);

                // Configure amount (stack) per spawn
                var baseItem = go.GetComponent<DropItemBase>();
                if (baseItem)
                {
                    baseItem.amount = (def.MinAmount == def.MaxAmount)
                        ? def.MinAmount
                        : Random.Range(def.MinAmount, def.MaxAmount + 1);
                }
            }
        }

        private Vector2 GetRandomOffset()
        {
            // Generate random angle
            float angle = (float)(_rng.NextDouble() * 2.0 * Mathf.PI);
            
            // Generate random radius with square root for uniform distribution
            float radius = Mathf.Lerp(spawnRadiusMin, spawnRadiusMax, Mathf.Sqrt((float)_rng.NextDouble()));
            
            // Convert polar coordinates to Cartesian
            return new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
        }

        private DropItemDef Sample(LootTableDef table)
        {
            // prefix-sum sample (one-shot)
            float total = 0f;
            for (int i = 0; i < table.Entries.Length; i++)
                total += Mathf.Max(0f, table.Entries[i].Weight);
            if (total <= 0f) return null;

            double roll = _rng.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < table.Entries.Length; i++)
            {
                float w = Mathf.Max(0f, table.Entries[i].Weight);
                acc += w;
                if (roll <= acc) return table.Entries[i].Item;
            }
            return table.Entries[^1].Item; // fallback
        }
    }
}
