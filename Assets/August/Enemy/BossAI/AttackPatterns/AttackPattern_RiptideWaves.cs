using AugustsUtility.CameraShake;
using AugustsUtility.Telegraph;
using AugustsUtility.Tween;
using Survivor.Control;
using Survivor.Game;
using System.Collections;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(fileName = "New RiptideWavesPattern", menuName = "Defs/Boss Attacks/Riptide Waves")]
    public sealed class AttackPattern_RiptideWaves : AttackPattern
    {
        #region Fields
        [Header("Camera Shake")]
        [SerializeField] private float cameraShakeStrength = 3f;
        [SerializeField] private float cameraShakeDuration = 0.3f;

        [Header("Wave Count")]
        [SerializeField] private int minWaveCount = 3;
        [SerializeField] private int maxWaveCount = 7;

        [Header("Geometry")]
        [SerializeField] private float waveBaseDistance = 5f;
        [SerializeField] private float segmentSpacing = 3f;
        [SerializeField] private float blastRadius = 1.8f;
        [SerializeField] private float spreadAngle = 60f;

        [Header("Prediction")]
        [Tooltip("How many times to sample player input during the wave spawning phase. 1 = snapshot at start. >1 = resamples periodically.")]
        [SerializeField] private int inputResampleCount = 2;
        [Tooltip("How strongly the wave leads the player. 0 = aims at current position, 1 = aims at fully predicted position.")]
        [SerializeField, Range(0f, 1f)] private float predictionStrength = 0.8f;
        [Tooltip("The assumed speed of the player when calculating the lead.")]
        [SerializeField] private float assumedPlayerSpeed = 7f;

        [Header("Timing")]
        [SerializeField] private float waveInterval = 0.4f;
        [SerializeField] private float telegraphDuration = 0.6f;

        [Header("Damage")]
        [SerializeField] private float damage = 8f;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private bool showVFX;
        [SerializeField] private GameObject VFXPrefab;

        [Header("Telegraph Visual")]
        [SerializeField] private Color telegraphColor = Color.cyan;

        [Header("Channeling")]
        [SerializeField] private string channelAnim = "Cast";
        [SerializeField] private float channelDuration = 1.0f;

        [Header("Enrage")]
        [SerializeField] private float enragedRateMul = 1.25f;
        [SerializeField] private float enragedDamageMul = 1.3f;
        [SerializeField] private float enragedResampleMultiplier = 1.5f;

        private static readonly Collider2D[] _hits = new Collider2D[16];
        #endregion

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null) yield break;

            // --- Setup based on enraged state ---
            bool enraged = controller.IsEnraged;
            float rateMul = enraged ? enragedRateMul : 1f;
            float dmg = damage * (enraged ? enragedDamageMul : 1f);
            float currentTelegraphTime = telegraphDuration / rateMul;
            float currentWaveInterval = waveInterval / rateMul;
            int actualResampleCount = enraged ? Mathf.FloorToInt(inputResampleCount * enragedResampleMultiplier) : inputResampleCount;

            int waveCount = Random.Range(minWaveCount, maxWaveCount + 1);
            PlayerController playerController = controller.PlayerTransform.GetComponent<PlayerController>();
            Vector2 bossPos = controller.BehaviorPivotWorld;

            // --- Resampling Setup ---
            float totalSpawnDuration = (waveCount - 1) * currentWaveInterval;
            float resampleInterval = (actualResampleCount > 1) ? totalSpawnDuration / (actualResampleCount - 1) : float.PositiveInfinity;
            float nextResampleTime = 0f;

            // --- Snapshot State
            Vector2 snapshotPlayerPos = Vector2.zero;
            Vector2 snapshotInputDir = Vector2.zero;

            for (int i = 0; i < waveCount; i++)
            {
                float currentTime = i * currentWaveInterval;

                // --- Check if it's time to take a new input snapshot ---
                if (currentTime >= nextResampleTime)
                {
                    snapshotPlayerPos = playerController.transform.position;
                    snapshotInputDir = playerController.InputDirection;
                    nextResampleTime += resampleInterval;
                }

                // --- Pre-calculate the path for this specific wave using the LATEST snapshot ---
                float delay = currentTime;
                float distance = waveBaseDistance + (segmentSpacing * i);
                float normalizedIndex = waveCount > 1 ? (float)i / (waveCount - 1) : 0.5f;
                float angleOffset = Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, normalizedIndex);

                // 1. Calculate the START position (aimed at player's initial T=0 position for a smooth curve)
                Vector2 initialDirToPlayer = ((Vector2)playerController.transform.position - bossPos).normalized;
                Quaternion startRotation = Quaternion.Euler(0, 0, angleOffset);
                Vector2 startDir = startRotation * initialDirToPlayer;
                Vector2 startPos = bossPos + startDir * distance;

                // 2. Calculate the PREDICTED END position (using the most recent snapshot)
                float timeToImpact = delay + currentTelegraphTime; // This is how far in the future we predict
                Vector2 predictedPlayerPos = snapshotPlayerPos + (assumedPlayerSpeed * timeToImpact * snapshotInputDir);
                Vector2 centralPredictedDir = (predictedPlayerPos - bossPos).normalized;


                // Rotate the central predicted direction to get this wave's final direction.
                // This maintains the fan shape's integrity regardless of how much the aim changes.
                Quaternion endRotation = Quaternion.Euler(0, 0, angleOffset);
                Vector2 finalDir = endRotation * centralPredictedDir;
                Vector2 predictedEndPos = bossPos + finalDir * distance;

                // 3. Blend the final position based on prediction strength
                Vector2 finalEndPos = Vector2.Lerp(startPos, predictedEndPos, predictionStrength);

                // 4. Start the coroutine with the pre-calculated path
                controller.StartCoroutine(
                    RiptideWaveRoutine(controller, delay, currentTelegraphTime, blastRadius, dmg, startPos, finalEndPos)
                );
            }


            float channel = channelDuration / rateMul;
            if (channel > 0f && controller.Animator != null && !string.IsNullOrEmpty(channelAnim))
            {
                controller.VelocityOverride = Vector2.zero;
                controller.Animator.Play(channelAnim);
                controller.Animator.speed = rateMul;
                yield return new WaitForSeconds(channel);
                controller.Animator.Play("Idle");
                controller.Animator.speed = 1f;
                controller.VelocityOverride = Vector2.zero;
            }
        }

        private IEnumerator RiptideWaveRoutine(
            BossController controller, float delay, float telegraphTime,
            float radius, float damage, Vector2 startPos, Vector2 endPos)
        {
            // This coroutine is now "dumb" - it only executes the path it's given.
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (controller == null) yield break;

            Vector2 interpolatedWavePos = startPos;
            Tween.TweenValue(startPos, endPos, telegraphTime, pos => interpolatedWavePos = pos, Lerp.Get<Vector2>(), EasingFunctions.EaseOutQuad);

            Telegraph.Circle(controller, () => interpolatedWavePos, radius, telegraphTime, telegraphColor);

            yield return new WaitForSeconds(telegraphTime);

            if (controller == null) yield break;

            if (showVFX)
            {
                if (VFXPrefab == null)
                {
                    VFX.VFXManager.Instance.ShowHitEffect(interpolatedWavePos);

                }
                else
                {
                    GameObject vfx = Instantiate(VFXPrefab);
                    vfx.transform.position = interpolatedWavePos;
                }
            }

            ContactFilter2D filter = new() { useTriggers = true, useDepth = false };
            filter.SetLayerMask(hitMask);
            int hitCount = Physics2D.OverlapCircle(interpolatedWavePos, radius, filter, _hits);
            for (int i = 0; i < hitCount; i++)
            {
                if (_hits[i] != null && _hits[i].TryGetComponent<HealthComponent>(out var hp) && !hp.IsDead)
                {
                    hp.Damage(damage);
                    Debug.Log(hp.name);
                    CameraShake2D.Shake(cameraShakeDuration, cameraShakeStrength);
                    
                }
            }
        }
    }
}