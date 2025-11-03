using UnityEngine;
using System.Collections.Generic;
using Survivor.Weapon;

public sealed class DroneAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6.5f;
    [SerializeField] private float arriveRadius = 0.25f;
    [SerializeField] private float separationRadius = 1.1f;
    [SerializeField] private float separationGain = 22f;
    [SerializeField] private float separationMax = 30f;

    [Header("Weapon")]
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private Transform fireOrigin;

    private Vector2 _vel = Vector2.zero;
    private Vector2 _target = Vector2.zero;

    // Called by FormationManager every frame
    public Vector2 CalculateSteeringVelocity(in Vector2 worldTarget, in List<DroneAgent> neighbors, float dt)
    {
        _target = worldTarget;

        Vector2 pos = transform.position;
        Vector2 toT = _target - pos;
        // Reduce speed as it gets closer for a smoother stop.
        float desiredSpeed = maxSpeed;
        if (toT.magnitude < arriveRadius)
        {
            // Map distance inside arriveRadius to a speed from maxSpeed down to 0.
            desiredSpeed = maxSpeed * (toT.magnitude / arriveRadius);
        }
        Vector2 desiredVel = toT.normalized * desiredSpeed;

        // simple steering approach:
        Vector2 steeringForce = (desiredVel - _vel); // Force needed to get from current to desired velocity.

        Vector2 sep = ComputeSeparation(pos, neighbors);

        // Combine and apply acceleration
        Vector2 acc = steeringForce + sep; // We can weight these if needed.
        _vel += acc * dt;

        // Clamp final velocity
        float spd = _vel.magnitude;
        if (spd > maxSpeed) _vel *= (maxSpeed / spd);

        return _vel; // Return the final velocity for the manager to use.
    }

    private Vector2 ComputeSeparation(in Vector2 pos, in List<DroneAgent> neighbors)
    {
        Vector2 accum = Vector2.zero;
        float rs = separationRadius;
        for (int i = 0; i < neighbors.Count; i++)
        {
            DroneAgent other = neighbors[i];
            if (other == this) continue;
            Vector2 d = pos - (Vector2)other.transform.position;
            float dist = d.magnitude;
            if (dist < 1e-4f || dist >= rs) continue;

            float w = 1f - (dist / rs);
            w *= w;
            Vector2 push = (d / dist) * (w * separationGain);
            accum += push;
        }
        float len = accum.magnitude;
        if (len > separationMax && len > 0f) accum *= (separationMax / len);
        return accum;
    }

    // Initialise the drone’s weapon context from the formation layer.
    public void InitialiseWeapons(Transform sharedPoolRoot, LayerMask enemyMask, float searchRadius, Team team = Team.Player)
    {
        if (weaponController == null)
            weaponController = GetComponentInChildren<WeaponController>();

        if (fireOrigin == null)
            fireOrigin = this.transform; // fallback: use body if not set

        if (weaponController != null)
            weaponController.InitialiseFromHost(this.transform, fireOrigin, sharedPoolRoot, enemyMask, searchRadius, team);
    }
}
