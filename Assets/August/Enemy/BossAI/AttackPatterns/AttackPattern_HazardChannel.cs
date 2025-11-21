using AugustsUtility.Telegraph;
using Survivor.Weapon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New HazardChannelPattern",
        menuName = "Defs/Boss Attacks/Hazard Channel")]
    public sealed class AttackPattern_HazardChannel : AttackPattern
    {
        [Header("Audio")]
        [SerializeField] private SFXResource windupSFX;      // Initial charge up
        [SerializeField] private SFXResource channelLoopSFX; // While casting
        [SerializeField] private SFXResource waveFireSFX;    // Per wave
        [SerializeField] private SFXResource releaseSFX;     // End animation

        [Header("Hazard Prefab")]
        [Tooltip("Prefab that must contain HazardZone2D.")]
        [SerializeField] private GameObject hazardZonePrefab;

        [Header("Pattern Geometry")]
        [SerializeField] private float minRadius = 3f;
        [SerializeField] private float maxRadius = 8f;

        [Header("Wave Configuration")]
        [SerializeField] private int minHazardsPerWave = 2;
        [SerializeField] private int maxHazardsPerWave = 4;

        [Header("Telegraph Settings (spawn warning)")]
        [SerializeField] private float telegraphRadius = 1.2f;
        [SerializeField] private float telegraphDuration = 1.0f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.75f, 0.2f, 1f);

        [Header("Timeline")]
        [Tooltip("Pre-channel time before the first wave (boss animates but no telegraphs yet).")]
        [SerializeField] private float windupSeconds = 0.5f;

        [Tooltip("Delay between the END of one wave's telegraph and the START of the next wave's telegraph.")]
        [SerializeField] private float interWaveDelay = 0.3f;

        [Header("Repetitions")]
        [Tooltip("If false → fixed count. If true → decaying probability until hardCap.")]
        [SerializeField] private bool repIsProbabilistic = false;

        [SerializeField] private int repetitions = 3;                // used if not probabilistic
        [SerializeField, Range(0f, 1f)] private float probDecayPerWave = 0.25f; // p -= this
        [SerializeField] private int hardCap = 6;

        [Header("Hazard Parameters")]
        [SerializeField] private bool hazardOnlyEnraged = true;
        [SerializeField] private float hazardRadius = 2f;
        [SerializeField] private float hazardDamagePerSecond = 8f;
        [SerializeField] private float hazardLifetime = 3f;

        [Header("Animation (optional)")]
        [SerializeField] private string channelAnim = "Cast";
        [SerializeField] private string releaseAnim = "Shoot";

        [Header("Enrage")]
        [SerializeField] private float enrageRateMul = 1.15f;   // faster timeline (windup, telegraph, gaps)
        [SerializeField] private float enrageHazardDpsMul = 1.3f;
        [SerializeField] private float enrageHazardSizeMul = 1.5f;
        [SerializeField, Range(0f, 1f)] private float enrageDecayReduction = 0.5f;
        [SerializeField] private int enragedWaveCountBonus = 1; // extra deterministic reps

        private bool _isEnraged;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || hazardZonePrefab == null || controller.PlayerTransform == null)
                yield break;

            _isEnraged = controller.IsEnraged;
            float rateMul = _isEnraged ? enrageRateMul : 1f;

            // Scale core timings by rate
            float windup = windupSeconds / Mathf.Max(rateMul, 0.01f);
            float telegraphTime = telegraphDuration / Mathf.Max(rateMul, 0.01f);
            float waveGap = interWaveDelay / Mathf.Max(rateMul, 0.01f);

            // Enrage scalars
            float decay = repIsProbabilistic
                ? probDecayPerWave * (_isEnraged ? enrageDecayReduction : 1f)
                : probDecayPerWave;

            // Hazard count per wave
            int minCount = Mathf.Max(1, minHazardsPerWave);
            int maxCount = Mathf.Max(minCount, maxHazardsPerWave);

            // Hazard scaling
            float finalHazardDps = hazardDamagePerSecond * (_isEnraged ? enrageHazardDpsMul : 1f);
            float finalHazardSize = hazardRadius * (_isEnraged ? enrageHazardSizeMul : 1f);

            bool hazardsActive =
                hazardZonePrefab != null &&
                hazardLifetime > 0f &&
                finalHazardSize > 0f &&
                (!hazardOnlyEnraged || _isEnraged);

            Transform bossTf = controller.transform;

            AudioManager.Instance?.PlaySFX(windupSFX, bossTf.position);
            AudioHandle channelHandle = AudioManager.Instance?.PlaySFX(channelLoopSFX, bossTf.position) ?? AudioHandle.Invalid;

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
                    AudioManager.Instance.PlaySFX(waveFireSFX, bossTf.position, bossTf);
                    FireOneWave(
                        controller,
                        bossTf.position,
                        minCount,
                        maxCount,
                        telegraphTime,
                        finalHazardDps,
                        finalHazardSize,
                        hazardsActive
                    );

                    p = Mathf.Max(0f, p - decay);

                    // Wait until this wave finishes telegraphing before starting the next
                    yield return new WaitForSeconds(telegraphTime + waveGap);
                }
            }
            else
            {
                int reps = Mathf.Max(0, repetitions + (_isEnraged ? enragedWaveCountBonus : 0));
                for (int i = 0; i < reps; i++)
                {
                    AudioManager.Instance.PlaySFX(waveFireSFX, bossTf.position, bossTf);
                    FireOneWave(
                        controller,
                        bossTf.position,
                        minCount,
                        maxCount,
                        telegraphTime,
                        finalHazardDps,
                        finalHazardSize,
                        hazardsActive
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

            channelHandle.Stop();
            AudioManager.Instance?.PlaySFX(releaseSFX, bossTf.position);
            controller.VelocityOverride = Vector2.zero;
        }

        private void FireOneWave(
            BossController controller,
            Vector3 bossPos,
            int minCount,
            int maxCount,
            float telegraphTime,
            float hazardDps,
            float hazardSize,
            bool hazardsActive)
        {
            int count = Random.Range(minCount, maxCount + 1);
            if (count <= 0) return;

            List<Vector3> positions = new List<Vector3>(count);

            for (int i = 0; i < count; i++)
            {
                float r = Random.Range(minRadius, maxRadius);
                float angRad = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angRad), Mathf.Sin(angRad), 0f) * r;
                positions.Add(bossPos + offset);
            }

            foreach (Vector3 pos in positions)
            {
                // Telegraph: "hazard will appear here after telegraphTime"
                Telegraph.Circle(
                    host: controller,
                    pos: pos,
                    radius: telegraphRadius,
                    duration: telegraphTime,
                    color: telegraphColor
                );

                if (hazardsActive)
                {
                    controller.StartCoroutine(
                        SpawnHazardAfterDelay(
                            pos,
                            telegraphTime,
                            hazardSize,
                            hazardDps
                        )
                    );
                }
            }
        }

        private IEnumerator SpawnHazardAfterDelay(
            Vector2 pos,
            float delay,
            float hazardSize,
            float hazardDps)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            GameObject hzObj = Object.Instantiate(hazardZonePrefab, pos, Quaternion.identity);
            if (!hzObj.TryGetComponent<HazardZone2D>(out var hz))
            {
                Debug.LogError("HazardZone2D component not found on hazardZonePrefab!");
                Object.Destroy(hzObj);
                yield break;
            }

            hz.Activate(
                pos,
                r: hazardSize,
                dps: hazardDps,
                life: hazardLifetime
            );
        }
    }
}
