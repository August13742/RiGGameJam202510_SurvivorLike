using AugustsUtility.CameraShake;
using AugustsUtility.Telegraph;
using Survivor.Control;
using Survivor.Game;
using Survivor.Weapon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New MeteorGravityWellPattern",
        menuName = "Defs/Boss Attacks/Meteor Gravity Well")]
    public sealed class AttackPattern_MeteorGravityWell : AttackPattern
    {
        [SerializeField] private LayerMask hitMask;

        [Header("Impact Parameter")]
        [SerializeField] private float cameraShakeStrength = 3f;
        [SerializeField] private float cameraShakeDuration = 1f;
        [SerializeField] private float globalPauseDuration = 0.5f;

        [Header("Meteor Prefab")]
        [SerializeField] private GameObject meteorPrefab;

        [Header("Meteor Ring (where meteors fall)")]
        [SerializeField] private float minMeteorRadius = 4f;
        [SerializeField] private float maxMeteorRadius = 10f;

        [Header("Wave Configuration")]
        [SerializeField] private int waves = 3;
        [SerializeField] private int minMeteorsPerWave = 3;
        [SerializeField] private int maxMeteorsPerWave = 6;

        [Header("Telegraph Timeline")]
        [Tooltip("Duration of each wave's impact telegraph.")]
        [SerializeField] private float telegraphDuration = 1.0f;

        [Tooltip("Time after telegraph start when meteors spawn and start falling. < telegraphDuration.")]
        [SerializeField] private float meteorSpawnLead = 0.4f;

        [Tooltip("Delay between end of one wave's telegraph and start of the next.")]
        [SerializeField] private float interWaveDelay = 0.25f;

        [Header("Channel Rings")]
        [Tooltip("Radius of the large pull ring (INNER gravity well range during channel).")]
        [SerializeField] private float pullRadius = 12f;

        [Tooltip("Radius of the inner kill zone explosion at the end.")]
        [SerializeField] private float innerExplosionRadius = 3f;

        [Header("Ring Colors")]
        [SerializeField] private Color pullRingColor = new(0.5f, 0.7f, 1f, 1f);
        [SerializeField] private Color meteorRingColor = new(1f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color innerRingColor = new(1f, 0.3f, 0.3f, 1f);

        [Header("Meteor Properties")]
        [Tooltip("Base impact damage per meteor (scaled by enrage).")]
        [SerializeField] private float meteorImpactDamage = 25f;
        [SerializeField] private float meteorImpactRadius = 1.2f;
        [SerializeField] private Vector2 meteorSpawnOffset = new(0f, 10f);

        [Header("Hazard Zone (optional)")]
        [Tooltip("If assigned, a hazard zone will be spawned where each meteor lands.")]
        [SerializeField] private GameObject hazardZonePrefab;
        [Tooltip("If true, hazards only appear while enraged.")]
        [SerializeField] private bool hazardOnlyEnraged = true;
        [SerializeField] private float hazardRadius = 2f;
        [SerializeField] private float hazardDamagePerSecond = 8f;
        [SerializeField] private float hazardLifetime = 3f;

        [Header("Final Explosion")]
        [SerializeField] private GameObject explosionVfxPrefab;
        [SerializeField] private float explosionDamage = 80f;

        [Header("Initial Big Pull Phase")]
        [SerializeField] private bool PullOnlyWhenEnraged = true;
        [Tooltip("Delay before the big global pull starts.")]
        [SerializeField] private float bigPullDelay = 0.35f;
        [Tooltip("Radius of the big, initial gravity well.")]
        [SerializeField] private float bigPullRadius = 18f;
        [Tooltip("Max distance the player can be dragged during the big pull.")]
        [SerializeField] private float bigPullMaxTravel = 8f;


        [Header("Inner Pull Phase")]
        [Tooltip("Delay (after channel start) before the inner, frequent pulls start.")]
        [SerializeField] private float innerPullDelay = 0.3f;
        [Tooltip("Seconds between inner pull pulses (scaled by enrage).")]
        [SerializeField] private float pullInterval = 0.4f;
        [Tooltip("How far each pull pulse drags the player (world units).")]
        [SerializeField] private float pullStepDistance = 1.5f;
        [SerializeField] private Color bigPullRingColor = new(0.5f, 0.9f, 1f, 1f);
        [SerializeField] private float bigPullRingExtraLifetime = 0.15f;

        
        [Header("Animation (optional)")]
        [SerializeField] private string channelAnim = "Cast";
        [SerializeField] private string releaseAnim = "Shoot";

        [Header("Enrage Scaling")]
        [SerializeField] private float enrageRateMul = 1.15f;      // faster timeline
        [SerializeField] private float enrageMeteorCountMul = 1.25f;
        [SerializeField] private float enrageImpactDamageMul = 1.35f;
        [SerializeField] private float enrageExplosionDamageMul = 1.4f;
        [SerializeField] private float enrageHazardDpsMul = 1.3f;
        [SerializeField] private float enrageHazardSizeMul = 1.5f;
        [SerializeField] private int enrageExtraWaves = 1;

        private bool _isEnraged;
        private static readonly Collider2D[] _hits = new Collider2D[8];

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || meteorPrefab == null || controller.PlayerTransform == null)
                yield break;

            _isEnraged = controller.IsEnraged;
            float rateMul = _isEnraged ? enrageRateMul : 1f;
            float invRate = 1f / Mathf.Max(rateMul, 0.01f);

            // Scale per-wave timings by enrage
            float waveTelegraph = telegraphDuration * invRate;
            float spawnLeadTime = meteorSpawnLead * invRate;
            float waveGap = interWaveDelay * invRate;

            if (spawnLeadTime > waveTelegraph * 0.95f)
                spawnLeadTime = waveTelegraph * 0.95f;

            int totalWaves = Mathf.Max(1, waves + (_isEnraged ? enrageExtraWaves : 0));

            // Meteor counts
            int minCount = Mathf.Max(1, Mathf.RoundToInt(minMeteorsPerWave * (_isEnraged ? enrageMeteorCountMul : 1f)));
            int maxCount = Mathf.Max(minCount, Mathf.RoundToInt(maxMeteorsPerWave * (_isEnraged ? enrageMeteorCountMul : 1f)));

            // Damage
            float finalMeteorDamage = meteorImpactDamage * (_isEnraged ? enrageImpactDamageMul : 1f);
            float finalExplosionDamage = explosionDamage * (_isEnraged ? enrageExplosionDamageMul : 1f);

            // Hazard parameters
            bool hazardsActive =
                hazardZonePrefab != null &&
                hazardLifetime > 0f &&
                hazardRadius > 0f &&
                (!hazardOnlyEnraged || _isEnraged);

            float finalHazardSize = hazardsActive
                ? hazardRadius * (_isEnraged ? enrageHazardSizeMul : 1f)
                : 0f;

            float finalHazardDps = hazardsActive
                ? hazardDamagePerSecond * (_isEnraged ? enrageHazardDpsMul : 1f)
                : 0f;

            Transform bossTf = controller.transform;

            // --------------------------------------------------
            // PHASE 0: Big global pull (short delay, strong, interpolated)
            // --------------------------------------------------

            if ((PullOnlyWhenEnraged && _isEnraged) || !PullOnlyWhenEnraged)
            {
                float bigDelay = bigPullDelay * invRate;

                // Telegraph the big pull
                Telegraph.Circle(
                    host: controller,
                    pos: bossTf.position,
                    radius: bigPullRadius,
                    duration: bigDelay + bigPullRingExtraLifetime,
                    color: bigPullRingColor
                );

                // Wait, then perform the one-shot yank
                if (bigDelay > 0f)
                    yield return new WaitForSeconds(bigDelay);

                DoBigPullImpulse(controller, bigPullRadius, bigPullMaxTravel);
            }
            // --------------------------------------------------
            // PHASE 1: Start channeling the nuke
            // --------------------------------------------------
            if (controller.Animator != null && !string.IsNullOrEmpty(channelAnim))
            {
                controller.Animator.Play(channelAnim);
                controller.Animator.speed = rateMul;
            }

            // Total channel duration (meteor waves + gaps)
            float channelDuration =
                totalWaves * waveTelegraph +
                Mathf.Max(0, totalWaves - 1) * waveGap;

            // Rings stay up for the whole channel
            Telegraph.Circle(
                host: controller,
                pos: bossTf.position,
                radius: pullRadius,      // inner gravity radius
                duration: channelDuration,
                color: pullRingColor
            );

            Telegraph.Circle(
                host: controller,
                pos: bossTf.position,
                radius: maxMeteorRadius,
                duration: channelDuration,
                color: meteorRingColor
            );

            Telegraph.Circle(
                host: controller,
                pos: bossTf.position,
                radius: innerExplosionRadius,
                duration: channelDuration,
                color: innerRingColor
            );

            // --------------------------------------------------
            // PHASE 2: Reopen gravity (smaller radius, frequent pulls) + meteors
            // --------------------------------------------------
            float innerDelayTime = Mathf.Max(0f, innerPullDelay * invRate);
            if (innerDelayTime > channelDuration * 0.7f)
                innerDelayTime = channelDuration * 0.7f; // don't delay so long it never runs

            float innerPullDurationTotal = Mathf.Max(0f, channelDuration - innerDelayTime);

            if (innerPullDurationTotal > 0f && pullInterval > 0f && pullStepDistance > 0f)
            {
                controller.StartCoroutine(
                    GravityWellPullRoutine(
                        controller,
                        radius: pullRadius,
                        pullStep: pullStepDistance,
                        interval: pullInterval * invRate,
                        duration: innerPullDurationTotal,
                        startDelay: innerDelayTime
                    )
                );
            }

            // Meteor waves timeline (aligned with channelDuration)
            for (int i = 0; i < totalWaves; i++)
            {
                FireMeteorWave(
                    controller,
                    bossTf.position,
                    minCount,
                    maxCount,
                    minMeteorRadius,
                    maxMeteorRadius,
                    waveTelegraph,
                    spawnLeadTime,
                    finalMeteorDamage,
                    hazardsActive,
                    finalHazardSize,
                    finalHazardDps
                );

                float segment = waveTelegraph + ((i < totalWaves - 1) ? waveGap : 0f);
                if (segment > 0f)
                    yield return new WaitForSeconds(segment);
            }

            // Exit channel anim → release
            if (controller.Animator != null && !string.IsNullOrEmpty(releaseAnim))
            {
                controller.Animator.Play(releaseAnim);
                controller.Animator.speed = rateMul;
            }

            // Final inner explosion at boss position
            Vector3 explosionPos = bossTf.position;
            SpawnExplosionVfx(explosionPos, controller);
            ApplyExplosionDamage(explosionPos, innerExplosionRadius, finalExplosionDamage);

            if (controller.Animator != null)
                controller.Animator.speed = 1f;

            controller.VelocityOverride = Vector2.zero;
        }

        // ----------------- WAVE + METEOR LOGIC (unchanged core) -----------------

        private void FireMeteorWave(
            BossController controller,
            Vector3 bossPos,
            int minCount,
            int maxCount,
            float minRadius,
            float maxRadius,
            float telegraphTime,
            float spawnLeadTime,
            float impactDamage,
            bool hazardsActive,
            float hazardSize,
            float hazardDps)
        {
            int meteorCount = Random.Range(minCount, maxCount + 1);
            if (meteorCount <= 0) return;

            float travelDistance = meteorSpawnOffset.magnitude;
            float flightTime = Mathf.Max(0.05f, telegraphTime - spawnLeadTime);
            float meteorSpeed = travelDistance / flightTime;

            List<Vector3> impactPositions = new List<Vector3>(meteorCount);

            for (int i = 0; i < meteorCount; i++)
            {
                float r = Random.Range(minRadius, maxRadius);
                float angRad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angRad), Mathf.Sin(angRad), 0f) * r;
                impactPositions.Add(bossPos + offset);
            }

            foreach (Vector3 impactPos in impactPositions)
            {
                Telegraph.Circle(
                    host: controller,
                    pos: impactPos,
                    radius: meteorImpactRadius,
                    duration: telegraphTime,
                    color: Color.white
                );

                controller.StartCoroutine(
                    MeteorRoutine(
                        impactPos,
                        spawnLeadTime,
                        meteorSpeed,
                        impactDamage,
                        hazardsActive,
                        hazardSize,
                        hazardDps
                    )
                );
            }
        }

        private IEnumerator MeteorRoutine(
            Vector2 impactPos,
            float spawnLeadTime,
            float meteorSpeed,
            float impactDamage,
            bool hazardsActive,
            float hazardSize,
            float hazardDps)
        {
            if (spawnLeadTime > 0f)
                yield return new WaitForSeconds(spawnLeadTime);

            GameObject meteorObj = Object.Instantiate(meteorPrefab);
            if (!meteorObj.TryGetComponent<MeteorProjectile2D>(out MeteorProjectile2D meteor))
            {
                Debug.LogError("MeteorProjectile2D component not found on meteorPrefab!");
                Object.Destroy(meteorObj);
                yield break;
            }

            // Configure hazard behaviour for this meteor (optional)
            if (hazardsActive)
            {
                meteor.ConfigureHazard(
                    hazardZonePrefab,
                    hazardSize,
                    hazardDps,
                    hazardLifetime,
                    enable: true
                );
            }
            else
            {
                meteor.ConfigureHazard(null, 0f, 0f, 0f, enable: false);
            }

            meteor.Launch(
                impactPos: impactPos,
                spawnOffset: meteorSpawnOffset,
                spd: meteorSpeed,
                dmg: impactDamage,
                radius: meteorImpactRadius
            );
        }

        // ----------------- GRAVITY LOGIC -----------------

        /// <summary>
        /// One-time big pull composed of several small external displacement pulses.
        /// Total travel is strictly bounded by maxTravel.
        /// </summary>
        private void DoBigPullImpulse(
                        BossController controller,
                        float radius,
                        float maxTravel)
        {
            if (controller == null) return;
            Transform playerTf = controller.PlayerTransform;
            if (playerTf == null) return;

            PlayerController pc = playerTf.GetComponent<PlayerController>();

            Vector3 center = controller.transform.position;
            Vector3 start = playerTf.position;
            Vector3 toCenter = center - start;
            float dist = toCenter.magnitude;

            // Outside influence or already basically at center → do nothing
            if (dist <= 0.05f || dist > radius)
                return;

            // Clamp how far we can travel this impulse, and don't overshoot the center
            float travel = Mathf.Min(maxTravel, dist * 0.9f);
            if (travel <= 0f)
                return;

            Vector2 delta = (Vector2)(toCenter.normalized * travel);

            if (pc != null)
            {
                // Single bounded impulse through player's external displacement pipe
                pc.AddExternalDisplacement(delta);
            }
            else
            {
                // Fallback if for some reason there is no PlayerController
                playerTf.position += (Vector3)delta;
            }
        }

        /// <summary>
        /// Small-radius gravity well with frequent pulses during the channel.
        /// </summary>
        private IEnumerator GravityWellPullRoutine(
            BossController controller,
            float radius,
            float pullStep,
            float interval,
            float duration,
            float startDelay)
        {
            Transform playerTf = controller.PlayerTransform;
            if (playerTf == null) yield break;

            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            PlayerController pc = playerTf.GetComponent<PlayerController>();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (controller == null || controller.IsDead) yield break;
                if (playerTf == null) yield break;

                Vector3 bossPos = controller.transform.position;
                Vector3 playerPos = playerTf.position;
                Vector2 toBoss = (bossPos - playerPos);

                float dist = toBoss.magnitude;
                if (dist > 0.01f && dist <= radius)
                {
                    Vector2 dir = toBoss / dist;

                    float step = pullStep;
                    if (step > dist)
                        step = dist * 0.9f;

                    if (pc != null)
                    {
                        pc.AddExternalDisplacement(dir * step);
                    }
                    else
                    {
                        playerTf.position += (Vector3)(dir * step);
                    }
                }

                if (interval <= 0f) yield break;

                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }
        }

        // ----------------- EXPLOSION -----------------

        private void SpawnExplosionVfx(Vector3 pos, BossController controller)
        {
            if (explosionVfxPrefab == null) return;

            GameObject vfx = Object.Instantiate(explosionVfxPrefab, pos, Quaternion.identity);

            // Scale VFX based on inner explosion radius
            float scale = innerExplosionRadius;
            vfx.transform.localScale = new Vector3(scale, scale, 1f);

            controller.StartCoroutine(PlayImpactEffect());
        }

        private IEnumerator PlayImpactEffect()
        {
            CameraShake2D.Shake(cameraShakeStrength, cameraShakeDuration);
            yield return new WaitForSeconds(0.15f); // small delay
            HitstopManager.Instance.RequestGlobal(globalPauseDuration);
        }

        private void ApplyExplosionDamage(Vector3 center, float radius, float dmg)
        {
            ContactFilter2D filter = new()
            {
                useTriggers = true,
                useDepth = false
            };
            filter.SetLayerMask(hitMask);

            int hitCount = Physics2D.OverlapCircle(center, radius, filter, _hits);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = _hits[i];
                if (col == null) continue;
                if (!col.TryGetComponent<HealthComponent>(out var hp)) continue;
                if (hp.IsDead) continue;

                hp.Damage(dmg);
            }
        }
    }
}
