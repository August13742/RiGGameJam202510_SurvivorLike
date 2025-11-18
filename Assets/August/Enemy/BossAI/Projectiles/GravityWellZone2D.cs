using Survivor.Control;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace Survivor.Weapon
{
    [DisallowMultipleComponent]
    public sealed class GravityWellHazard2D : HazardZone2D
    {
        [Header("Gravity Well")]
        [Tooltip("Max radius in which the pull is applied.")]
        [SerializeField] private float pullRadius = 6f;

        [Tooltip("Max pull speed at the very edge of the radius (world units / second).")]
        [SerializeField] private float edgePullSpeed = 6f;

        [Tooltip("If true, use hitMask from base as pull mask when pullMask == 0.")]
        [SerializeField] private bool useDamageMaskForPull = true;

        [SerializeField] private LayerMask pullMask;

        // Optional: tweak how pull falls off with distance (0 = center, 1 = edge)
        [SerializeField] private AnimationCurve falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        private static readonly Collider2D[] _pullHits = new Collider2D[8];

        protected override void FixedUpdate()
        {
            // 1) normal hazard behaviour: lifetime + DPS
            base.FixedUpdate();

            // If we got destroyed in base.FixedUpdate(), don't try to pull
            if (this == null) return;

            // 2) gravity pull
            DoPull(Time.fixedDeltaTime);
        }

        private void DoPull(float dt)
        {
            if (pullRadius <= 0f || edgePullSpeed <= 0f) return;

            LayerMask maskToUse = pullMask;
            if (maskToUse == 0 && useDamageMaskForPull)
            {
                // reuse whatever you assigned in HazardZone2D.hitMask in inspector
                // this works because we inherit that serialized field
                maskToUse = GetHitMask();
            }

            ContactFilter2D filter = new()
            {
                useTriggers = true,
                useDepth = false
            };
            filter.SetLayerMask(maskToUse);

            int count = Physics2D.OverlapCircle(transform.position, pullRadius, filter, _pullHits);
            if (count == 0) return;

            Vector2 center = transform.position;

            for (int i = 0; i < count; i++)
            {
                Collider2D col = _pullHits[i];
                if (col == null) continue;

                // We only care about the player (or anything with PlayerController)
                if (!col.TryGetComponent<PlayerController>(out var pc)) continue;

                Vector2 pos = col.transform.position;
                Vector2 toCenter = center - pos;
                float dist = toCenter.magnitude;
                if (dist < 0.01f) continue;

                float t = Mathf.Clamp01(dist / pullRadius); // 0 near center, 1 at edge
                float strengthFactor = falloff.Evaluate(1f - t); // stronger near center
                float speed = edgePullSpeed * strengthFactor;

                if (speed <= 0f) continue;

                Vector2 delta = toCenter.normalized * speed * dt;
                pc.AddExternalDisplacement(delta);
            }
        }

        // Small helper to read the base's private serialized hitMask via inspector:
        // Easiest solution is to change HazardZone2D.hitMask to 'protected' and expose this:
        private LayerMask GetHitMask()
        {
            // AFTER you change 'hitMask' in HazardZone2D from 'private' to 'protected':
            return hitMask;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, pullRadius);
        }
    }
}
