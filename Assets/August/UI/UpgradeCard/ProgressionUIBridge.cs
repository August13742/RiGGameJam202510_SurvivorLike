using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Survivor.Progression.UI
{
    [DisallowMultipleComponent]
    public sealed class ProgressionUIBridge : MonoBehaviour
    {
        [Header("Refs or Prefabs")]
        [SerializeField] private ProgressionManager progression;   // optional: auto-find
        [SerializeField] private UpgradeMenuUI menu;               // optional: scene ref
        [SerializeField] private Canvas canvas;
        [SerializeField] private UpgradeMenuUI menuPrefab;         // optional: lazy instantiate

        private void Awake()
        {
            if (!progression) progression = FindFirstObjectByType<ProgressionManager>();
        }

        private void OnEnable()
        {
            if (!progression) progression = FindFirstObjectByType<ProgressionManager>();
            if (progression) progression.OfferReady += HandleOfferReady;
        }

        private void OnDisable()
        {
            if (progression) progression.OfferReady -= HandleOfferReady;
        }

        private void HandleOfferReady(UpgradeCardVM[] cards)
        {
            if (cards == null || cards.Length == 0) return;

            if (!menu)
            {
                menu = GetOrCreateMenu();
                if (!menu)
                {
                    Debug.LogError("UpgradeMenuUI could not be created; auto-picking first to avoid soft-lock.");
                    progression.Pick(cards[0].Id);
                    return;
                }
            }

            menu.Show(cards, OnPickFromMenu, "Choose an Upgrade");
        }

        private void OnPickFromMenu(string idOrNull)
        {
            if (string.IsNullOrEmpty(idOrNull)) return; // treat as skip
            progression.Pick(idOrNull);
        }

        private UpgradeMenuUI GetOrCreateMenu()
        {
            // If a scene instance exists (inactive or hidden), use it.
            var existing = FindFirstObjectByType<UpgradeMenuUI>();
            if (existing) return existing;

            if (!menuPrefab)
            {
                Debug.LogWarning("No UpgradeMenuUI prefab assigned; cannot create menu.");
                return null;
            }

            // Ensure Canvas + EventSystem exist
            if (canvas==null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {

                var canvasGO = new GameObject("UpgradeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = canvasGO.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas = c;
            }
            if (!EventSystem.current)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(es); // optional
            }

            // Instantiate menu under the canvas
            var instance = Instantiate(menuPrefab, canvas.transform);
            instance.HideImmediate(); // keep hidden until Show() call
            return instance;
        }
    }
}
