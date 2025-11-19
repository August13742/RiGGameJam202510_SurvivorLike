using System.Collections;
using UnityEngine;

namespace Survivor.Control
{
    public static class DirectionalDisplacementUtility
    {
        /// <summary>
        /// Applies a directional impulse.
        /// dir MUST be normalised.
        /// If useDistanceGate == true, influence only applies if player is within 'radius'.
        /// </summary>
        public static void ApplyDirectionalImpulse(
            PlayerController player,
            Vector2 dir,
            float maxTravel,
            bool useDistanceGate = false,
            float radius = 0f,
            Vector2? gateCenter = null)
        {
            if (player == null) return;
            if (dir.sqrMagnitude <= 0.0001f) return;

            Vector3 start = player.transform.position;

            // Optional gating based on distance
            if (useDistanceGate)
            {
                Vector2 ctr = gateCenter ?? (Vector2)start; // fallback shouldn't happen
                float dist = Vector2.Distance(start, ctr);
                if (dist > radius)
                    return;
            }

            // Bound the travel
            float travel = Mathf.Max(0f, maxTravel);
            if (travel <= 0f)
                return;

            Vector2 delta = dir.normalized * travel;
            player.AddExternalDisplacement(delta);
        }


        /// <summary>
        /// Repeated directional pulses over time (windy push, conveyor effect, sweeping wall).
        /// dir MUST be normalised.
        /// </summary>
        public static IEnumerator DirectionalPulseRoutine(
            PlayerController player,
            Vector2 dir,
            float stepPerPulse,
            float interval,
            float duration)
        {
            if (player == null) yield break;
            if (interval <= 0f || stepPerPulse <= 0f || duration <= 0f) yield break;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (player == null) yield break;

                ApplyDirectionalImpulse(
                    player,
                    dir,
                    stepPerPulse);

                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }
        }
    }
}
