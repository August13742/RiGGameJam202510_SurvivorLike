using UnityEngine;
using Survivor.Game;
namespace Survivor.VFX
{

    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private GameObject HitEffectPrefab; // prefab with DamageNumber + PrefabStamp + TMP
        [SerializeField] private int prewarm = 128;
        [SerializeField] private Transform poolRoot;

        private ObjectPool _pool;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (!poolRoot) poolRoot = new GameObject("VFXPool").transform;
            _pool = new ObjectPool(HitEffectPrefab, prewarm, poolRoot);
        }

        public void ShowHitEffect(Vector3 worldPos)
        {
            GameObject go = _pool.Rent(worldPos, Quaternion.identity);
        }
    }
}