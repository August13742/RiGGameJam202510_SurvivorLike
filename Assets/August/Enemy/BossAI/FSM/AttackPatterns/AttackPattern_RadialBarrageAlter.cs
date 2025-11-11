using Survivor.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New RadialBarrageAlterPattern", menuName = "Defs/Boss Attacks/Radial Barrage Alter")]
    public sealed class AttackPattern_RadialBarrageAlter : AttackPattern
    {
        [Header("Projectile & Damage")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float damage = 5f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float life = 4f;
        [SerializeField] private bool homing = false;
        [SerializeField] private float homingDuration = 0.75f;

        [Header("Barrage Shape")]
        [SerializeField] private int bulletsPerWave = 24;
        [SerializeField] private float ringRadius = 1.5f;      // local offset from root
        [SerializeField] private float initialAngleDeg = 0f;   // starting rotation for symmetry control

        [Header("Timeline")]
        [SerializeField] private float windupSeconds = 0.25f;  // pre-spawn telegraph
        [SerializeField] private float holdSeconds = 0.60f;    // bullets are parented & root rotates
        [SerializeField] private float interWaveDelay = 0.25f; // after release before next wave

        [Header("Root Rotation During Hold")]
        [SerializeField] private float steadySpinDegPerSec = 90f;  // continuous rotation during hold
        [SerializeField, Range(0f, 1f)] private float spinImpulseAtFrac = 0.5f; // e.g., mid-cast
        [SerializeField] private float spinImpulseDegrees = 35f;    // extra instantaneous spin at the fraction
        [SerializeField] private float spinImpulseDuration = 0.05f; // do it over a short time to be visible

        [Header("Repetitions")]
        [SerializeField] private bool repIsProbabilistic = false;
        [SerializeField] private int repetitions = 3;                // used if not probabilistic
        [SerializeField, Range(0f, 1f)] private float probDecayPerWave = 0.25f; // p -= this
        [SerializeField] private int hardCap = 32;

        [Header("Enrage")]
        [SerializeField, Range(0f, 1f)] private float healthThresholdForEnrage = 0.5f;
        [SerializeField] private float enrageRateMul = 1.15f;   // faster timeline
        [SerializeField] private float enrageSpinMul = 1.25f;   // more rotation
        [SerializeField] private float enrageBulletMul = 1.10f; // +10% bullets (rounded)
        [SerializeField, Range(0f, 1f)] private float enrageDecayReduction = 0.5f;

        [Header("Animation (optional)")]
        [SerializeField] private string castAnim = "Shoot";

        // Execute method remains the same as your fully-featured version
        public override IEnumerator Execute(BossController controller)
        {
            if (projectilePrefab == null || controller == null || controller.PlayerTransform == null)
                yield break;

            // Health snapshot
            float hp = 1f;
            var hc = controller.GetComponent<HealthComponent>();
            if (hc != null) hp = hc.GetCurrentPercent();

            bool enraged = hp <= healthThresholdForEnrage;

            // Enrage scalars
            float rateMul = enraged ? enrageRateMul : 1f;
            float steadySpin = steadySpinDegPerSec * (enraged ? enrageSpinMul : 1f);
            float impulseDeg = spinImpulseDegrees * (enraged ? enrageSpinMul : 1f);
            int bullets = Mathf.Max(1, Mathf.RoundToInt(bulletsPerWave * (enraged ? enrageBulletMul : 1f)));
            float decay = repIsProbabilistic
                ? probDecayPerWave * (enraged ? enrageDecayReduction : 1f)
                : probDecayPerWave;

            // Telegraph
            if (controller.Animator && !string.IsNullOrEmpty(castAnim))
                controller.Animator.Play(castAnim);
            yield return new WaitForSeconds(windupSeconds / rateMul);

            if (repIsProbabilistic)
            {
                float p = 1f;
                int guard = 0;
                while (Random.value <= p && guard++ < hardCap)
                {
                    yield return FireOneWave(controller, bullets, steadySpin, impulseDeg, rateMul);
                    p = Mathf.Max(0f, p - decay);
                    yield return new WaitForSeconds(interWaveDelay / rateMul);
                }
            }
            else
            {
                int reps = Mathf.Max(0, repetitions);
                for (int i = 0; i < reps; i++)
                {
                    yield return FireOneWave(controller, bullets, steadySpin, impulseDeg, rateMul);
                    if (i < reps - 1)
                        yield return new WaitForSeconds(interWaveDelay / rateMul);
                }
            }
        }


        private IEnumerator FireOneWave(BossController controller, int bullets, float steadySpin, float impulseDeg, float rateMul)
        {
            // Root object holds all bullets and drives their rotation
            var root = new GameObject("RadialRoot").transform;
            root.SetPositionAndRotation(controller.transform.position, Quaternion.identity);
            root.SetParent(controller.transform, worldPositionStays: true);

            var spawned = new List<Weapon.EnemyBullet2D>(bullets);
            float step = 360f / bullets;
            Transform player = controller.PlayerTransform;

            // --- Spawn & fire immediately, but keep them parented ---
            for (int i = 0; i < bullets; i++)
            {
                float ang = initialAngleDeg + step * i;
                Vector2 local = Polar(ringRadius, ang * Mathf.Deg2Rad);

                var go = Object.Instantiate(projectilePrefab, root);
                go.transform.SetLocalPositionAndRotation(local, Quaternion.Euler(0, 0, ang));

                var bullet = go.GetComponent<Weapon.EnemyBullet2D>();
                if (bullet == null)
                {
                    Debug.LogWarning("Projectile prefab missing EnemyBullet2D; destroying.");
                    Object.Destroy(go);
                    continue;
                }

                // enable and fire immediately (still parented)
                bullet.enabled = true;
                Vector2 worldPos = go.transform.position;
                Vector2 outward = ((Vector2)worldPos - (Vector2)root.position).normalized;
                bullet.Fire(worldPos, outward, speed, damage, life, player,
                    overrideHomingSeconds: homing ? homingDuration : 0f);

                spawned.Add(bullet);
            }

            // --- Rotate root while bullets remain parented ---
            float duration = holdSeconds / rateMul;
            float t = 0f;
            bool didImpulse = false;
            float impulseT = Mathf.Clamp01(spinImpulseAtFrac) * duration;
            float impulseEnd = impulseT + (spinImpulseDuration / rateMul);

            while (t < duration)
            {
                float dt = Time.deltaTime;
                t += dt;

                root.Rotate(0f, 0f, steadySpin * dt, Space.Self);

                if (!didImpulse && t >= impulseT)
                {
                    float remain = Mathf.Min(dt, Mathf.Max(0f, impulseEnd - (t - dt)));
                    float frac = remain / (spinImpulseDuration / rateMul);
                    float delta = impulseDeg * frac;
                    root.Rotate(0f, 0f, delta);

                    if (t >= impulseEnd)
                        didImpulse = true;
                }

                yield return null;
            }

            // --- Detach bullets and clean up root ---
            for (int i = 0; i < spawned.Count; i++)
            {
                var b = spawned[i];
                if (b && b.transform) b.transform.SetParent(null, worldPositionStays: true);
            }

            Object.Destroy(root.gameObject);
        }


        private static Vector2 Polar(float r, float rad)
        {
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(r * c, r * s);
        }
    }
}
