using UnityEngine;
using Survivor.Game;
namespace Survivor.UI
{


    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [SerializeField] private float horizontalJitterRange = 1f;
        [SerializeField] private float verticalJitterRange = 1f;
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
        private Vector3 ApplyHorizontalJitter(Vector3 pos)
        {
            return new Vector3(pos.x + Random.Range(-horizontalJitterRange, horizontalJitterRange), 
                pos.y + Random.Range(0, verticalJitterRange), pos.z);
        }
        public void ShowNormal(Vector3 worldPos, float amount)
        {
            Vector3 jitteredPos = ApplyHorizontalJitter(worldPos);
            DamageText text = _pool.Rent(jitteredPos, Quaternion.identity);
            text.ShowNormal(jitteredPos, amount);
        }

        public void ShowCrit(Vector3 worldPos, float amount)
        {
            Vector3 jitteredPos = ApplyHorizontalJitter(worldPos);
            DamageText text = _pool.Rent(jitteredPos, Quaternion.identity);
            text.ShowCrit(jitteredPos, amount);
        }

        public void ShowHeal(Vector3 worldPos, float amount)
        {
            Vector3 jitteredPos = ApplyHorizontalJitter(worldPos);
            DamageText text = _pool.Rent(jitteredPos, Quaternion.identity);
            text.ShowHeal(jitteredPos, amount);
        }
    }
}