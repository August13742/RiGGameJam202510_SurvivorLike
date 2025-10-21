using Survivor.Game;
using System.Collections.Generic;
using UnityEngine;

public static class Targeting
{
    // Shared buffers (single-threaded gameplay assumption)
    private static readonly List<Collider2D> _overlap = new(32);
    private static readonly List<Transform> _valid = new(32);

    public static Transform SelfCentered(Transform origin) {  return origin; }
    public static Transform NearestEnemy(Transform origin, float radius, ContactFilter2D filter)
    {
        if (!origin || radius <= 0f) return null;

        _overlap.Clear();
        _valid.Clear();

        int hitCount = Physics2D.OverlapCircle((Vector2)origin.position, radius, filter, _overlap);
        if (hitCount <= 0) return null;

        for (int i = 0; i < hitCount; i++)
        {
            var c = _overlap[i];
            if (!c) continue;
            if (c && c.GetComponentInParent<HealthComponent>() is { } hp && !hp.IsDead)
                _valid.Add(hp.transform);
        }
        if (_valid.Count == 0) return null;

        float best = float.PositiveInfinity;
        Transform bestT = null;
        for (int i = 0; i < _valid.Count; i++)
        {
            var t = _valid[i];
            float d = (t.position - origin.position).sqrMagnitude;
            if (d < best) { best = d; bestT = t; }
        }
        return bestT;
    }

    public static Transform RandomK(int amount, Transform origin, float radius, ContactFilter2D filter)
    {
        if (!origin || radius <= 0f || amount <= 0) return null;

        _overlap.Clear();
        _valid.Clear();

        int hitCount = Physics2D.OverlapCircle((Vector2)origin.position, radius, filter, _overlap);
        if (hitCount <= 0) return null;

        for (int i = 0; i < hitCount; i++)
        {
            var c = _overlap[i];
            if (!c) continue;
            if (c.TryGetComponent<HealthComponent>(out var hp) && !hp.IsDead)
                _valid.Add(c.transform);
        }
        if (_valid.Count == 0) return null;

        int k = Mathf.Min(amount, _valid.Count);
        // Partial Fisher–Yates on first k
        for (int i = 0; i < k; i++)
        {
            int j = Random.Range(i, _valid.Count);
            (_valid[i], _valid[j]) = (_valid[j], _valid[i]);
        }
        return _valid[Random.Range(0, k)];
    }
}
