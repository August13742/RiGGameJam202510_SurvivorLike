using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Game
{
    public sealed class ObjectPool
    {
        private readonly Transform _parent;
        private readonly GameObject _prefab;
        private readonly Stack<GameObject> _stack = new();

        public ObjectPool(GameObject prefab, int prewarm, Transform parent)
        {
            _prefab = prefab; _parent = parent;
            for (int i = 0; i < prewarm; i++)
                _stack.Push(CreateInstance());
        }

        private GameObject CreateInstance()
        {
            GameObject obj = Object.Instantiate(_prefab, _parent);
            var stamp = obj.GetComponent<PrefabStamp>() ?? obj.AddComponent<PrefabStamp>();
            stamp.Prefab = _prefab;
            stamp.OwnerPool = this;
            obj.SetActive(false);
            return obj;
        }

        public GameObject Rent(Vector3 pos, Quaternion rot, Transform reparentTo = null)
        {
            var obj = _stack.Count > 0 ? _stack.Pop() : CreateInstance();
            if (reparentTo && obj.transform.parent != reparentTo)
                obj.transform.SetParent(reparentTo, worldPositionStays: false);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
            if (obj.TryGetComponent<IPoolable>(out var ip)) ip.OnSpawned();
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (!obj) return;
            if (obj.TryGetComponent<IPoolable>(out var ip)) ip.OnDespawned();
            // restore static parent to avoid dangling under dynamic hierarchies
            if (obj.transform.parent != _parent) obj.transform.SetParent(_parent, worldPositionStays: false);
            obj.SetActive(false);
            _stack.Push(obj);
        }

    }
}
