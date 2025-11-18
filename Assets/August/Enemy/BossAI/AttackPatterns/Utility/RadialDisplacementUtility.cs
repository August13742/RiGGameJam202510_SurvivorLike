using System.Collections;
using UnityEngine;

namespace Survivor.Control
{
    public static class RadialDisplacementUtility
    {
        /// <summary>
        /// One-shot radial impulse.
        /// If pull == true  → move player towards source (gravity).
        /// If pull == false → move player away from source (repulse).
        /// Travel is clamped by radius and maxTravel, and never overshoots the source point.
        /// </summary>
        public static void ApplyRadialImpulse(
            PlayerController player,
            Vector2 sourceWorldPos,
            float radius,
            float maxTravel,
            bool pull)
        {
            if (player == null) return;

            Transform playerTf = player.transform;
            Vector3 start = playerTf.position;

            // Direction depends on pull vs push
            Vector3 vec = pull
                ? (Vector3)sourceWorldPos - start   // toward source
                : start - (Vector3)sourceWorldPos; // away from source

            float dist = vec.magnitude;

            // Too close or outside influence radius → no effect
            if (dist <= 0.05f || dist > radius)
                return;

            // Bound travel and don't overshoot center
            float travel = Mathf.Min(maxTravel, dist * 0.9f);
            if (travel <= 0f)
                return;

            Vector2 delta = (Vector2)(vec.normalized * travel);
            player.AddExternalDisplacement(delta);
        }

        /// <summary>
        /// Repeated radial pulses over time (gravity well style).
        /// Duration is approximate: it stops once elapsed >= duration.
        /// </summary>
        public static IEnumerator RadialPulseRoutine(
            PlayerController player,
            Transform source,
            float radius,
            float stepPerPulse,
            float interval,
            float duration,
            bool pull)
        {
            if (player == null || source == null) yield break;
            if (interval <= 0f || stepPerPulse <= 0f || duration <= 0f) yield break;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (player == null || source == null) yield break;

                ApplyRadialImpulse(
                    player,
                    source.position,
                    radius,
                    stepPerPulse,
                    pull);

                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }
        }
    }
}
