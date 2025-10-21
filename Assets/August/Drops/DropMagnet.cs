using UnityEngine;

namespace Survivor.Drop
{
    [RequireComponent(typeof(DropItemBase))]
    public sealed class PickupMagnet : MonoBehaviour
    {
        [SerializeField] private float startDelay = 0.12f;  // small delay before attraction
        [SerializeField] private float speed = 12f;

        private DropItemBase _drop;
        private Transform _target; // player
        private float _t;
        private bool _magnetised;

        private void Awake()
        {
            _drop = GetComponent<DropItemBase>();
        }

        private void OnEnable()
        {
            _t = 0f;
            _magnetised = false;
            _target = null;
        }

        private void Update()
        {
            if (!_magnetised || !_target) return;

            _t += Time.deltaTime; if (_t < startDelay) return;

            Vector3 to = _target.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                to,
                speed * Time.deltaTime);

        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // If the pickup itself has the trigger, it will also see the magnet zone.
            // Detect the magnet zone, not the player body, here.
            var zone = other.GetComponent<PlayerMagnetZone>();
            if (!zone) return;

            _target = zone.Player ? zone.Player : other.transform.root;
            _magnetised = true;
        }
    }
}
