using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Survivor.Progression.UI
{
    [DisallowMultipleComponent]
    public sealed class UpgradeMenuUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;                 // Modal root
        [SerializeField] private Transform cardParent;            // LayoutGroup parent
        [SerializeField] private UpgradeCardUI cardPrefab;        // Prefab
        [SerializeField] private TMP_Text headerText;             // Optional: "Choose one" etc.
        [SerializeField] private Button rerollButton;             // Optional (disabled by default)
        [SerializeField] private Button skipButton;               // Optional

        [Header("Behavior")]
        [SerializeField] private bool pauseTimeScale = true;      // Pause game while open
        [SerializeField] private bool clearOnShow = true;

        private readonly List<UpgradeCardUI> _spawned = new();
        private Action<string> _onPick;

        private float _prevTimeScale;
        private bool _isOpen;

        private void Awake()
        {
            HideImmediate();
            if (rerollButton) rerollButton.gameObject.SetActive(false); // placeholder
            if (skipButton)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(() => CloseWithPick(null));
            }
        }

        public void Show(UpgradeCardVM[] vms, Action<string> onPick, string header = "Level Up: Choose One")
        {
            _onPick = onPick;

            if (root) root.SetActive(true);
            if (headerText) headerText.text = header;

            if (clearOnShow) Clear();

            for (int i = 0; i < vms.Length; i++)
            {
                UpgradeCardUI card = Instantiate(cardPrefab, cardParent);
                _spawned.Add(card);
                card.Configure(vms[i], CloseWithPick);
            }

            // Pause & focus
            if (pauseTimeScale)
            {
                _prevTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            _isOpen = true;

            // Ensure keyboard focus lands on first selectable
            if (_spawned.Count > 0)
            {
                var btn = _spawned[0].GetComponentInChildren<Button>();
                if (btn) EventSystem.current.SetSelectedGameObject(btn.gameObject);
            }
        }

        public void HideImmediate()
        {
            if (root) root.SetActive(false);
            _isOpen = false;
        }

        private void Clear()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i]) Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();
        }

        private void CloseWithPick(string idOrNull)
        {
            if (!_isOpen) return;

            // Unpause
            if (pauseTimeScale)
            {
                Time.timeScale = _prevTimeScale;
            }
                // Close UI
                HideImmediate();
            Clear();
            _isOpen = false;

            // Report back to caller
            _onPick?.Invoke(idOrNull);
        }
    }
}
