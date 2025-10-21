using UnityEngine;

namespace Survivor.Game
{
    [DisallowMultipleComponent]
    public sealed class PrefabStamp : MonoBehaviour
    {
        // Set by the pool at creation time; never changes.
        public GameObject Prefab { get; internal set; }
        public ObjectPool OwnerPool { get; internal set; }
    }
}
