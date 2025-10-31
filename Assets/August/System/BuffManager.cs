using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Status
{
    public sealed class BuffManager : MonoBehaviour
    {
        private struct Buff
        {
            public float ExpiresAt;
            public float Magnitude;
        }

        private readonly Dictionary<string, List<Buff>> _stacks = new();

        public void AddTimedStack(string key, float magnitude, float duration)
        {
            if (!_stacks.TryGetValue(key, out var list)) { list = new(); _stacks[key] = list; }
            list.Add(new Buff { Magnitude = magnitude, ExpiresAt = Time.time + duration });
        }

        public float Sum(string key)
        {
            if (!_stacks.TryGetValue(key, out var list)) return 0f;
            float now = Time.time;
            float sum = 0f;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].ExpiresAt <= now) list.RemoveAt(i);
                else sum += list[i].Magnitude;
            }
            return sum;
        }
    }
}
