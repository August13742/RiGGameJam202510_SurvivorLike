using UnityEngine;
using Survivor.Game;
namespace Survivor.UI
{


    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [SerializeField] private DamageText textPrefab; // prefab with DamageNumber + PrefabStamp + TMP
        [SerializeField] private int prewarm = 128;
        private Transform poolRoot;

        private ObjectPool<DamageText> _pool;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            GameObject pools = GameObject.FindWithTag("PoolRoot");
            if (!pools)
            {
                pools = new GameObject("ObjectPools");
            }

            poolRoot = new GameObject("DamageNumbersPool").transform;
            poolRoot.parent = pools.transform;


            _pool = new ObjectPool<DamageText>(textPrefab, prewarm, poolRoot);
        }

        public void ShowNormal(Vector3 worldPos, float amount)
        {
            DamageText text = _pool.Rent(worldPos, Quaternion.identity);
            text.ShowNormal(worldPos, amount); 
        }

        public void ShowCrit(Vector3 worldPos, float amount)
        {
            DamageText text = _pool.Rent(worldPos, Quaternion.identity);
            text.ShowCrit(worldPos, amount);
        }

        public void ShowHeal(Vector3 worldPos, float amount)
        {
            DamageText text = _pool.Rent(worldPos, Quaternion.identity);
            text.ShowHeal(worldPos, amount);
        }
    }
}