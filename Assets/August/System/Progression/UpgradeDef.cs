using UnityEngine;

namespace Survivor.Progression
{
    public enum UpgradeKind
    {
        WeaponUnlock,
        WeaponLevel,
        PlayerStat,
        WeaponMod,
        Utility // reroll, heal, gold, etc.
    }
    public abstract class UpgradeDef : ScriptableObject
    {
        [Header("Meta")]
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public Sprite Icon;
        public Rarity Rarity;
        public UpgradeKind Kind;
        public bool IsInfinite;     // true for repeatable stat bumps
        public int MaxPicks = 1;    // caps finite upgrades

        [Header("Weights")]
        public float BaseWeight = 1f;
        public WeightRule[] Rules;

        public virtual bool IsAvailable(ProgressionContext ctx) => !ctx.History.IsCapped(Id);
        public float ComputeWeight(ProgressionContext ctx)
        {
            if (!IsAvailable(ctx)) return 0f;
            float w = BaseWeight;
            if (Rules != null) foreach (var r in Rules) if (r) w *= r.GetMultiplier(ctx, this);
            // diminishing return: penalise repeated picks of same Id
            int n = ctx.History.Count(Id);
            if (n > 0) w *= 1f / (1f + 0.5f * n);
            return Mathf.Max(0f, w);
        }

        // Apply and return a ChangeSet for UI preview logging.
        public abstract ChangeSet Apply(ProgressionContext ctx);
    }
}
