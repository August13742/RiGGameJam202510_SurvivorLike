using UnityEngine;

namespace Survivor.Progression
{
    [CreateAssetMenu(menuName = "Defs/Progression/Upgrades/PlayerStat")]
    public sealed class PlayerStatUpgradeDef : UpgradeDef
    {
        [Header("Player Deltas")]
        public float MoveSpeedMul = 1f;      // e.g., 1.1
        public float PickupRadiusMul = 1f;
        public int MaxHPAdd = 0;

        public override string[] GetPreviewLines(ProgressionContext ctx)
        {
            var lines = new System.Collections.Generic.List<string>();
            
            if (MoveSpeedMul != 1f)
                lines.Add($"Move Speed ×{MoveSpeedMul:0.##}");
            
            if (PickupRadiusMul != 1f)
                lines.Add($"Pickup Radius ×{PickupRadiusMul:0.##}");
            
            if (MaxHPAdd != 0)
                lines.Add($"+{MaxHPAdd} Max HP");
            
            return lines.ToArray();
        }

        public override ChangeSet Apply(ProgressionContext ctx)
        {
            var cs = new ChangeSet();
            var player = ctx.PlayerGO;
            if (!player) return cs;

            // Apply player-specific stat modifications
            var stats = player.GetComponent<PlayerStatsComponent>();
            if (!stats) stats = player.AddComponent<PlayerStatsComponent>();

            if (MoveSpeedMul != 1f)
            {
                stats.MoveSpeedMul *= MoveSpeedMul;
                cs.Add($"Move Speed ×{MoveSpeedMul:0.##}");
            }

            if (PickupRadiusMul != 1f)
            {
                stats.PickupRadiusMul *= PickupRadiusMul;
                cs.Add($"Pickup Radius ×{PickupRadiusMul:0.##}");
            }

            if (MaxHPAdd != 0)
            {
                var hp = player.GetComponent<Survivor.Game.HealthComponent>();
                if (hp)
                {
                    hp.SetMaxHP(hp.Max + MaxHPAdd, resetCurrent: false);
                    cs.Add($"+{MaxHPAdd} Max HP");
                }
            }

            // Cap logic for finite perks
            if (!IsInfinite && ctx.History.Count(Id) + 1 >= MaxPicks)
                ctx.History.MarkCapped(Id);

            ctx.History.RecordPick(Id);
            return cs;
        }
    }
}
