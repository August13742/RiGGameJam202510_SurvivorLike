using UnityEngine;

namespace Survivor.Drop
{

    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerMagnetZone : MonoBehaviour
    {
        public Transform Player;  // assign to root player transform in inspector
        private void Reset() { Player = transform.root; }
    }
}