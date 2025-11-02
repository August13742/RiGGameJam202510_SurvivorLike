using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class KinematicMotor2D : MonoBehaviour
{
    // doing everything it needs to avoid dynamic rb. Unity NO

    public LayerMask collisionMask;   // e.g., WorldBound | WorldProps
    public float skin = 0.05f;        // small padding to avoid grazing overlaps
    public int maxCasts = 2;          // 1 = hard stop, 2 = one slide attempt

    private Rigidbody2D rb;
    private Collider2D col;
    private ContactFilter2D filter;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;

        filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = collisionMask,
            useTriggers = false
        };
    }

    public void Move(Vector2 delta)
    {
        Vector2 pos = rb.position;
        Vector2 remaining = delta;

        for (int pass = 0; pass < maxCasts; pass++)
        {
            if (remaining.sqrMagnitude <= 1e-8f) break;

            Vector2 dir = remaining.normalized;
            float dist = remaining.magnitude;

            // Cast the *current collider shape* along remaining displacement
            int count = rb.Cast(dir, filter, hits, dist + skin);
            float allowed = dist;
            Vector2 hitNormal = Vector2.zero;
            bool hitSomething = false;

            for (int i = 0; i < count; i++)
            {
                // subtract skin so we donft touch-run into continuous correction
                float permit = Mathf.Max(0f, hits[i].distance - skin);
                if (permit < allowed)
                {
                    allowed = permit;
                    hitNormal = hits[i].normal;
                    hitSomething = true;
                }
            }

            pos += dir * allowed;

            if (!hitSomething)
            {
                remaining = Vector2.zero; // moved full delta
            }
            else
            {
                // Hard stop:
                // remaining = Vector2.zero;

                // Or: single slide along the wall (project out the normal)
                Vector2 leftover = dir * (dist - allowed);
                Vector2 tangent = leftover - Vector2.Dot(leftover, hitNormal) * hitNormal;
                remaining = tangent; // next pass tries to slide
            }
        }

        rb.MovePosition(pos);
    }
}
