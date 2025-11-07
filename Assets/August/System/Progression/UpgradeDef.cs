using UnityEngine;

namespace Survivor.Progression
{
    public abstract class UpgradeAction : ScriptableObject
    {
        public abstract bool IsAvailable(ProgressionContext ctx, UpgradeDef card);
        public abstract string[] GetPreviewLines(ProgressionContext ctx, UpgradeDef card);
        public abstract ChangeSet Apply(ProgressionContext ctx, UpgradeDef card);
    }

    public enum UpgradeKind { WeaponUnlock, WeaponLevel, PlayerStat, WeaponMod, Utility }

    [CreateAssetMenu(menuName = "Defs/Progression/Upgrades/Card")]
    public sealed class UpgradeDef : ScriptableObject
    {
        [Header("Meta")]
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public Sprite Icon;
        public Rarity Rarity;
        public UpgradeKind Kind;

        [Header("Picks")]
        public bool IsRepeatable;           // repeatable?
        [Tooltip("If > 0 and Repeatable, cap repeats")]
        public int MaxPicks = 0;

        [Header("Weights")]
        public float BaseWeight = 1f;
        public Rule.WeightRule[] Rules;

        [Header("Effect")]
        public UpgradeAction Action;      // <- only this varies per upgrade

        public bool IsAvailable(ProgressionContext ctx)
        {
            if (ctx.History.IsCapped(Id)) return false;
            return Action ? Action.IsAvailable(ctx, this) : false;
        }

        public float ComputeWeight(ProgressionContext ctx)
        {
            if (!IsAvailable(ctx)) return 0f;
            float w = BaseWeight;
            if (Rules != null) foreach (var r in Rules) if (r) w *= r.GetMultiplier(ctx, this);
            int n = ctx.History.Count(Id);
            if (n > 0) w *= 1f / (1f + 0.5f * n);
            return Mathf.Max(0f, w);
        }

        public string[] GetPreviewLines(ProgressionContext ctx)
            => Action ? Action.GetPreviewLines(ctx, this) : new[] { "(No Action)" };

        public ChangeSet Apply(ProgressionContext ctx)
        {
            var cs = Action ? Action.Apply(ctx, this) : new ChangeSet();

            // Shared pick/cap bookkeeping
            int after = ctx.History.Count(Id) + 1;
            if (!IsRepeatable)
            {
                ctx.History.MarkCapped(Id);
            }
            else if (MaxPicks > 0 && after >= MaxPicks)
            {
                ctx.History.MarkCapped(Id);
            }
            ctx.History.RecordPick(Id);
            return cs;
        }
    }
}
