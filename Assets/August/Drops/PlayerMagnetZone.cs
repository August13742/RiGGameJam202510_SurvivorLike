using UnityEngine;

namespace Survivor.Drop
{

    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class PlayerMagnetZone : MonoBehaviour
    {
        public float Radius
        {
            get { return radius; }
            set
            {
                value = Mathf.Max(0, value);
                radius = value;
                col.radius = radius;
            }

        }
        private float radius = 2f;
        public Transform Owner;
        private CircleCollider2D col;
        private void Awake()
        {
            col = GetComponent<CircleCollider2D>();
        }

        private void Reset() { Owner = transform.root; }
    }
}