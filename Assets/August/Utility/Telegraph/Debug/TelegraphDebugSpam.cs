using UnityEngine;
using UnityEngine.InputSystem;
using AugustsUtility.Telegraph;


/*
    T – single circle at mouse.

    Y – radial burst of circles.

    G – single box (oriented from mouse → right).

    H – random box spam around mouse.

    B – single sector at mouse, pointing towards the player, arc 60°.

    N – sector fan (3 directions) at mouse.
 
 */
[DefaultExecutionOrder(10)]
public sealed class TelegraphDebugSpam : MonoBehaviour
{
    [Header("Circle Settings")]
    [SerializeField] private float circleRadius = 3f;
    [SerializeField] private float circleDuration = 1.0f;
    [SerializeField] private Color circleColor = Color.red;
    [SerializeField] private int radialBurstCount = 12;
    [SerializeField] private float radialBurstRadius = 4f;

    [Header("Box Settings")]
    [SerializeField] private Vector2 boxSize = new Vector2(4f, 2f);
    [SerializeField] private float boxDuration = 1.0f;
    [SerializeField] private Color boxColor = Color.yellow;

    [Header("Sector Settings")]
    [SerializeField] private float sectorRadius = 4f;
    [SerializeField] private float sectorArcDeg = 60f;
    [SerializeField] private float sectorDuration = 1.0f;
    [SerializeField] private Color sectorColor = Color.cyan;

    private InputAction _actCircleSingle;
    private InputAction _actCircleRadial;
    private InputAction _actBoxSingle;
    private InputAction _actBoxScatter;
    private InputAction _actSectorSingle;
    private InputAction _actSectorFan;

    private Camera _cam;
    [SerializeField] private Transform player; // optional, for sector direction. Can be null.

    private void OnEnable()
    {
        _cam = Camera.main;

        _actCircleSingle = MakeKey("<Keyboard>/t", OnCircleSingle);
        _actCircleRadial = MakeKey("<Keyboard>/y", OnCircleRadial);
        _actBoxSingle = MakeKey("<Keyboard>/g", OnBoxSingle);
        _actBoxScatter = MakeKey("<Keyboard>/h", OnBoxScatter);
        _actSectorSingle = MakeKey("<Keyboard>/b", OnSectorSingle);
        _actSectorFan = MakeKey("<Keyboard>/n", OnSectorFan);
    }

    private void OnDisable()
    {
        DisposeAction(_actCircleSingle, OnCircleSingle);
        DisposeAction(_actCircleRadial, OnCircleRadial);
        DisposeAction(_actBoxSingle, OnBoxSingle);
        DisposeAction(_actBoxScatter, OnBoxScatter);
        DisposeAction(_actSectorSingle, OnSectorSingle);
        DisposeAction(_actSectorFan, OnSectorFan);
    }

    private InputAction MakeKey(string binding, System.Action<InputAction.CallbackContext> handler)
    {
        InputAction a = new InputAction(type: InputActionType.Button, binding: binding);
        a.performed += handler;
        a.Enable();
        return a;
    }

    private void DisposeAction(InputAction act, System.Action<InputAction.CallbackContext> handler)
    {
        if (act == null) return;
        act.performed -= handler;
        act.Disable();
    }

    private Vector3 WorldMouse()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return Vector3.zero;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 world = _cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        world.z = 0f;
        return world;
    }

    // ---------------------------
    //  CIRCLE
    // ---------------------------

    private void OnCircleSingle(InputAction.CallbackContext ctx)
    {
        Vector3 pos = WorldMouse();
        Telegraph.Circle(this, pos, circleRadius, circleDuration, circleColor);
    }

    private void OnCircleRadial(InputAction.CallbackContext ctx)
    {
        Vector3 center = WorldMouse();
        for (int i = 0; i < radialBurstCount; i++)
        {
            float ang = (i / (float)radialBurstCount) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radialBurstRadius;
            Telegraph.Circle(this, center + offset, circleRadius, circleDuration, circleColor);
        }
    }

    // ---------------------------
    //  BOX
    // ---------------------------

    private void OnBoxSingle(InputAction.CallbackContext ctx)
    {
        Vector3 pos = WorldMouse();

        // Orientation: if player exists, box faces from player to mouse.
        float angleDeg = 0f;
        if (player != null)
        {
            Vector2 dir = (pos - player.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }
        }

        Telegraph.Box(this, pos, boxSize, boxDuration, angleDeg, boxColor);
    }

    private void OnBoxScatter(InputAction.CallbackContext ctx)
    {
        Vector3 center = WorldMouse();
        for (int i = 0; i < 10; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * 6f;
            float angleDeg = Random.Range(0f, 360f);
            Telegraph.Box(this,
                          center + (Vector3)rnd,
                          boxSize,
                          boxDuration,
                          angleDeg,
                          boxColor);
        }
    }

    // ---------------------------
    //  SECTOR
    // ---------------------------

    private void OnSectorSingle(InputAction.CallbackContext ctx)
    {
        Vector3 pos = WorldMouse();

        float angleDeg = 0f;
        if (player != null)
        {
            Vector2 dir = (pos - player.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }
        }

        Telegraph.Sector(this,
                         pos,
                         sectorRadius,
                         sectorArcDeg,
                         angleDeg,
                         sectorDuration,
                         sectorColor);
    }

    private void OnSectorFan(InputAction.CallbackContext ctx)
    {
        Vector3 pos = WorldMouse();

        // 3 cones: -sectorArcDeg, 0, +sectorArcDeg
        for (int i = -1; i <= 1; i++)
        {
            float angleDeg = i * sectorArcDeg;
            Telegraph.Sector(this,
                             pos,
                             sectorRadius,
                             sectorArcDeg,
                             angleDeg,
                             sectorDuration,
                             sectorColor);
        }
    }
}
