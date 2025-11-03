using UnityEngine;
using System.Collections.Generic;
using Survivor.Weapon;

public sealed class DroneFormationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Survivor.Control.PlayerController playerController;
    [SerializeField] private DroneAgent dronePrefab;

    [Header("Weapon Services (shared)")]
    [SerializeField] private Transform sharedPoolRoot;
    [SerializeField] private LayerMask enemyMask = ~0;
    [SerializeField] private float searchRadius = 12f;

    [Header("Leader / Behind Settings")]
    [SerializeField] private float behindDistance = 3.0f;
    [SerializeField, Range(0f, 1f)] private float forwardLerp = 0.25f;

    [Header("Slot Layout")]
    [SerializeField] private float arcHalfAngleDeg = 65f;
    [SerializeField] private float ellipseA = 1.2f;
    [SerializeField] private float ellipseB = 0.7f;
    [SerializeField] private float spacingScale = 1.0f;

    [Header("Assignment")]
    [SerializeField, Range(1.0f, 2.0f)] private float reassignGain = 1.15f;

    [SerializeField, Min(0)] private int initialDroneCount = 1;

    private readonly List<DroneAgent> _drones = new List<DroneAgent>();
    private readonly List<Vector2> _slots = new List<Vector2>(8);
    private readonly List<int> _slotIndex = new List<int>(8);
    private readonly List<(float ang, int idx)> _anglePairs = new List<(float, int)>(8);

    private Vector2 _fwdSmooth = Vector2.right;

    private void Awake()
    {
        if (playerController == null && player != null) playerController = player.GetComponent<Survivor.Control.PlayerController>();
        if (sharedPoolRoot == null)
        {
            GameObject pools = GameObject.FindWithTag("PoolRoot");
            sharedPoolRoot = pools ? pools.transform : new GameObject("Pools").transform;
        }

        for (int i = 0; i < initialDroneCount; i++)
        {
            DroneAgent d = Instantiate(dronePrefab, transform);
            _drones.Add(d);
            _slotIndex.Add(i);

            // initialize per-drone weapons with shared services
            d.InitialiseWeapons(sharedPoolRoot, enemyMask, searchRadius, Team.Player);
        }
    }

    private void Update()
    {
        if (player == null) return;

        // --- 1. Calculate Formation Shape and Slot Positions (No Change) ---
        Vector2 fwdRaw = GetPlayerForward();
        _fwdSmooth = Vector2.Lerp(_fwdSmooth, fwdRaw, forwardLerp);
        if (_fwdSmooth.sqrMagnitude < 1e-6f) _fwdSmooth = Vector2.right;
        _fwdSmooth.Normalize();

        Vector2 center = (Vector2)player.position - _fwdSmooth * behindDistance;
        ComputeSlots(center, _fwdSmooth, _drones.Count, _slots);

        // --- 2. Assign Drones to Slots (No Change, but be aware of instability) ---
        ReassignStable(center, _fwdSmooth, _slots);

        // --- 3. Update Each Drone's State ---
        float dt = Time.deltaTime;
        for (int i = 0; i < _drones.Count; i++)
        {
            DroneAgent drone = _drones[i];
            int slotIndex = _slotIndex[i];
            Vector2 targetSlotPosition = _slots[slotIndex];

            Vector2 newVelocity = drone.CalculateSteeringVelocity(targetSlotPosition, _drones, dt);

            // apply position update
            drone.transform.position += (Vector3)newVelocity * dt;

            // set rotation to match the formation's orientation
            if (_fwdSmooth.sqrMagnitude > 1e-4f)
            {
                drone.transform.right = _fwdSmooth;
            }
        }
    }

    private Vector2 GetPlayerForward()
    {
        if (playerController != null && playerController.CurrentVelocity.sqrMagnitude > 0.01f)
            return playerController.CurrentVelocity.normalized;
        
        return (_fwdSmooth.sqrMagnitude > 0.01f) ? _fwdSmooth : Vector2.right; // fallback: Last known direction
    }

    private void ComputeSlots(in Vector2 center, in Vector2 fwd, int n, List<Vector2> outWorld)
    {
        outWorld.Clear();
        if (n <= 0) return;

        float half = arcHalfAngleDeg * Mathf.Deg2Rad;
        float arc = 2f * half;
        float widen = Mathf.Clamp(0.6f + 0.05f * n, 0.6f, 1.4f) * spacingScale;
        Vector2 right = new (-fwd.y, fwd.x);

        for (int k = 0; k < n; k++)
        {
            float t = (n == 1) ? 0f : (k / (float)(n - 1));
            float theta = -half + t * arc;
            float ex = ellipseA * widen * Mathf.Cos(theta);
            float ey = ellipseB * widen * Mathf.Sin(theta);
            Vector2 local = new (ex, ey + ellipseB);
            Vector2 world = center + right * local.x - fwd * local.y;
            outWorld.Add(world);
        }
    }

    private void ReassignStable(in Vector2 center, in Vector2 fwd, List<Vector2> slots)
    {
        _anglePairs.Clear();
        Vector2 right = new (-fwd.y, fwd.x);

        for (int i = 0; i < _drones.Count; i++)
        {
            Vector2 rel = (Vector2)_drones[i].transform.position - center;
            float x = Vector2.Dot(rel, right);
            float y = -Vector2.Dot(rel, fwd);
            float ang = Mathf.Atan2(y, x);
            _anglePairs.Add((ang, i));
        }
        _anglePairs.Sort((a, b) => a.ang.CompareTo(b.ang));

        for (int rank = 0; rank < _anglePairs.Count; rank++)
        {
            int di = _anglePairs[rank].idx;
            int desired = rank;
            int current = _slotIndex[di];
            if (current == desired) continue;

            float curDist = ((Vector2)_drones[di].transform.position - slots[current]).sqrMagnitude;
            float newDist = ((Vector2)_drones[di].transform.position - slots[desired]).sqrMagnitude;

            if (newDist * reassignGain < curDist)
                _slotIndex[di] = desired;
        }
    }
}
