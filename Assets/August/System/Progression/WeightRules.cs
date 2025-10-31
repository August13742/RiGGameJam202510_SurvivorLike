using UnityEngine;

namespace Survivor.Progression
{
    public abstract class WeightRule : ScriptableObject
    {
        public abstract float GetMultiplier(ProgressionContext ctx, UpgradeDef def);
    }

    [CreateAssetMenu(menuName = "Defs/Progression/Rules/UpweightWeaponWhenSlotsFree")]
    public sealed class UpweightWeaponWhenSlotsFree : WeightRule
    {
        public float MultiplierWhenFree = 2.0f;
        public override float GetMultiplier(ProgressionContext ctx, UpgradeDef def)
        {
            if (ctx.HasEmptyWeaponSlot && (def.Kind == UpgradeKind.WeaponUnlock))
                return MultiplierWhenFree;
            return 1f;
        }
    }

    [CreateAssetMenu(menuName = "Defs/Progression/Rules/DownweightInfiniteEarly")]
    public sealed class DownweightInfiniteEarly : WeightRule
    {
        public int UntilLevel = 10;
        public float Multiplier = 0.4f;
        public override float GetMultiplier(ProgressionContext ctx, UpgradeDef def)
        {
            if (ctx.PlayerLevel < UntilLevel && def.IsInfinite)
                return Multiplier;
            return 1f;
        }
    }
}
