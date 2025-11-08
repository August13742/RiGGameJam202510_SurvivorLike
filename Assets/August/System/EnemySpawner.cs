using Survivor.Enemy;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Game
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        public bool Enabled = true;
        [Header("Sources")]
        [SerializeField] private EnemyWeightTable weightTable;
        [SerializeField] private Transform player;

        [Header("Spawn Geometry")]
        [SerializeField] private float ringRadiusMin = 12f;
        [SerializeField] private float ringRadiusMax = 14f;
        [SerializeField] private float ringJitterDeg = 8f;

        [Header("Rates & Caps")]
        [SerializeField] private float spawnsPerSecond = 3f;
        [SerializeField] private int maxAlive = 150;


        private Transform poolRoot;
        private Transform enemyPool;

        private readonly Dictionary<GameObject, ObjectPool> _pools = new();
        private float _spawnAcc;
        private int _aliveCount;
        private System.Random _rng;

        private void Awake()
        {
            poolRoot = GameObject.FindWithTag("PoolRoot").transform;
            if (!poolRoot) { poolRoot = new GameObject("ObjectPools").transform; }

            enemyPool = new GameObject("EnemyPool").transform;
            enemyPool.tag = "EnemyPool";
            enemyPool.parent = poolRoot;
                
            _rng = new System.Random(13742);
        }

        private void Update()
        {
            if (!Enabled) return;
            if (!player || !weightTable) return;

            _spawnAcc += spawnsPerSecond * Time.deltaTime;
            while (_spawnAcc >= 1f && _aliveCount < maxAlive)
            {
                _spawnAcc -= 1f;
                if (!weightTable.TrySample(_rng, out var def) || !def || !def.Prefab) continue;

                var pool = GetOrCreatePool(def.Prefab, def.PrewarmCount);
                Vector3 pos = SampleRingPosition(player.position);
                GameObject go = pool.Rent(pos, Quaternion.identity);

                var enemy = go.GetComponent<EnemyBase>();
                if (!enemy) { Debug.LogError($"Enemy prefab missing EnemyBase: {def.name}"); pool.Return(go); continue; }

                // init + subscribe
                enemy.InitFrom(def);
                enemy.Despawned -= OnEnemyDespawned; // safety against double-sub
                enemy.Despawned += OnEnemyDespawned;

                _aliveCount++;
            }
        }

        private void OnEnemyDespawned(EnemyBase e)
        {
            // unsubscribe to avoid handler buildup over many runs
            e.Despawned -= OnEnemyDespawned;
            _aliveCount = Mathf.Max(0, _aliveCount - 1);
        }

        private ObjectPool GetOrCreatePool(GameObject prefab, int prewarm)
        {
            if (_pools.TryGetValue(prefab, out var p)) return p;
            var pool = new ObjectPool(prefab, Mathf.Max(0, prewarm), enemyPool);
            _pools.Add(prefab, pool);
            return pool;
        }

        private Vector3 SampleRingPosition(Vector3 around)
        {
            float r = Mathf.Lerp(ringRadiusMin, ringRadiusMax, (float)_rng.NextDouble());
            float ang = (float)(_rng.NextDouble() * Mathf.PI * 2.0);
            float jitter = Mathf.Deg2Rad * ringJitterDeg * (float)(_rng.NextDouble() * 2.0 - 1.0);
            ang += jitter;
            return new Vector3(around.x + Mathf.Cos(ang) * r, around.y + Mathf.Sin(ang) * r, 0f);
        }

        public void SetSpawnRate(float sps) => spawnsPerSecond = Mathf.Max(0f, sps);
        public void SetMaxAlive(int max) => maxAlive = Mathf.Max(0, max);
        public int AliveCount => _aliveCount;
    }
}
