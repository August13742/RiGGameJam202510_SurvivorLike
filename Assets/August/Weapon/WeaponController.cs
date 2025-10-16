using UnityEngine;

namespace Survivor.Weapon
{
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private Transform fireOrigin;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private float searchRadius = 12f;

        private WeaponContext _ctx;
        private IWeapon[] _weapons;
        private ContactFilter2D _enemyFilter;

        private void Awake()
        {
            _enemyFilter = new ContactFilter2D { useTriggers = true, useDepth = false };
            _enemyFilter.SetLayerMask(enemyMask);
        }

        private void Start()
        {
            if (!poolRoot)
                poolRoot = GameObject.FindWithTag("PoolRoot")?.transform ?? new GameObject("Pools").transform;

            System.Func<Transform> nearest = () => Targeting.NearestEnemy(fireOrigin, searchRadius, _enemyFilter);
            System.Func<int, Transform> randK = (k) => Targeting.RandomK(k, fireOrigin, searchRadius, _enemyFilter);

            _ctx = new WeaponContext
            {
                FireOrigin = fireOrigin,
                Owner = transform,
                PoolRoot = poolRoot,
                Stats = new WeaponStats(),
                Nearest = nearest,
                RandomInRange = randK,

            };

            _weapons = GetComponentsInChildren<IWeapon>(includeInactive: false);
            for (int i = 0; i < _weapons.Length; i++)
                _weapons[i].Equip(_ctx);
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _weapons.Length; i++)
                _weapons[i].Tick(Time.fixedDeltaTime);
        }
    }
}