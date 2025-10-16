using System.Collections.Generic;
using UnityEngine;

namespace Survivor.System
{

    public sealed class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int prewarm = 16;
        private readonly Queue<GameObject> q = new();

        private void Awake()
        {
            for (int i = 0; i < prewarm; i++) { var go = Instantiate(prefab, transform); go.SetActive(false); q.Enqueue(go); }
        }
        public GameObject Rent(Vector3 pos, Quaternion rot)
        {
            var go = q.Count > 0 ? q.Dequeue() : Instantiate(prefab, transform);
            go.transform.SetPositionAndRotation(pos, rot); go.SetActive(true); return go;
        }
        public void Return(GameObject go) { go.SetActive(false); q.Enqueue(go); }
    }
}