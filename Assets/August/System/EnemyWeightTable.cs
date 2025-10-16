using Survivor.Enemy;
using System;
using UnityEngine;


[Serializable]
public struct WeightedItem<T>
{
    public T Item;
    [Min(0f)] public float Weight;
}
namespace Survivor.Game { 
    [CreateAssetMenu(menuName = "Tables/EnemyWeightTable")]
    public sealed class EnemyWeightTable : ScriptableObject
    {
        public WeightedItem<EnemyDef>[] Entries;

        private float[] _prefix;
        private float _total;
        private bool _dirty = true;

        private void OnValidate() { _dirty = true; }

        private void Ensure()
        {
            if (!_dirty) return;
            if (Entries == null || Entries.Length == 0) { _prefix = Array.Empty<float>(); _total = 0f; _dirty = false; return; }

            _prefix = new float[Entries.Length];
            float acc = 0f;
            for (int i = 0; i < Entries.Length; i++)
            {
                float w = Mathf.Max(0f, Entries[i].Weight);
                acc += w;
                _prefix[i] = acc;
            }
            _total = acc;
            _dirty = false;
        }

        public bool TrySample(System.Random rng, out EnemyDef def)
        {
            Ensure();
            def = null;
            if (_total <= 0f || _prefix.Length == 0) return false;

            double r = rng.NextDouble() * _total;
            // binary search
            int lo = 0, hi = _prefix.Length - 1, idx = hi;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (r <= _prefix[mid]) { idx = mid; hi = mid - 1; }
                else lo = mid + 1;
            }
            def = Entries[idx].Item;
            return def != null;
        }
    }
}