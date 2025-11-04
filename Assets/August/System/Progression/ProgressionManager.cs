using System;
using UnityEngine;
using Survivor.Game;

namespace Survivor.Progression
{
    [DisallowMultipleComponent]
    public sealed class ProgressionManager : MonoBehaviour
    {
        [SerializeField] private bool isDebugAutoPick = true;
        [SerializeField] private int cardsPerLevel = 3;
        [SerializeField] private UpgradeDef[] UpgradePool;  // authoring
        private SelectionHistory _history;
        private OfferBuilder _builder;

        public Action<UpgradeCardVM[]> OfferReady; // UI subscribes

        private void Awake()
        {
            _history = new SelectionHistory();
            _builder = new OfferBuilder();
        }

        private void OnEnable()
        {
            SessionManager.Instance.LevelUp += OnLevelUp;
            if (isDebugAutoPick) OfferReady += HandleDebugAutoPick;
        }
        private void OnDisable()
        {
            if (SessionManager.Instance) SessionManager.Instance.LevelUp -= OnLevelUp;
            if (isDebugAutoPick) OfferReady -= HandleDebugAutoPick;
        }

        private void OnLevelUp()
        {
            var player = SessionManager.Instance.GetPlayerReference();
            var dm = player ? player.GetComponent<DroneManager>() : null;
            var ctx = new ProgressionContext(SessionManager.Instance, player, dm, _history);

            var cards = _builder.BuildOffer(ctx, UpgradePool, cardsPerLevel);
            OfferReady?.Invoke(cards);
        }

        public void Pick(string id)
        {
            var player = SessionManager.Instance.GetPlayerReference();
            var dm = player ? player.GetComponent<DroneManager>() : null;
            var ctx = new ProgressionContext(SessionManager.Instance, player, dm, _history);

            var def = Array.Find(_builder.LastDefs, d => d.Id == id);
            if (!def) return;

            var changes = def.Apply(ctx);

            Debug.Log($"<color=green>Upgrade Applied: {def.Title}</color>");
            foreach (var line in changes.PreviewLines)
            {
                Debug.Log($"  - {line}");
            }
        }

        private void HandleDebugAutoPick(UpgradeCardVM[] cards)
        {
            if (cards == null || cards.Length == 0)
            {
                Debug.LogWarning("Level Up triggered, but no available upgrades were found in the pool.");
                return;
            }

            Debug.Log($"<color=yellow>--- LEVEL UP OFFER (Auto-picking first) ---</color>");
            for (int i = 0; i < cards.Length; i++)
            {
                Debug.Log($"Offer [{i}]: {cards[i].Title} ({cards[i].Id})");
            }

            // Automatically pick the first card in the offer.
            string pickedId = cards[0].Id;
            Debug.Log($"<color=cyan>Auto-picking ID: {pickedId}</color>");
            Pick(pickedId);
        }
    }
}
