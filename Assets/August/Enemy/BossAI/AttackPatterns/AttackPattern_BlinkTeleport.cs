using System.Collections;
using AugustsUtility.Tween;

using Survivor.Game;
using UnityEngine;

namespace Survivor.Enemy.FSM
{
    [CreateAssetMenu(
        fileName = "New BlinkTeleportPattern",
        menuName = "Defs/Boss Attacks/Blink Teleport")]
    public sealed class AttackPattern_BlinkTeleport : AttackPattern
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

        [Header("Enrage Multipliers")]
        [SerializeField] private float enragedRateMultiplier = 1.25f;
        [SerializeField] private float enragedDistanceMultiplier = 1.25f;

        public override IEnumerator Execute(BossController controller)
        {
            if (controller == null || controller.PlayerTransform == null)
            {
                yield break;
            }

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
                Debug.LogWarning("BlinkTeleport: No SpriteRenderers found to fade.", controller);
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

            int spriteCount = sprites.Length;
            Color[] originalColors = new Color[spriteCount];
            for (int i = 0; i < spriteCount; i++)
            {
                originalColors[i] = sprites[i].color;
            }

            // Fade-out
            AudioManager.Instance?.PlaySFX(blinkSFX);
            if (fadeOutTime > 0f)
            {
                ValueTween<float> fadeOutTween = Tween.TweenValue(
                    1f, 0f, fadeOutTime,
                    alpha =>
                    {
                        for (int i = 0; i < spriteCount; i++)
                        {
                            Color c = originalColors[i];
                            c.a = alpha;
                            sprites[i].color = c;
                        }
                    },
                    Lerp.Get<float>(),
                    EasingFunctions.EaseInQuad
                );

                if (fadeOutTween != null)
                {
                    yield return fadeOutTween; // waits while IsActive
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

            //  Invisible hold (optional)
            if (holdTime > 0f)
            {
                yield return new WaitForSeconds(holdTime);
            }

            // Teleport around player
            TeleportAroundPlayer(controller, distMul);
            AudioManager.Instance?.PlaySFX(appearSFX);

            // Fade-in
            if (fadeInTime > 0f)
            {
                ValueTween<float> fadeInTween = Tween.TweenValue(
                    0f, 1f, fadeInTime,
                    alpha =>
                    {
                        for (int i = 0; i < spriteCount; i++)
                        {
                            Color c = originalColors[i];
                            c.a = alpha;
                            sprites[i].color = c;
                        }
                    },
                    Lerp.Get<float>(),
                    EasingFunctions.EaseOutQuad
                );

                if (fadeInTween != null)
                {
                    yield return fadeInTween;
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

            // Snap back to exact original RGB/alpha
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
