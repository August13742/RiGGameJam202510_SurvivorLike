using UnityEngine;
using Survivor.Game;
namespace Survivor.Drop
{ 
    [RequireComponent(typeof(Collider2D), typeof(PrefabStamp))]
    [RequireComponent(typeof(PickupMagnet),typeof(SpriteOutline))]
    [DisallowMultipleComponent]
    public abstract class DropItemBase : MonoBehaviour, IDrop, IPoolable
    {
        [SerializeField] SFXResource pickupSFX;
        [Min(1)] public int amount = 1;
        protected PrefabStamp _stamp;
        protected Collider2D _collider;
        protected SpriteOutline _outlineDriver;
        protected PickupMagnet _magnet;
        protected bool _picked = false;
        public System.Action<DropItemBase> Despawned;

        private void Awake()
        {
            _stamp = GetComponent<PrefabStamp>();
            _collider = GetComponent<Collider2D>();
            _outlineDriver = GetComponent<SpriteOutline>();
            _magnet = GetComponent<PickupMagnet>();
            
            if (_collider) _collider.isTrigger = true;
        }
        private void Start()
        {
            _magnet.Triggered += OnMagnetTriggered;
        }
        private void OnMagnetTriggered()
        {
            _outlineDriver.enabled = false;
        }
        private void OnDestroy()
        {
            _magnet.Triggered -= OnMagnetTriggered;
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
            AudioManager.Instance?.PlaySFX(pickupSFX, this.transform.position, transform);
            Apply(player);
            Despawn();
        }

        public virtual void OnSpawned()
        {
            _picked = false;
            _collider.enabled = true;
            _outlineDriver.enabled = true;
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