using UnityEngine;
using Survivor.Game;
namespace Survivor.Drop
{ 
    [RequireComponent(typeof(Collider2D), typeof(PrefabStamp))]
    [DisallowMultipleComponent]
    public abstract class DropItemBase : MonoBehaviour, IDrop, IPoolable
    {
        [SerializeField, Min(1)] public int amount = 1;
        protected PrefabStamp _stamp;
        protected Collider2D _collider;
        protected bool _picked = false;
        public System.Action<DropItemBase> Despawned;

        private void Awake()
        {
            _stamp = GetComponent<PrefabStamp>();
            _collider = GetComponent<Collider2D>();
            if (_collider) _collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other || !other.CompareTag("Player")) return;
            OnPickup(other.gameObject);
        }

        public void OnPickup(GameObject player)
        {
            if (_picked) return;
            _picked = true;
            if (_collider) _collider.enabled = false;
            Apply(player);
            Despawn();
        }

        public virtual void OnSpawned()
        {
            _picked = false;
            if(_collider) _collider.enabled = true;
        }

        public virtual void OnDespawned()
        {
            // Clear vfx/sfx/trails here if needed

        }

        protected abstract void Apply(GameObject player);

        protected void Despawn()
        {
            Despawned?.Invoke(this);
            if (_stamp && _stamp.OwnerPool != null)
                _stamp.OwnerPool.Return(gameObject);
            else
                gameObject.SetActive(false);
        }


    }
}