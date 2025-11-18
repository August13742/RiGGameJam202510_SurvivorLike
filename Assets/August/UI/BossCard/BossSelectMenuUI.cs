using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Survivor.Game;

namespace Survivor.Progression.UI
{
    [DisallowMultipleComponent]
    public sealed class BossSelectMenuUI : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private GameObject root;           // panel root
        [SerializeField] private Transform cardParent;      // layout group parent
        [SerializeField] private BossCardUI cardPrefab;     // boss card prefab
        [SerializeField] private TMP_Text headerText;       // e.g. "Select Boss"
        [SerializeField] private Button backButton;         // "Back" button


        [Header("Navigation")]
        [SerializeField] private GameObject previousMenuRoot; // main menu panel to return to

        private readonly List<BossCardUI> _spawned = new();
        private Action<BossDef> _onPick;
        private bool _isOpen;

        private void Awake()
        {
            HideImmediate();

            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(HandleBack);
            }
        }

        /// <summary>
        /// Show boss selection menu with given bosses.
        /// </summary>
        public void Show(BossDef[] bosses, Action<BossDef> onPick, string header = "Select Boss")
        {
            _onPick = onPick;

            if (previousMenuRoot) previousMenuRoot.SetActive(false);
            if (root) root.SetActive(true);
            if (headerText) headerText.text = header;

            Clear();

            if (bosses != null)
            {
                for (int i = 0; i < bosses.Length; i++)
                {
                    var def = bosses[i];
                    if (!def) continue;

                    BossCardUI card = Instantiate(cardPrefab, cardParent);
                    _spawned.Add(card);
                    card.Configure(def, HandlePickBoss);
                }
            }

            _isOpen = true;

            // focus first button for keyboard / gamepad
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

        private void HandlePickBoss(BossDef boss)
        {
            if (!_isOpen) return;

            // close this menu
            HideImmediate();
            Clear();
            _isOpen = false;

            _onPick?.Invoke(boss);
        }

        private void HandleBack()
        {
            if (!_isOpen) return;

            HideImmediate();
            Clear();
            _isOpen = false;

            // go back Ågone levelÅh
            if (previousMenuRoot) previousMenuRoot.SetActive(true);
        }
    }
}
