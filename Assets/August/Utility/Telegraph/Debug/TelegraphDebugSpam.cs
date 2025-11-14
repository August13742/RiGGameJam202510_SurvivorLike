using UnityEngine;
using UnityEngine.InputSystem;
using AugustsUtility.Telegraph;



/*
    T	Single circle at mouse
    Y	5 quick telegraphs jittered around the mouse
    U	Radial burst (12 by default) around mouse
    I	Random scatter in large radius
    O	Expanding multi-ring telegraph sequence
 */
[DefaultExecutionOrder(10)]
public sealed class TelegraphDebugSpam : MonoBehaviour
{
    [Header("Common Settings")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private Color color = Color.red;

    [Header("Spam Settings")]
    [SerializeField] private int radialBurstCount = 12;
    [SerializeField] private float radialBurstRadius = 4f;

    [SerializeField] private int randomScatterCount = 20;
    [SerializeField] private float randomScatterRadius = 6f;

    [SerializeField] private int expandingRingCount = 5;
    [SerializeField] private float expandingRingSpacing = 0.5f;

    // InputActions
    private InputAction actSingle;
    private InputAction actRapid;
    private InputAction actRadialBurst;
    private InputAction actRandomScatter;
    private InputAction actExpandingRing;

    private Camera _cam;

    private void OnEnable()
    {
        _cam = Camera.main;

        actSingle = MakeKey("<Keyboard>/t", OnSingle);
        actRapid = MakeKey("<Keyboard>/y", OnRapid);
        actRadialBurst = MakeKey("<Keyboard>/u", OnRadialBurst);
        actRandomScatter = MakeKey("<Keyboard>/i", OnRandomScatter);
        actExpandingRing = MakeKey("<Keyboard>/o", OnExpandingRing);
    }

    private void OnDisable()
    {
        Disable(actSingle);
        Disable(actRapid);
        Disable(actRadialBurst);
        Disable(actRandomScatter);
        Disable(actExpandingRing);
    }

    private InputAction MakeKey(string binding, System.Action<InputAction.CallbackContext> handler)
    {
        var a = new InputAction(type: InputActionType.Button, binding: binding);
        a.performed += handler;
        a.Enable();
        return a;
    }

    private void Disable(InputAction act)
    {
        if (act == null) return;
        act.performed -= null;
        act.Disable();
    }

    private Vector3 WorldMouse()
    {
        var v = Mouse.current.position.ReadValue();
        var w = _cam.ScreenToWorldPoint(v);
        w.z = 0f;
        return w;
    }

    // ---------------------------
    //  SPAM TYPES
    // ---------------------------

    private void OnSingle(InputAction.CallbackContext ctx)
    {
        SpawnCircle(WorldMouse());
    }

    private void OnRapid(InputAction.CallbackContext ctx)
    {
        Vector3 pos = WorldMouse();
        for (int i = 0; i < 5; i++)
        {
            SpawnCircle(pos + new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                0f));
        }
    }

    private void OnRadialBurst(InputAction.CallbackContext ctx)
    {
        Vector3 center = WorldMouse();
        for (int i = 0; i < radialBurstCount; i++)
        {
            float ang = (i / (float)radialBurstCount) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radialBurstRadius;
            SpawnCircle(center + offset);
        }
    }

    private void OnRandomScatter(InputAction.CallbackContext ctx)
    {
        Vector3 center = WorldMouse();
        for (int i = 0; i < randomScatterCount; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * randomScatterRadius;
            SpawnCircle(center + (Vector3)rnd);
        }
    }

    private void OnExpandingRing(InputAction.CallbackContext ctx)
    {
        Vector3 center = WorldMouse();
        var seq = TelegraphSequence.Begin(this);

        for (int i = 0; i < expandingRingCount; i++)
        {
            float r = radius + i * expandingRingSpacing;
            seq.Circle(center, r, duration);
        }

        seq.Play();
    }

    private void SpawnCircle(Vector3 pos)
    {
        Telegraph.Circle(this, pos, radius, duration, color);
    }
}
