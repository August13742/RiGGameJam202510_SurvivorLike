using AugustsUtility.Tween;
using UnityEngine;

namespace Survivor.VFX
{
    [DisallowMultipleComponent]
    public sealed class ScreenDamageOverlay : MonoBehaviour
    {
        public static ScreenDamageOverlay Instance { get; private set; }

        [Header("Wiring")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Hit Flash")]
        [SerializeField] private float hitAlpha = 0.5f;
        [SerializeField] private float hitFadeIn = 0.05f;
        [SerializeField] private float hitFadeOut = 0.25f;

        [Header("Death Overlay")]
        [SerializeField] private float deathAlpha = 1.0f;
        [SerializeField] private float deathFadeIn = 0.3f;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (!canvasGroup)
                canvasGroup = GetComponentInChildren<CanvasGroup>();

            if (canvasGroup)
                canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Quick red flash when the player is damaged.
        /// </summary>
        public void Flash()
        {
            if (!canvasGroup) return;

            // Reset to transparent immediately
            canvasGroup.alpha = 0f;

            // Ease-in then fade out
            canvasGroup.TweenAlpha(hitAlpha, hitFadeIn, null, onComplete: () =>
            {
                canvasGroup.TweenAlpha(0f, hitFadeOut);
            });
        }

        /// <summary>
        /// Persistent full-red overlay on death.
        /// </summary>
        public void ShowFull()
        {
            if (!canvasGroup) return;
            canvasGroup.TweenAlpha(deathAlpha, deathFadeIn);
        }
    }
}
