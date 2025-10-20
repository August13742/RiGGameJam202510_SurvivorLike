using UnityEngine;
using Survivor.Game;
namespace Survivor.UI
{


    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [SerializeField] private GameObject textPrefab; // prefab with DamageNumber + PrefabStamp + TMP
        [SerializeField] private int prewarm = 96;
        [SerializeField] private Transform poolRoot;

        private ObjectPool _pool;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (!poolRoot) poolRoot = new GameObject("DamageNumbersPool").transform;
            _pool = new ObjectPool(textPrefab, prewarm, poolRoot);
        }

        public void ShowNormal(Vector3 worldPos, int amount)
        {
            GameObject go = _pool.Rent(worldPos, Quaternion.identity);
            DamageText text = go.GetComponent<DamageText>();
            text.ShowNormal(worldPos, amount); 
        }

        public void ShowCrit(Vector3 worldPos, int amount)
        {
            GameObject go = _pool.Rent(worldPos, Quaternion.identity);
            DamageText text = go.GetComponent<DamageText>();
            text.ShowCrit(worldPos, amount);
        }

        public void ShowHeal(Vector3 worldPos, int amount)
        {
            GameObject go = _pool.Rent(worldPos, Quaternion.identity);
            DamageText text = go.GetComponent<DamageText>();
            text.ShowHeal(worldPos, amount);
        }
    }
}