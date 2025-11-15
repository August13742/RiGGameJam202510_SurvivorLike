using AugustsUtility.Telegraph;
using Survivor.Weapon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New MeteorChannelPattern", menuName = "Defs/Boss Attacks/Meteor Channel")]
    public sealed class AttackPattern_MeteorChannel : AttackPattern
    {
        [Header("Meteor Prefab")]
        [SerializeField] private GameObject meteorPrefab;

        [Header("Pattern Geometry")]
        [SerializeField] private float minRadius = 3f;
        [SerializeField] private float maxRadius = 8f;

        [Header("Wave Configuration")]
        [SerializeField] private int minMeteorsPerWave = 2;
        [SerializeField] private int maxMeteorsPerWave = 4;

        [Header("Telegraph Settings (impact warning)")]
        [SerializeField] private float telegraphRadius = 1.2f;
        [SerializeField] private float telegraphDuration = 1.0f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.75f, 0.2f, 1f);

        [Tooltip("Time AFTER telegraph start before meteors become visible and begin falling. Must be < telegraphDuration.")]
        [SerializeField] private float meteorSpawnLead = 0.4f;

        [Header("Timeline")]
        [Tooltip("Pre-channel time before the first meteor wave (boss animates but no telegraphs yet).")]
        [SerializeField] private float windupSeconds = 0.5f;

        [Tooltip("Delay between the END of one wave's telegraph and the START of the next wave's telegraph.")]
        [SerializeField] private float interWaveDelay = 0.3f;

        [Header("Repetitions")]
        [SerializeField] private bool repIsProbabilistic = false;
        [SerializeField] private int repetitions = 3;                // used if not probabilistic
        [SerializeField, Range(0f, 1f)] private float probDecayPerWave = 0.25f; // p -= this
        [SerializeField] private int hardCap = 6;

        [Header("Meteor Properties")]
        [Tooltip("Base impact damage per meteor (scaled by enrage).")]
        [SerializeField] private float meteorImpactDamage = 25f;
        [SerializeField] private float meteorImpactRadius = 1.2f;
        [SerializeField] private Vector2 meteorSpawnOffset = new Vector2(0f, 10f);

        [Header("Hazard Zone (optional)")]
        [Tooltip("If assigned, a hazard zone will be spawned where each meteor lands.")]
        [SerializeField] private GameObject hazardZonePrefab;
        [SerializeField] private float hazardRadius = 2f;
        [SerializeField] private float hazardDamagePerSecond = 8f;
        [SerializeField] private float hazardLifetime = 3f;

        [Header("Animation (optional)")]
        [SerializeField] private string channelAnim = "Cast";
        [SerializeField] private string releaseAnim = "Shoot";

        [Header("Enrage")]
        [SerializeField] private float enrageRateMul = 1.15f;   // faster timeline (windup, telegraph, gaps)
        [SerializeField] private float enrageMeteorCountMul = 1.25f;
        [SerializeField] private float enrageImpactDamageMul = 1.35f;
        [SerializeField] private float enrageHazardDpsMul = 1.3f;
        [SerializeField] private float enrageHazardSizeMul = 1.5f;
        [SerializeField, Range(0f, 1f)] private float enrageDecayReduction = 0.5f;
        [SerializeField] private int enragedWaveCountBonus = 1; // extra deterministic reps

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || meteorPrefab == null || controller.PlayerTransform == null)
                yield break;

            bool enraged = controller.IsEnraged;
            float rateMul = enraged ? enrageRateMul : 1f;

            // Scale core timings by rate
            float windup = windupSeconds / rateMul;
            float telegraphTime = telegraphDuration / rateMul;
            float spawnLeadTime = meteorSpawnLead / rateMul;
            float waveGap = interWaveDelay / rateMul;

            // Clamp spawnLead so it remains < telegraphTime
            if (spawnLeadTime > telegraphTime * 0.95f)
                spawnLeadTime = telegraphTime * 0.95f;

            // Enrage scalars
            float decay = repIsProbabilistic
                ? probDecayPerWave * (enraged ? enrageDecayReduction : 1f)
                : probDecayPerWave;

            // Meteor count
            int minCount = Mathf.Max(1, Mathf.RoundToInt(minMeteorsPerWave * (enraged ? enrageMeteorCountMul : 1f)));
            int maxCount = Mathf.Max(minCount, Mathf.RoundToInt(maxMeteorsPerWave * (enraged ? enrageMeteorCountMul : 1f)));

            // Damage scaling
            float finalImpactDamage = meteorImpactDamage * (enraged ? enrageImpactDamageMul : 1f);
            float finalHazardDps = hazardDamagePerSecond * (enraged ? enrageHazardDpsMul : 1f);
            float finalHazardSize = hazardRadius * (enraged ? enrageHazardSizeMul : 1f);

            Transform bossTf = controller.transform;

            // Boss channels: no telegraphs yet.
            controller.VelocityOverride = Vector2.zero;
            if (controller.Animator != null && !string.IsNullOrEmpty(channelAnim))
            {
                controller.Animator.Play(channelAnim);
                controller.Animator.speed = rateMul;
            }

            if (windup > 0f)
                yield return new WaitForSeconds(windup);

            // --- Repetitions / Probabilistic waves ---
            if (repIsProbabilistic)
            {
                float p = 1f;
                int guard = 0;
                while (Random.value <= p && guard++ < hardCap)
                {
                    FireOneWave(
                        controller,
                        bossTf.position,
                        minCount,
                        maxCount,
                        telegraphTime,
                        spawnLeadTime,
                        finalImpactDamage,
                        finalHazardDps,
                        finalHazardSize
                    );

                    p = Mathf.Max(0f, p - decay);

                    // Wait until this wave finishes telegraphing before starting the next
                    yield return new WaitForSeconds(telegraphTime + waveGap);
                }
            }
            else
            {
                int reps = Mathf.Max(0, repetitions + (enraged ? enragedWaveCountBonus : 0));
                for (int i = 0; i < reps; i++)
                {
                    FireOneWave(
                        controller,
                        bossTf.position,
                        minCount,
                        maxCount,
                        telegraphTime,
                        spawnLeadTime,
                        finalImpactDamage,
                        finalHazardDps,
                        finalHazardSize
                    );

                    if (i < reps - 1)
                        yield return new WaitForSeconds(telegraphTime + waveGap);
                }
            }

            // After final wave telegraph finishes, play release / exit channel
            if (controller.Animator != null && !string.IsNullOrEmpty(releaseAnim))
            {
                controller.Animator.Play(releaseAnim);
                controller.Animator.speed = rateMul;
            }

            if (controller.Animator != null)
                controller.Animator.speed = 1f;

            controller.VelocityOverride = Vector2.zero;
        }

        private void FireOneWave(
            BossController controller,
            Vector3 bossPos,
            int minCount,
            int maxCount,
            float telegraphTime,
            float spawnLeadTime,
            float impactDamage,
            float hazardDps,
            float hazardSize)
        {
            int meteorCount = Random.Range(minCount, maxCount + 1);
            if (meteorCount <= 0) return;

            // Precompute meteor travel distance (spawnOffset magnitude)
            float travelDistance = meteorSpawnOffset.magnitude;
            // Flight time is telegraphTime - spawnLeadTime → they land as telegraph finishes
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

            // For each impact in this wave:
            foreach (Vector3 impactPos in impactPositions)
            {
                // 1) Impact telegraph only: "this is where the meteor will land"
                Telegraph.Circle(
                    host: controller,
                    pos: impactPos,
                    radius: telegraphRadius,
                    duration: telegraphTime,
                    color: telegraphColor
                );

                // 2) Meteor logic as a coroutine:
                controller.StartCoroutine(
                    MeteorRoutine(
                        impactPos,
                        spawnLeadTime,
                        meteorSpeed,
                        impactDamage,
                        hazardDps,
                        hazardSize
                    )
                );
            }
        }

        private IEnumerator MeteorRoutine(
            Vector2 impactPos,
            float spawnLeadTime,
            float meteorSpeed,
            float impactDamage,
            float hazardDps,
            float hazardSize)
        {
            // Wait until the meteor visual should appear (inside the telegraph window)
            if (spawnLeadTime > 0f)
                yield return new WaitForSeconds(spawnLeadTime);

            GameObject meteorObj = Object.Instantiate(meteorPrefab);
            if (!meteorObj.TryGetComponent<MeteorProjectile2D>(out var meteor))
            {
                Debug.LogError("MeteorProjectile2D component not found on meteorPrefab!");
                Object.Destroy(meteorObj);
                yield break;
            }

            // Configure hazard behaviour for this meteor (optional)
            if (hazardZonePrefab != null && hazardLifetime > 0f && hazardSize > 0f)
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
    }
}
