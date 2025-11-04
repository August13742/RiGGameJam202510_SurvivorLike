using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Survivor.Progression.UI
{
    [DisallowMultipleComponent]
    public sealed class UpgradeCardUI : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Wiring")]
        [SerializeField] private Button pickButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private TMP_Text previewLinesText;

        [Header("Visuals")]
        [SerializeField] private CanvasGroup disabledGroup;
        [SerializeField] private Image highlightFrame;
        [SerializeField] private Color commonColor = new(0.85f, 0.85f, 0.85f);
        [SerializeField] private Color uncommonColor = new(0.55f, 0.85f, 0.55f);
        [SerializeField] private Color rareColor = new(0.7f, 0.7f, 1.0f);

        private UpgradeCardVM _vm;
        private Action<string> _onPick;

        public void Configure(UpgradeCardVM vm, Action<string> onPick)
        {
            _vm = vm;
            _onPick = onPick;

            if (iconImage) iconImage.sprite = vm.Icon;
            if (titleText) titleText.text = vm.Title;
            if (descText) descText.text = vm.Description ?? string.Empty;

            if (rarityText)
            {
                rarityText.text = vm.Rarity.ToString();
                rarityText.color = vm.Rarity switch
                {
                    Rarity.Common => commonColor,
                    Rarity.Uncommon => uncommonColor,
                    Rarity.Rare => rareColor,
                    _ => Color.white
                };
            }

            if (previewLinesText)
                previewLinesText.text = (vm.PreviewLines == null || vm.PreviewLines.Length == 0)
                    ? string.Empty
                    : string.Join("\n", vm.PreviewLines);

            bool isDisabled = vm.IsDisabled;
            if (disabledGroup)
            {
                disabledGroup.alpha = isDisabled ? 0.6f : 0f;
                disabledGroup.blocksRaycasts = isDisabled;
                disabledGroup.interactable = !isDisabled;
            }
            if (pickButton)
            {
                pickButton.interactable = !isDisabled;
                pickButton.onClick.RemoveAllListeners();
                pickButton.onClick.AddListener(HandlePick);
            }

            SetHighlight(false);
        }

        private void HandlePick()
        {
            if (_vm == null || _vm.IsDisabled) return;
            _onPick?.Invoke(_vm.Id);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) HandlePick();
        }

        public void OnSelect(BaseEventData eventData) { SetHighlight(true); }
        public void OnDeselect(BaseEventData eventData) { SetHighlight(false); }

        private void SetHighlight(bool on)
        {
            if (highlightFrame) highlightFrame.enabled = on;
        }
    }
}
