using AugustsUtility.Tween;
using UnityEngine;
using UnityEngine.UI;

namespace Survivor.VFX
{
    [DisallowMultipleComponent]
    public sealed class ScreenDamageOverlay : MonoBehaviour
    {
        public static ScreenDamageOverlay Instance { get; private set; }

        [Header("Wiring")]
        [Tooltip("Prefab with a RawImage/Image as root. Will be instantiated under a Canvas and stretched full-screen.")]
        [SerializeField] private GameObject overlayPrefab;
        [Tooltip("Optional: explicit canvas to parent the overlay under. If null, first Canvas in scene is used.")]
        [SerializeField] private Canvas targetCanvas;

        [Header("Hit Flash")]
        [SerializeField] private float hitAlpha = 0.5f;
        [SerializeField] private float hitFadeIn = 0.05f;
        [SerializeField] private float hitFadeOut = 0.25f;

        [Header("Death Overlay")]
        [SerializeField] private float deathAlpha = 1.0f;
        [SerializeField] private float deathFadeIn = 0.3f;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupOverlayInstance();
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private void SetupOverlayInstance()
        {
            if (overlayPrefab == null)
            {
                Debug.LogError("[ScreenDamageOverlay] overlayPrefab is not assigned.");
                return;
            }

            // Resolve canvas
            Canvas canvas = targetCanvas;
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("ScreenDamageOverlayCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Instantiate prefab as child of canvas
            GameObject instance = Instantiate(overlayPrefab, canvas.transform, worldPositionStays: false);
            instance.name = "ScreenDamageOverlayInstance";

            // Ensure full-screen stretch
            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = instance.AddComponent<RectTransform>();
            }
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;

            // Optional: make sure it doesn't block clicks (can be toggled in prefab if you want the opposite)
            var graphic = instance.GetComponent<Graphic>();
            if (graphic != null)
                graphic.raycastTarget = false;

            // Ensure CanvasGroup
            _canvasGroup = instance.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = instance.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Quick red flash when the player is damaged.
        /// </summary>
        public void Flash()
        {
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = 0f;

            _canvasGroup.TweenAlpha(hitAlpha, hitFadeIn, null, onComplete: () =>
            {
                _canvasGroup.TweenAlpha(0f, hitFadeOut);
            });
        }

        /// <summary>
        /// Persistent full-red overlay on death.
        /// </summary>
        public void ShowFull()
        {
            if (_canvasGroup == null) return;
            _canvasGroup.TweenAlpha(deathAlpha, deathFadeIn);
        }
    }
}
