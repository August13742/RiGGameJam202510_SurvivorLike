using UnityEngine;
using System.Collections.Generic;
using Survivor.Weapon;

namespace Survivor.Progression
{
    [CreateAssetMenu(menuName = "Defs/Progression/Actions/Weapon Level")]
    public sealed class WeaponLevelAction : UpgradeAction
    {
        public WeaponDef WeaponDef;
        public WeaponLevelBonus ConstantBonusPerPick;
        public List<WeaponLevelBonus> Levels = new();
        [Min(1)] public int MaxLevel = 1;

        private void OnValidate()
        {
            MaxLevel = Mathf.Max(1, Levels?.Count ?? 0);
        }

        public override bool IsAvailable(ProgressionContext ctx, UpgradeDef card)
        {
            if (!ctx.DroneManager || !WeaponDef) return false;
            if (!ctx.DroneManager.OwnsWeapon(WeaponDef)) return false;

            int n = ctx.History.Count(card.Id);

            // Below max level: always available
            if (n < MaxLevel) return true;

            // At or above max level: only available if repeatable
            if (card.IsRepeatable)
            {
                if (card.MaxPicks > 0 && n >= card.MaxPicks) return false;
                return true;
            }

            return false;
        }

        public override string[] GetPreviewLines(ProgressionContext ctx, UpgradeDef card)
        {
            var lines = new List<string>();
            if (!WeaponDef) return new[] { "Missing WeaponDef" };

            int current = ctx.History.Count(card.Id);
            bool isInLevelRange = current < MaxLevel;

            lines.Add($"{WeaponDef.name} Level {current + 1}");

            // Show level-specific bonus if still leveling
            if (isInLevelRange && Levels != null && current < Levels.Count && Levels[current] != null && !Levels[current].IsNoop())
                AppendBonus(lines, Levels[current], "Level Bonus");

            // Show constant bonus if it exists and we're in repeatable territory
            if (!isInLevelRange && ConstantBonusPerPick != null && !ConstantBonusPerPick.IsNoop())
                AppendBonus(lines, ConstantBonusPerPick, "Repeatable Bonus");

            if (!isInLevelRange && card.IsRepeatable)
                lines.Add(card.MaxPicks > 0 ? $"(Repeatable, up to {card.MaxPicks})" : "(Repeatable)");
            else if (current >= MaxLevel && !card.IsRepeatable)
                lines.Add("Max level reached");

            return lines.ToArray();
        }

        public override ChangeSet Apply(ProgressionContext ctx, UpgradeDef card)
        {
            var cs = new ChangeSet();
            if (ctx.DroneManager && WeaponDef)
            {
                var controller = ctx.DroneManager.GetControllerForWeapon(WeaponDef);
                if (controller)
                {
                    int n = ctx.History.Count(card.Id);
                    bool isInLevelRange = n < MaxLevel;

                    // Apply level-specific bonus if still leveling
                    if (isInLevelRange && Levels != null && n < Levels.Count && Levels[n] != null && !Levels[n].IsNoop())
                    {
                        controller.ApplyUpgrade(WeaponDef, Levels[n]);
                        cs.Add($"{WeaponDef.name}: Level {n + 1} applied");
                    }

                    // Apply constant bonus if we're in repeatable territory
                    if (!isInLevelRange && ConstantBonusPerPick != null && !ConstantBonusPerPick.IsNoop())
                    {
                        controller.ApplyUpgrade(WeaponDef, ConstantBonusPerPick);
                        cs.Add($"{WeaponDef.name}: Constant bonus applied");
                    }
                }
            }
            return cs;
        }

        private static void AppendBonus(List<string> lines, WeaponLevelBonus b, string header)
        {
            if (!string.IsNullOrEmpty(header)) lines.Add(header + ":");
            if (b.DamageMultiplierBonus != 0f) lines.Add($"  Damage ×(1{SignedPct(b.DamageMultiplierBonus)})");
            if (b.CooldownReduction != 0f) lines.Add($"  Cooldown {SignedPct(-b.CooldownReduction)}");
            if (b.AreaScaleBonus != 0f) lines.Add($"  Area ×(1{SignedPct(b.AreaScaleBonus)})");
            if (b.SpeedMultiplierBonus != 0f) lines.Add($"  Speed ×(1{SignedPct(b.SpeedMultiplierBonus)})");
            if (b.ProjectileCountBonus != 0) lines.Add($"  +{b.ProjectileCountBonus} Projectiles");
            if (b.PierceCountBonus != 0) lines.Add($"  +{b.PierceCountBonus} Pierce");
            if (b.CritChanceBonus != 0f) lines.Add($"  Crit Chance {SignedPct(b.CritChanceBonus)}");
            if (b.CritDamageMultiplierBonus != 0f) lines.Add($"  Crit Damage ×(1{SignedPct(b.CritDamageMultiplierBonus)})");
        }
        private static string SignedPct(float x) => $"{(x >= 0f ? "+" : "")}{x * 100f:0.#}%";
    }
}