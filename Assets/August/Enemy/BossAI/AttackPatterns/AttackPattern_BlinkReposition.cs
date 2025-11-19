using System.Collections;
using AugustsUtility.Tween;
using AugustsUtility.AudioSystem;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New BlinkRepositionPattern",
        menuName = "Defs/Boss Attacks/Blink Reposition")]
    public sealed class AttackPattern_BlinkReposition : AttackPattern
    {
        [Header("SFX")]
        [SerializeField] private SFXResource blinkSFX;
        [SerializeField] private SFXResource appearSFX;

        [Header("Blink Distances (around player)")]
        [SerializeField] private float minBlinkDistance = 4f;
        [SerializeField] private float maxBlinkDistance = 8f;

        [Header("Timing")]
        [SerializeField] private float fadeOutDuration = 0.4f;
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float invisibleHoldDuration = 0.1f;

        [Header("Ring Interaction(Optional)")]
        [Tooltip("Radius level to contract to during blink (see RotatingRingHazard.radiusLevels). " +
                 "0 is assumed to be 'base' size.")]
        [SerializeField] private int contractedRadiusLevel = 2;

        [Header("Enrage Multipliers")]
        [SerializeField] private float enragedRateMultiplier = 1.25f;
        [SerializeField] private float enragedDistanceMultiplier = 1.25f;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null)
            {
                yield break;
            }

            // --- Find ring (optional) ---
            RotatingRingHazard ring = null;
            if (controller.Visuals != null)
            {
                ring = controller.Visuals.GetComponentInChildren<RotatingRingHazard>();
            }
            if (ring == null)
            {
                ring = controller.GetComponentInChildren<RotatingRingHazard>();
            }
            bool hasRing = ring != null;

            // --- Collect sprite renderers to fade ---
            SpriteRenderer[] sprites = null;
            if (controller.Visuals != null)
            {
                sprites = controller.Visuals.GetComponentsInChildren<SpriteRenderer>();
            }
            if (sprites == null || sprites.Length == 0)
            {
                sprites = controller.GetComponentsInChildren<SpriteRenderer>();
            }
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning("BlinkReposition: No SpriteRenderers found to fade.", controller);
                yield break;
            }

            bool enraged = controller.IsEnraged;
            float rateMul = enraged ? enragedRateMultiplier : 1f;
            float distMul = enraged ? enragedDistanceMultiplier : 1f;

            float fadeOutTime = fadeOutDuration / rateMul;
            float fadeInTime = fadeInDuration / rateMul;
            float holdTime = invisibleHoldDuration / rateMul;

            // --- 0. Prep: stop movement and normalise anim ---
            controller.Velocity = Vector2.zero;
            controller.VelocityOverride = Vector2.zero;
            controller.Direction = Vector2.zero;

            if (controller.Animator != null)
            {
                controller.Animator.Play("Idle");
                controller.Animator.speed = 1f;
            }

            // Cache original colours
            int spriteCount = sprites.Length;
            Color[] originalColors = new Color[spriteCount];
            for (int i = 0; i < spriteCount; i++)
            {
                originalColors[i] = sprites[i].color;
            }

            // --- 1. Fade-out (+ contract ring via tween, if present) ---
            AudioManager.Instance?.PlaySFX(blinkSFX);
            if (hasRing && fadeOutTime > 0f)
            {
                controller.StartCoroutine(
                    ring.TweenToLevelRoutine(
                        contractedRadiusLevel,
                        fadeOutTime,
                        EasingFunctions.EaseInQuad
                    ));
            }
            else if (hasRing && fadeOutTime <= 0f)
            {
                ring.SnapToLevel(contractedRadiusLevel);
            }

            if (fadeOutTime > 0f)
            {
                float t = 0f;
                while (t < fadeOutTime)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / fadeOutTime);
                    float alpha = Mathf.Lerp(1f, 0f, u);

                    for (int i = 0; i < spriteCount; i++)
                    {
                        Color c = originalColors[i];
                        c.a = alpha;
                        sprites[i].color = c;
                    }

                    yield return null;
                }
            }
            else
            {
                // instant hide
                for (int i = 0; i < spriteCount; i++)
                {
                    Color c = originalColors[i];
                    c.a = 0f;
                    sprites[i].color = c;
                }
            }

            // --- 2. Invisible hold (optional) ---
            if (holdTime > 0f)
            {
                yield return new WaitForSeconds(holdTime);
            }

            // --- 3. Teleport around player (via BehaviourPivotWorld) ---
            TeleportAroundPlayer(controller, distMul);
            AudioManager.Instance?.PlaySFX(appearSFX);
            // --- 4. Fade-in (+ expand ring back to base level 0, if present) ---
            if (hasRing && fadeInTime > 0f)
            {
                controller.StartCoroutine(
                    ring.TweenToLevelRoutine(
                        0,                      // assume level 0 = base
                        fadeInTime,
                        EasingFunctions.EaseOutQuad
                    ));
            }
            else if (hasRing && fadeInTime <= 0f)
            {
                ring.SnapToLevel(0);
            }

            if (fadeInTime > 0f)
            {
                float t = 0f;
                while (t < fadeInTime)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / fadeInTime);
                    float alpha = Mathf.Lerp(0f, 1f, u);

                    for (int i = 0; i < spriteCount; i++)
                    {
                        Color c = originalColors[i];
                        c.a = alpha;
                        sprites[i].color = c;
                    }

                    yield return null;
                }
            }
            else
            {
                // instant show
                for (int i = 0; i < spriteCount; i++)
                {
                    Color c = originalColors[i];
                    c.a = 1f;
                    sprites[i].color = c;
                }
            }

            // Ensure exact original colours
            for (int i = 0; i < spriteCount; i++)
            {
                sprites[i].color = originalColors[i];
            }

            controller.Velocity = Vector2.zero;
            controller.VelocityOverride = Vector2.zero;
            controller.Direction = Vector2.zero;

            if (controller.Animator != null)
            {
                controller.Animator.Play("Idle");
                controller.Animator.speed = 1f;
            }
        }

        private void TeleportAroundPlayer(BossController controller, float distanceMultiplier)
        {
            Transform playerTf = controller.PlayerTransform;
            if (playerTf == null) return;

            float minDist = Mathf.Max(0.1f, minBlinkDistance * distanceMultiplier);
            float maxDist = Mathf.Max(minDist, maxBlinkDistance * distanceMultiplier);

            float angleRad = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(minDist, maxDist);

            Vector2 targetPivot = (Vector2)playerTf.position +
                                  new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * dist;

            Vector2 currentPivot = controller.BehaviorPivotWorld;
            Vector2 rootPos = controller.transform.position;
            Vector2 pivotOffset = rootPos - currentPivot;

            Vector2 newRootPos = targetPivot + pivotOffset;

            if (controller.RB != null)
            {
                controller.RB.position = newRootPos;
            }

            controller.transform.position = newRootPos;
        }
    }
}
