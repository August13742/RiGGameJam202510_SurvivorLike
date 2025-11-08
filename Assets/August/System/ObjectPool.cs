using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Game
{
    public sealed class ObjectPool<T> : IReturnToPool where T : Component
    {
        private readonly Transform _parent;
        private readonly T _prefab;
        private readonly Stack<T> _stack = new();

        public ObjectPool(T prefab, int prewarm, Transform parent)
        {
            _prefab = prefab; _parent = parent;
            for (int i = 0; i < prewarm; i++)
                _stack.Push(CreateInstance());
        }

        private T CreateInstance()
        {
            // Instantiate the GameObject from the component's GameObject
            var go = Object.Instantiate(_prefab.gameObject, _parent);

            // Get the component of type T from the new GameObject. for some reason if you don't do it like this it throws errors.
            var inst = go.GetComponent<T>();

            var stamp = go.GetComponent<PrefabStamp>() ?? go.AddComponent<PrefabStamp>();
            stamp.Prefab = _prefab.gameObject;
            stamp.OwnerPool = this;
            go.SetActive(false);
            return inst;
        }

        public T Rent(Vector3 pos, Quaternion rot, Transform reparentTo = null)
        {
            var inst = _stack.Count > 0 ? _stack.Pop() : CreateInstance();
            var tr = inst.transform;

            if (reparentTo && tr.parent != reparentTo)
                tr.SetParent(reparentTo, worldPositionStays: false);

            tr.SetPositionAndRotation(pos, rot);
            inst.gameObject.SetActive(true);

            if (inst.TryGetComponent<IPoolable>(out var ip)) ip.OnSpawned();
            return inst;
        }

        /*
        The idea is,  either attach `PrefabStamp` to the object and use stamp.OwnerPool.Return(), 
        
        or the component need to store Pool reference and call Pool.IReturnToPool.Return(this), 
        because I can't attach [RequireComponent(type(PrefabStamp)) on an interface] since it's not MonoBehaviour
         */
        public void Return(T inst)
        {
            if (!inst) return;
            if (inst.TryGetComponent<IPoolable>(out var ip)) ip.OnDespawned();

            var tr = inst.transform;
            if (tr.parent != _parent) tr.SetParent(_parent, worldPositionStays: false);

            inst.gameObject.SetActive(false);
            _stack.Push(inst);
        }
        // IReturnToPool hook (for objects that only know their GameObject)
        void IReturnToPool.Return(GameObject obj)
        {
            if (!obj) return;
            var inst = obj.GetComponent<T>();
            if (inst) Return(inst);
        }

    }

    public interface IReturnToPool
    {
        void Return(GameObject obj);
    }
}
