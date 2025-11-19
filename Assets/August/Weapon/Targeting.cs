using Survivor.Game;
using System.Collections.Generic;
using UnityEngine;

public static class Targeting
{
    // Shared buffers (single-threaded gameplay assumption)
    private static readonly List<Collider2D> _overlap = new(32);
    private static readonly List<Transform> _valid = new(32);
    private static readonly HashSet<HealthComponent> _seenHealthComponents = new(32);
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
            var col = _overlap[i];
            if (!col) continue;
            HealthComponent target;
            target = col.GetComponent<HealthComponent>();
            if (target == null) target = col.GetComponentInParent<HealthComponent>();
            if (target == null) continue;
            if (target.IsDead) continue;

            _valid.Add(target.transform);
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
        _seenHealthComponents.Clear(); // <-- Clear the static set

        int hitCount = Physics2D.OverlapCircle((Vector2)origin.position, radius, filter, _overlap);
        if (hitCount <= 0) return null;


        for (int i = 0; i < hitCount; i++)
        {
            var col = _overlap[i];
            if (!col) continue;
            HealthComponent target;
            target = col.GetComponent<HealthComponent>();
            if (target == null) target = col.GetComponentInParent<HealthComponent>();
            if (target == null) return null;
            if (target.IsDead) return null;

            if (_seenHealthComponents.Add(target))
                _valid.Add(target.transform);

        }
        if (_valid.Count == 0) return null;

        // Pick one random target from the unique list
        int randomIndex = Random.Range(0, _valid.Count);
        return _valid[randomIndex];
    }
}
