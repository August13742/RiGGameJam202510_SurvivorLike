using System;
using UnityEngine;
using Survivor.Game;
using Survivor.Weapon;

namespace Survivor.Progression
{
    [DisallowMultipleComponent]
    public sealed class ProgressionManager : MonoBehaviour
    {
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
        }
        private void OnDisable()
        {
            if (SessionManager.Instance) SessionManager.Instance.LevelUp -= OnLevelUp;
        }

        private void OnLevelUp()
        {
            var player = SessionManager.Instance.GetPlayerReference();
            var wc = player ? player.GetComponent<WeaponController>() : null;
            var ctx = new ProgressionContext(SessionManager.Instance, player, wc, _history);

            var cards = _builder.BuildOffer(ctx, UpgradePool, cardsPerLevel);
            OfferReady?.Invoke(cards);
        }

        public void Pick(string id)
        {
            var player = SessionManager.Instance.GetPlayerReference();
            var wc = player ? player.GetComponent<WeaponController>() : null;
            var ctx = new ProgressionContext(SessionManager.Instance, player, wc, _history);

            var def = Array.Find(_builder.LastDefs, d => d.Id == id);
            if (!def) return;

            var changes = def.Apply(ctx);
            // You can raise a toast with changes.PreviewLines here.
            OnLevelUp(); // optional: immediate next offer if you chain levels
        }
    }
}
