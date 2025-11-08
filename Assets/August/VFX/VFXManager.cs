using UnityEngine;
using Survivor.Game;

namespace Survivor.VFX
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [SerializeField] private AutoExpandingVFXElement HitEffectPrefab;
        [SerializeField] private int prewarm = 128;

        private Transform _vfxPoolRoot;
        private ObjectPool<AutoExpandingVFXElement> _pool;

        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            GameObject rootGo = GameObject.FindWithTag("PoolRoot");
            Transform poolRoot = rootGo ? rootGo.transform : new GameObject("ObjectPools").transform;

            _vfxPoolRoot = new GameObject("VFXPool").transform;
            _vfxPoolRoot.SetParent(poolRoot, false);

            _pool = new ObjectPool<AutoExpandingVFXElement>(HitEffectPrefab, prewarm, _vfxPoolRoot);
        }

        public void ShowHitEffect(Vector3 worldPos, bool crit = false)
        {
            var fx = _pool.Rent(worldPos, Quaternion.identity);
            fx.Init(crit);
        }

    }
}
