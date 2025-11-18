using System;
using UnityEngine;
using Survivor.Game;

namespace Survivor.Progression
{
    [DisallowMultipleComponent]
    public sealed class ProgressionManager : MonoBehaviour
    {
        [Header("Offer Tuning")]
        [SerializeField] private bool isDebugAutoPick = true;
        [SerializeField, Min(1)] private int cardsPerLevel = 3;

        [Header("Authoring Pool")]
        [SerializeField] private UpgradeDef[] UpgradePool;

        private SelectionHistory _history;
        private OfferBuilder _builder;

        public Action<UpgradeCardVM[]> OfferReady; // UI subscribes
        public Action OfferSelected;

        //add
        [SerializeField] private WeaponList weaponList;

        private void Awake()
        {
            _history = new SelectionHistory();
            _builder = new OfferBuilder();
        }

        private void OnEnable()
        {
            var sm = SessionManager.Instance;
            if (sm) sm.LevelUp += OnLevelUp;
            if (isDebugAutoPick) OfferReady += HandleDebugAutoPick;
        }

        private void OnDisable()
        {
            var sm = SessionManager.Instance;
            if (sm) sm.LevelUp -= OnLevelUp;
            if (isDebugAutoPick) OfferReady -= HandleDebugAutoPick;
        }

        private void OnLevelUp()
        {
            var sm = SessionManager.Instance;
            if (!sm)
            {
                Debug.LogWarning("[Progression] No SessionManager.Instance.");
                return;
            }

            var player = sm.GetPlayerReference();
            var dm = player ? player.GetComponent<DroneManager>() : null;
            var ctx = new ProgressionContext(sm, player, dm, _history);

            var cards = _builder.BuildOffer(ctx, UpgradePool, cardsPerLevel);
            if (cards == null || cards.Length == 0)
            {
                Debug.LogWarning("[Progression] No available upgrades in pool.");
                return;
            }

            OfferReady?.Invoke(cards);
        }

        /// Pick by card Id (preferred from UI).
        public void Pick(string id)
        {
            var sm = SessionManager.Instance;
            if (!sm) return;

            var player = sm.GetPlayerReference();
            var dm = player ? player.GetComponent<DroneManager>() : null;
            var ctx = new ProgressionContext(sm, player, dm, _history);

            var def = Array.Find(_builder.LastDefs, d => d && d.Id == id);
            if (!def)
            {
                Debug.LogWarning($"[Progression] Pick failed; unknown Id: {id}");
                return;
            }

            //add
            weaponList.ShowInWeaponList(def.Icon);

            var changes = def.Apply(ctx);
            OfferSelected.Invoke();
            if (changes.PreviewLines != null && changes.PreviewLines.Count > 0)
            {
                Debug.Log($"<color=green>Upgrade Applied: {def.Title}</color>");
                for (int i = 0; i < changes.PreviewLines.Count; i++)
                    Debug.Log($"  - {changes.PreviewLines[i]}");
            }
            else
            {
                Debug.Log($"<color=green>Upgrade Applied: {def.Title}</color> (no details)");
            }
        }

        /// Optional: pick by index from current offer (useful for debug/autopick).
        public void PickIndex(int index)
        {
            if (_builder.LastDefs == null || index < 0 || index >= _builder.LastDefs.Length)
            {
                Debug.LogWarning($"[Progression] PickIndex out of range: {index}");
                return;
            }
            Pick(_builder.LastDefs[index].Id);
        }

        private void HandleDebugAutoPick(UpgradeCardVM[] cards)
        {
            if (cards == null || cards.Length == 0)
            {
                Debug.LogWarning("[Progression] LevelUp fired but offer was empty.");
                return;
            }

            Debug.Log("<color=yellow>--- LEVEL UP OFFER (Auto-picking first) ---</color>");
            for (int i = 0; i < cards.Length; i++)
                Debug.Log($"Offer [{i}]: {cards[i].Title} ({cards[i].Id})");

            string pickedId = cards[0].Id;
            Debug.Log($"<color=cyan>Auto-picking ID: {pickedId}</color>");
            Pick(pickedId);
        }
    }
}
