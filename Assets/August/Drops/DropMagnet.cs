using System;
using UnityEngine;

namespace Survivor.Drop
{
    [RequireComponent(typeof(DropItemBase))]
    [DisallowMultipleComponent]
    public sealed class PickupMagnet : MonoBehaviour
    {

        [SerializeField] private float tweenDuration = 1f;

        private DropItemBase _drop;
        private Transform _target; // player
        public Action Triggered;
        private void Awake()
        {
            _drop = GetComponent<DropItemBase>();
        }

        private void OnEnable()
        {
            _target = null;
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            // If the pickup itself has the trigger, it will also see the magnet zone.
            // Detect the magnet zone, not the player body, here.
            var zone = other.GetComponent<PlayerMagnetZone>();
            if (!zone) return;

            Triggered.Invoke();
            _target = zone.Owner ? zone.Owner : other.transform.root;
            transform.TweenPosition(
                target: _target,
                duration: tweenDuration,
                ease: EasingFunctions.EaseInOutBack);


        }
    }
}
