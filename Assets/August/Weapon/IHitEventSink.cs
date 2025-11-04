using UnityEngine;

namespace Survivor.Weapon { 
    public interface IHitEventSink { void OnHit(float damage, Vector2 pos, bool crit); void OnKill(Vector2 pos); }
}