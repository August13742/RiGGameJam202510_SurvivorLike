using UnityEngine;

/// <summary>
/// Procedurally draws the outline of a BoxCollider2D in the Game view.
/// This script automatically adds and configures a LineRenderer component.
/// </summary>
[RequireComponent(typeof(BoxCollider2D), typeof(LineRenderer))]
public class BoxCollider2DVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Color _lineColor = Color.green;

    [SerializeField]
    private float _lineWidth = 0.05f;

    [SerializeField]
    private int _sortingOrder = 10;

    private LineRenderer _lineRenderer;
    private BoxCollider2D _boxCollider;

    // We can cache the corner points array to avoid allocating memory every frame.
    private Vector3[] _corners = new Vector3[4];

    void Awake()
    {
        // Get the required components
        _lineRenderer = GetComponent<LineRenderer>();
        _boxCollider = GetComponent<BoxCollider2D>();

        // Configure the LineRenderer for 2D visualization
        SetupLineRenderer();
    }

    /// <summary>
    /// Sets the one-time properties of the LineRenderer.
    /// </summary>
    void SetupLineRenderer()
    {
        // We need 4 points for a box.
        _lineRenderer.positionCount = 4;

        // Use local space so the line moves with the GameObject's transform.
        _lineRenderer.useWorldSpace = false;

        // Ensure the line loops back to the start.
        _lineRenderer.loop = true;

        // Use a simple unlit material for consistent visibility.
        // "Sprites/Default" is a built-in unlit material that works well.
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    /// <summary>
    /// Using LateUpdate ensures that the visualization updates *after*
    /// all physics and transform calculations in Update() have finished.
    /// </summary>
    void LateUpdate()
    {
        // Update visual properties every frame (in case they are changed in the Inspector)
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.startColor = _lineColor;
        _lineRenderer.endColor = _lineColor;
        _lineRenderer.sortingOrder = _sortingOrder;

        // Calculate the collider's corners in local space
        Vector2 center = _boxCollider.offset;
        float halfWidth = _boxCollider.size.x / 2f;
        float halfHeight = _boxCollider.size.y / 2f;

        // Calculate corner positions
        // We use Vector3 because LineRenderer works with Vector3s.
        _corners[0] = new Vector3(center.x - halfWidth, center.y - halfHeight, 0); // Bottom-Left
        _corners[1] = new Vector3(center.x + halfWidth, center.y - halfHeight, 0); // Bottom-Right
        _corners[2] = new Vector3(center.x + halfWidth, center.y + halfHeight, 0); // Top-Right
        _corners[3] = new Vector3(center.x - halfWidth, center.y + halfHeight, 0); // Top-Left

        // Set the positions on the LineRenderer
        _lineRenderer.SetPositions(_corners);
    }
}