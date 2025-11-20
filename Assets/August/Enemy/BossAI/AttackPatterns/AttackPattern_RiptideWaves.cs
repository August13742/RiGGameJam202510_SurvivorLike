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
        [Header("SFX")]
        [SerializeField] private SFXResource fireSFX;
        [SerializeField] private SFXResource detonateSFX;
        [Header("Camera Shake")]
        [SerializeField] private float cameraShakeStrength = 3f;
        [SerializeField] private float cameraShakeDuration = 0.3f;

        [Header("Wave Count")]
        [SerializeField] private int minWaveCount = 3;
        [SerializeField] private int maxWaveCount = 7;

        [Header("Geometry")]
        [SerializeField] private float waveBaseDistance = 5f;
        [SerializeField] private float blastRadius = 1.8f;

        [Header("Chaining / Homing")]
        [SerializeField] private float stepSize = 3f;                   // distance between ring centers
        [SerializeField] private float maxTurnPerWaveDeg = 40f;         // max heading change per ring
        [SerializeField, Range(0f, 1f)] private float majorDirWeight = 0.7f;
        [SerializeField, Range(0f, 1f)] private float inputDirWeight = 0.2f;
        [SerializeField, Range(0f, 1f)] private float errorDirWeight = 0.3f;

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
        [SerializeField] private float enragedCountMul = 2f;

        private static readonly Collider2D[] _hits = new Collider2D[16];
        #endregion
    

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null)
                yield break;
            AudioManager.Instance?.PlaySFX(fireSFX);
            bool enraged = controller.IsEnraged;
            float rateMul = enraged ? enragedRateMul : 1f;

            // Fire-and-forget the *real* riptide driver.
            controller.StartCoroutine(RiptideDriver(controller, enraged, rateMul));

            // Fixed channel window (what actually �glocks�h the boss / plays anim)
            float channel = channelDuration / rateMul;
            if (channel > 0f && controller.Animator != null && !string.IsNullOrEmpty(channelAnim))
            {
                controller.VelocityOverride = Vector2.zero;
                controller.Animator.Play(channelAnim);
                controller.Animator.speed = rateMul;

                // Boss is considered casting only for this fixed window.
                yield return new WaitForSeconds(channel);

                controller.Animator.Play("Idle");
                controller.Animator.speed = 1f;
                controller.VelocityOverride = Vector2.zero;
            }
        }
        private IEnumerator RiptideDriver(BossController controller, bool enraged, float rateMul)
        {
            if (controller == null || controller.PlayerTransform == null)
                yield break;

            float dmg = damage * (enraged ? enragedDamageMul : 1f);
            float currentTelegraphTime = telegraphDuration / rateMul;
            float currentWaveInterval = waveInterval / rateMul;

            int waveCount = Random.Range(minWaveCount, maxWaveCount + 1);
            if (enraged) waveCount = (int)(enragedCountMul * waveCount);

            PlayerController playerController =
                controller.PlayerTransform.GetComponent<PlayerController>();
            if (playerController == null)
                yield break;

            Vector2 bossPos = controller.BehaviorPivotWorld;

            // --- Initial heading and center ---
            Vector2 toPlayer0 = (Vector2)playerController.transform.position - bossPos;
            if (toPlayer0.sqrMagnitude < 0.0001f)
                toPlayer0 = Vector2.right;
            toPlayer0.Normalize();

            float majorAngle = Mathf.Atan2(toPlayer0.y, toPlayer0.x) * Mathf.Rad2Deg;
            Vector2 currentCenter = bossPos + toPlayer0 * waveBaseDistance;

            float step = stepSize * (enraged ? enragedRateMul : 1f);
            float maxTurn = maxTurnPerWaveDeg * (enraged ? enragedRateMul : 1f);

            for (int i = 0; i < waveCount; i++)
            {
                // 1) sample current player state
                Vector2 playerPos = playerController.transform.position;
                Vector2 inputDir = playerController.InputDirection;
                if (inputDir.sqrMagnitude > 0.0001f)
                    inputDir.Normalize();
                else
                    inputDir = Vector2.zero;

                Vector2 errorDir = playerPos - currentCenter;
                if (errorDir.sqrMagnitude > 0.0001f)
                    errorDir.Normalize();
                else
                    errorDir = Vector2.zero;

                Vector2 majorDir = AngleToDir(majorAngle);

                // 2) blended desired direction
                Vector2 desired = Vector2.zero;
                desired += majorDir * majorDirWeight;
                desired += inputDir * inputDirWeight;
                desired += errorDir * errorDirWeight;

                if (desired.sqrMagnitude < 0.0001f)
                    desired = majorDir;
                desired.Normalize();

                // 3) clamp heading change
                float targetAngle = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(majorAngle, targetAngle);
                delta = Mathf.Clamp(delta, -maxTurn, maxTurn);

                majorAngle += delta;
                Vector2 finalDir = AngleToDir(majorAngle);

                // 4) step
                Vector2 nextCenter = currentCenter + finalDir * step;

                // 5) fire telegraph tween for this ring
                controller.StartCoroutine(
                    RiptideWaveRoutine(
                        controller,
                        delay: 0f,
                        telegraphTime: currentTelegraphTime,
                        radius: blastRadius,
                        damage: dmg,
                        startPos: currentCenter,
                        endPos: nextCenter
                    )
                );

                currentCenter = nextCenter;

                // 6) real-time chaining: wait before next sample
                if (i < waveCount - 1)
                    yield return new WaitForSeconds(currentWaveInterval);
            }
        }

        // helper
        private static Vector2 AngleToDir(float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
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
            AudioManager.Instance?.PlaySFX(detonateSFX);

            ContactFilter2D filter = new() { useTriggers = true, useDepth = false };
            filter.SetLayerMask(hitMask);
            int hitCount = Physics2D.OverlapCircle(interpolatedWavePos, radius, filter, _hits);
            for (int i = 0; i < hitCount; i++)
            {
                if (_hits[i] != null && _hits[i].TryGetComponent<HealthComponent>(out var hp) && !hp.IsDead)
                {
                    hp.Damage(damage, interpolatedWavePos);
                    CameraShake2D.Shake(cameraShakeDuration, cameraShakeStrength);
                    
                }
            }
        }
    }
}