using UnityEngine;

namespace Survivor.Progression.Rule
{
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
}