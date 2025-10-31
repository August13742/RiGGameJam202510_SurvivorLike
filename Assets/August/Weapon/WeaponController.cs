using UnityEngine;
using System.Collections.Generic;

namespace Survivor.Weapon
{
    public sealed class WeaponController : MonoBehaviour
    {

        [SerializeField] private int maxSlots = 4;
        [SerializeField] private Transform fireOrigin;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private LayerMask enemyMask = ~0;
        [SerializeField] private float searchRadius = 12f;

        private WeaponContext _ctx;
        private readonly List<IWeapon> _weapons = new List<IWeapon>();
        private ContactFilter2D _enemyFilter;

        public bool HasEmptySlot => _weapons.Count < maxSlots;
        public int WeaponCount => _weapons.Count;

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
            System.Func<Transform> selfCentered = () => Targeting.SelfCentered(fireOrigin);

            _ctx = new WeaponContext
            {
                Team = Team.Player,
                FireOrigin = fireOrigin,
                Owner = transform,
                PoolRoot = poolRoot,
                Nearest = nearest,
                RandomInRange = randK,
                SelfCentered = selfCentered
            };

            // Equip any weapons already present as child components
            var existingWeapons = GetComponentsInChildren<IWeapon>(includeInactive: false);
            for (int i = 0; i < existingWeapons.Length && i < maxSlots; i++)
            {
                _weapons.Add(existingWeapons[i]);
                existingWeapons[i].Equip(_ctx);
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _weapons.Count; i++)
                _weapons[i].Tick(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Checks if a weapon of the specified type is already equipped.
        /// </summary>
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

        /// <summary>
        /// Attempts to equip a new weapon from a definition.
        /// Returns true if successful, false if no slots available.
        /// </summary>
        public bool TryEquip(WeaponDef def)
        {
            if (!HasEmptySlot || !def) return false;

            // Create weapon instance as child GameObject
            GameObject weaponGO = new GameObject($"Weapon_{def.Id}");
            weaponGO.transform.SetParent(transform);
            weaponGO.transform.localPosition = Vector3.zero;

            IWeapon weapon = InstantiateWeapon(def, weaponGO);
            if (weapon == null)
            {
                Destroy(weaponGO);
                return false;
            }

            _weapons.Add(weapon);
            weapon.Equip(_ctx);
            return true;
        }

        /// <summary>
        /// Applies stat modifiers to a specific weapon (for leveling up individual weapons).
        /// </summary>
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

        /// <summary>
        /// Gets the current stats for a specific weapon.
        /// </summary>
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

        private IWeapon InstantiateWeapon(WeaponDef def, GameObject host)
        {
            if (!def.RuntimePrefab)
            {
                Debug.LogWarning($"[{def.name}] has no RuntimePrefab assigned.");
                return null;
            }

            var go = Instantiate(def.RuntimePrefab, host.transform);
            go.name = $"Weapon_{def.Id}";
            var weapon = go.GetComponent<IWeapon>();
            if (weapon == null)
            {
                Debug.LogError($"[{def.name}] prefab lacks an IWeapon component.");
                Destroy(go);
                return null;
            }
            return weapon;
        }
    }


}