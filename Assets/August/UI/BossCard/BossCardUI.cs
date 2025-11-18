using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Survivor.Game;

namespace Survivor.Progression.UI
{
    [DisallowMultipleComponent]
    public sealed class BossCardUI : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Wiring")]
        [SerializeField] private Button pickButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image highlightFrame;

        private BossDef _boss;
        private Action<BossDef> _onPick;

        public void Configure(BossDef boss, Action<BossDef> onPick)
        {
            _boss = boss;
            _onPick = onPick;

            if (iconImage) iconImage.sprite = boss.Portrait;
            if (titleText) titleText.text = string.IsNullOrEmpty(boss.DisplayName)
                ? boss.name
                : boss.DisplayName;

            SetHighlight(false);

            if (pickButton)
            {
                pickButton.onClick.RemoveAllListeners();
                pickButton.onClick.AddListener(HandlePick);
            }
        }

        private void HandlePick()
        {
            if (_boss == null) return;
            _onPick?.Invoke(_boss);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                HandlePick();
        }

        public void OnSelect(BaseEventData eventData) => SetHighlight(true);
        public void OnDeselect(BaseEventData eventData) => SetHighlight(false);

        private void SetHighlight(bool on)
        {
            if (highlightFrame) highlightFrame.enabled = on;
        }
    }
}
