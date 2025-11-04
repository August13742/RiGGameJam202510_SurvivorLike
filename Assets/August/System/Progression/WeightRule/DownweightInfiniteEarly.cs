using UnityEngine;
namespace Survivor.Progression.Rule
{
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