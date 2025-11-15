using UnityEngine;

namespace Survivor.Progression
{
    [CreateAssetMenu(menuName = "Defs/Progression/Actions/Player Stat")]
    public sealed class PlayerStatAction : UpgradeAction
    {
        [Header("Player Stat Bonuses")]
        [Tooltip("Additive multiplier. 0.1 = +10% move speed.")]
        public float MoveSpeedBonus = 0f;
        [Tooltip("Additive multiplier. 0.2 = +20% pickup radius.")]
        public float PickupRadiusBonus = 0f;
        [Tooltip("Flat bonus to max health.")]
        public int MaxHPAdd = 0;

        public override bool IsAvailable(ProgressionContext ctx, UpgradeDef card)
        {
            // This action is always available as long as it's not capped.
            // The cap is already checked in UpgradeDef.IsAvailable, so we just return true.
            return true;
        }

        public override string[] GetPreviewLines(ProgressionContext ctx, UpgradeDef card)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (MoveSpeedBonus != 0f) lines.Add($"Move Speed +{MoveSpeedBonus:P0}");
            if (PickupRadiusBonus != 0f) lines.Add($"Pickup Radius +{PickupRadiusBonus:P0}");
            if (MaxHPAdd != 0) lines.Add($"+{MaxHPAdd} Max HP");
            return lines.ToArray();
        }

        public override ChangeSet Apply(ProgressionContext ctx, UpgradeDef card)
        {
            var cs = new ChangeSet();
            if (!ctx.PlayerGO) return cs;

            var statsComponent = ctx.PlayerGO.GetComponent<PlayerStatsComponent>();
            if (!statsComponent)
            {
                Debug.LogWarning("Player has no PlayerStatsComponent to apply upgrade to.");
                return cs;
            }

            // Create a new bonus object with the values from this action.
            var bonus = new PlayerStatBonus
            {
                MoveSpeedBonus = this.MoveSpeedBonus,
                PickupRadiusBonus = this.PickupRadiusBonus,
                MaxHpBonus = this.MaxHPAdd
            };

            // Add the bonus to the player's stat component.
            statsComponent.AddBonus(bonus);

            // Add preview lines to the change set for logging.
            if (MoveSpeedBonus != 0f) cs.Add($"Move Speed +{MoveSpeedBonus:P0}");
            if (PickupRadiusBonus != 0f) cs.Add($"Pickup Radius +{PickupRadiusBonus:P0}");
            if (MaxHPAdd != 0) cs.Add($"+{MaxHPAdd} Max HP");

            return cs;
        }
    }
}