using UnityEngine;

/// Orthographic 2D camera that clamps to world bounds discovered via one-time raycasts.
/// Assumes rectangular arena made of colliders on a specific layer.
[ExecuteAlways]
public sealed class RaycastBoundedCamera2D : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;
    [SerializeField] private float acceleration = 4f; // lerp rate

    [Header("Raycast setup (do once)")]
    [SerializeField] private LayerMask wallsMask;      // layer(s) of wall colliders
    [SerializeField] private float maxCast = 1000f;    // must easily exceed arena half-size
    [SerializeField] private bool rebuildOnEnable = true;

    [Header("Optional deadzone (world units)")]
    [SerializeField] private Vector2 deadzoneHalfSize = Vector2.zero;

    private Camera cam;

    // Discovered inner rectangle (camera centers will be clamped *inside* minus half-extents)
    private bool boundsValid;
    private float innerMinX, innerMaxX, innerMinY, innerMaxY;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
            if (cam == null) Debug.LogError("Camera Not Found");
        }
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) target = p.transform;
        }
        if (rebuildOnEnable) TryDiscoverBounds();
    }

    private void OnEnable()
    {
        if (rebuildOnEnable) TryDiscoverBounds();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && rebuildOnEnable) TryDiscoverBounds();
    }

    /// One-time discovery using 4 raycasts from the seed (player) position.
    public void TryDiscoverBounds()
    {
        boundsValid = false;
        if (!target) return;

        Vector2 seed = target.position;

        // must start *inside* the arena; otherwise it�fll hit the outside.
        var hitL = Physics2D.Raycast(seed, Vector2.left, maxCast, wallsMask);
        var hitR = Physics2D.Raycast(seed, Vector2.right, maxCast, wallsMask);
        var hitD = Physics2D.Raycast(seed, Vector2.down, maxCast, wallsMask);
        var hitU = Physics2D.Raycast(seed, Vector2.up, maxCast, wallsMask);

        if (hitL.collider && hitR.collider && hitD.collider && hitU.collider)
        {
            // For axis-aligned walls, hit.point is the inner face.
            innerMinX = hitL.point.x;
            innerMaxX = hitR.point.x;
            innerMinY = hitD.point.y;
            innerMaxY = hitU.point.y;

            // Sanity: ensure min < max
            boundsValid = innerMinX < innerMaxX && innerMinY < innerMaxY;
        }
    }

    private void LateUpdate()
    {
        if (!target) return;

        Vector3 follow = new (target.position.x, target.position.y, transform.position.z);

        if (!cam || !cam.orthographic || !boundsValid)
        {
            // Fallback: plain follow
            transform.position = Vector3.Lerp(transform.position, follow, acceleration * Time.deltaTime);
            return;
        }

        // Camera half-extents
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // Clamp camera center so view never leaves the arena
        float minCamX = innerMinX + halfW;
        float maxCamX = innerMaxX - halfW;
        float minCamY = innerMinY + halfH;
        float maxCamY = innerMaxY - halfH;

        Vector3 desired = follow;

        // Optional per-axis deadzone
        if (deadzoneHalfSize.x > 0f)
        {
            float dx = desired.x - transform.position.x;
            if (Mathf.Abs(dx) <= deadzoneHalfSize.x) desired.x = transform.position.x;
        }
        if (deadzoneHalfSize.y > 0f)
        {
            float dy = desired.y - transform.position.y;
            if (Mathf.Abs(dy) <= deadzoneHalfSize.y) desired.y = transform.position.y;
        }

        float cx = Mathf.Clamp(desired.x, minCamX, maxCamX);
        float cy = Mathf.Clamp(desired.y, minCamY, maxCamY);

        // Handle the arena smaller than view�h corner case
        if (minCamX > maxCamX) cx = (innerMinX + innerMaxX) * 0.5f;
        if (minCamY > maxCamY) cy = (innerMinY + innerMaxY) * 0.5f;

        Vector3 clamped = new (cx, cy, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, clamped, acceleration * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!boundsValid) return;
        Gizmos.color = Color.cyan;
        Vector3 c = new ((innerMinX + innerMaxX) * 0.5f, (innerMinY + innerMaxY) * 0.5f, 0f);
        Vector3 s = new (innerMaxX - innerMinX, innerMaxY - innerMinY, 0f);
        Gizmos.DrawWireCube(c, s);
    }
}
