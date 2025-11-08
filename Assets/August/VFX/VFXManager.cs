using UnityEngine;
using Survivor.Game;
namespace Survivor.VFX
{

    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private GameObject HitEffectPrefab; // prefab with DamageNumber + PrefabStamp + TMP
        [SerializeField] private int prewarm = 128;

        private Transform vfxPool;
        private ObjectPool _pool;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;


            Transform poolRoot = GameObject.FindWithTag("PoolRoot").transform;
            if (!poolRoot) { poolRoot = new GameObject("ObjectPools").transform; }

            vfxPool = new GameObject("VFXPool").transform;
            vfxPool.tag = "EnemyPool";
            vfxPool.parent = poolRoot;


            _pool = new ObjectPool(HitEffectPrefab, prewarm, vfxPool);
        }

        public void ShowHitEffect(Vector3 worldPos, bool crit = false)
        {
            GameObject effect = _pool.Rent(worldPos, Quaternion.identity);
            
        }
    }
}