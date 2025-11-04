using UnityEngine;
using System.Collections.Generic;

namespace Survivor.Weapon
{
    public sealed class WeaponController : MonoBehaviour
    {
        //[SerializeField] private int maxSlots = 4;
        [SerializeField] private Transform fireOrigin;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private float searchRadius = 12f;

        private WeaponContext _ctx;
        private readonly List<IWeapon> _weapons = new(1);
        private ContactFilter2D _enemyFilter;
        private bool _initialized = false;

        public bool HasEmptySlot => _weapons.Count == 0;
        public int WeaponCount => _weapons.Count;

        private void Awake()
        {
            _enemyFilter = new ContactFilter2D { useTriggers = true, useDepth = false };
            _enemyFilter.SetLayerMask(enemyMask);
        }

        // NEW: explicit initializer so drones (or player) can inject context.
        public void InitialiseFromHost(Transform owner, Transform fire, Transform pool, LayerMask mask, float radius, Team team = Team.Player)
        {
            // capture
            fireOrigin = fire;
            poolRoot = pool;
            enemyMask = mask;
            searchRadius = radius;

            _enemyFilter = new ContactFilter2D { useTriggers = true, useDepth = false };
            _enemyFilter.SetLayerMask(enemyMask);

            // lazily create a pool root if not provided
            if (!poolRoot)
                poolRoot = GameObject.FindWithTag("PoolRoot")?.transform ?? new GameObject("Pools").transform;

            // targeting closures bound to THIS host
            System.Func<Transform> nearest = () => Targeting.NearestEnemy(fireOrigin, searchRadius, _enemyFilter);
            System.Func<int, Transform> randK = (k) => Targeting.RandomK(k, fireOrigin, searchRadius, _enemyFilter);
            System.Func<Transform> selfCent = () => Targeting.SelfCentered(fireOrigin);

            _ctx = new WeaponContext
            {
                Team = team,
                FireOrigin = fireOrigin,
                Owner = owner,     // <-- host = drone transform (or player)
                PoolRoot = poolRoot,
                Nearest = nearest,
                RandomInRange = randK,
                SelfCentered = selfCent
            };

            // Equip any weapons already present as child components
            var existingWeapon = GetComponentInChildren<IWeapon>(includeInactive: false);
            _weapons.Clear();
            if(existingWeapon!= null)
            {
                _weapons.Add(existingWeapon);
                existingWeapon.Equip(_ctx);
            }
                
            _initialized = true;
        }

        private void Start()
        {
            // Back-compat path: if not explicitly initialized (e.g., player-mounted use),
            // do the old Start() setup using serialized fields.
            if (_initialized) return;

            if (!poolRoot)
                poolRoot = GameObject.FindWithTag("PoolRoot")?.transform ?? new GameObject("Pools").transform;

            System.Func<Transform> nearest = () => Targeting.NearestEnemy(fireOrigin, searchRadius, _enemyFilter);
            System.Func<int, Transform> randK = (k) => Targeting.RandomK(k, fireOrigin, searchRadius, _enemyFilter);
            System.Func<Transform> selfCent = () => Targeting.SelfCentered(fireOrigin);

            _ctx = new WeaponContext
            {
                Team = Team.Player,
                FireOrigin = fireOrigin,
                Owner = transform,
                PoolRoot = poolRoot,
                Nearest = nearest,
                RandomInRange = randK,
                SelfCentered = selfCent
            };

            var existingWeapon = GetComponentInChildren<IWeapon>(includeInactive: false);
            _weapons.Clear();
            if (existingWeapon != null)
            {
                _weapons.Add(existingWeapon);
                existingWeapon.Equip(_ctx);
            }
            _initialized = true;
        }

        private void FixedUpdate()
        {
            if (!_initialized) return;
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _weapons.Count; i++)
                _weapons[i].Tick(dt);
        }

        public bool HasWeapon(WeaponDef def)
        {
            if (!def) return false;
            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] is IUpgradeableWeapon upgradeable && upgradeable.Owns(def))
                    return true;
            }
            return false;
        }

        public bool TryEquip(WeaponDef def)
        {
            if (!HasEmptySlot || !def) return false;

            IWeapon weapon = InstantiateWeapon(def);
            if (weapon == null)
            {
                return false;
            }

            _weapons.Add(weapon);
            weapon.Equip(_ctx); // <-- uses injected context
            return true;
        }

        public bool ApplyUpgrade(WeaponDef def, WeaponStats delta)
        {
            if (!def || delta == null) return false;
            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] is IUpgradeableWeapon upgradeable && upgradeable.Owns(def))
                {
                    upgradeable.ApplyUpgrade(delta);
                    return true;
                }
            }
            return false;
        }

        public WeaponStats GetWeaponStats(WeaponDef def)
        {
            if (!def) return null;
            for (int i = 0; i < _weapons.Count; i++)
            {
                if (_weapons[i] is IUpgradeableWeapon upgradeable && upgradeable.Owns(def))
                    return upgradeable.GetCurrentStats();
            }
            return null;
        }

        private IWeapon InstantiateWeapon(WeaponDef def)
        {
            if (!def.RuntimePrefab)
            {
                Debug.LogWarning($"[{def.name}] has no RuntimePrefab assigned.");
                return null;
            }

            // Instantiate the prefab directly as a child of THIS transform (the WeaponController's transform).
            GameObject go = Instantiate(def.RuntimePrefab, transform);
            go.name = $"Weapon_{def.Id}";

            if (!go.TryGetComponent<IWeapon>(out var weapon))
            {
                Debug.LogError($"[{def.name}] prefab lacks an IWeapon component.");
                Destroy(go);
                return null;
            }
            return weapon;
        }
    }
}
