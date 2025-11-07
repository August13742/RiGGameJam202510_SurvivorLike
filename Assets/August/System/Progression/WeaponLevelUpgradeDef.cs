using Survivor.Progression;
using Survivor.Weapon;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Defs/Progression/Upgrades/Weapon Level")]
public sealed class WeaponLevelUpgradeDef : UpgradeDef
{
    public WeaponLevelBonus ConstantBonusPerLevel;

    public WeaponDef WeaponDef;


    [Tooltip("Per-level bonuses. Index 0 = Level 1, etc.")]
    public List<WeaponLevelBonus> Levels = new();

    [Min(1)] public int MaxLevel = 1;

    private void OnValidate()
    {
        MaxLevel = Mathf.Max(1, Levels?.Count ?? 0);
    }

    public override bool IsAvailable(ProgressionContext ctx)
    {
        if (!ctx.DroneManager || !ctx.DroneManager.OwnsWeapon(WeaponDef)) return false;

        int n = ctx.History.Count(Id);

        if (IsInfinite)
        {
            // Infinite: only respect MaxPicks if you want an upper bound; 0 or <1 => unlimited.
            if (MaxPicks > 0 && n >= MaxPicks) return false;
            return true;
        }
        else
        {
            // Finite: respect level count.
            if (n >= MaxLevel) return false;
            return !ctx.History.IsCapped(Id);
        }
    }

    public override string[] GetPreviewLines(ProgressionContext ctx)
    {
        var lines = new List<string>();
        if (!WeaponDef) return new[] { "Missing WeaponDef" };

        int current = ctx.History.Count(Id);
        lines.Add($"{WeaponDef.name} Level {current + 1}");

        // 1) Per-level bonus (finite track) if any remains
        if (!IsInfinite && Levels != null && current < Levels.Count && Levels[current] != null && !Levels[current].IsNoop())
        {
            AppendBonus(lines, Levels[current], header: "Per-Level Bonus");
        }

        // 2) Constant bonus (always granted each pick when present)
        if (ConstantBonusPerLevel != null && !ConstantBonusPerLevel.IsNoop())
        {
            AppendBonus(lines, ConstantBonusPerLevel, header: IsInfinite ? "Per Pick (Constant)" : "Also Grants");
        }

        // 3) State hint
        if (IsInfinite)
        {
            if (MaxPicks > 0) lines.Add($"(Repeatable, up to {MaxPicks} picks)");
            else lines.Add("(Repeatable, no cap)");
        }
        else if (current >= MaxLevel)
        {
            lines.Add("Max level reached");
        }

        return lines.ToArray();
    }

    public override ChangeSet Apply(ProgressionContext ctx)
    {
        var cs = new ChangeSet();

        if (ctx.DroneManager && WeaponDef)
        {
            var controller = ctx.DroneManager.GetControllerForWeapon(WeaponDef);

            if (controller != null)
            {
                int n = ctx.History.Count(Id);

                // 1) Apply the finite track level (if applicable)
                if (!IsInfinite && Levels != null && n < Levels.Count && Levels[n] != null && !Levels[n].IsNoop())
                {
                    controller.ApplyUpgrade(WeaponDef, Levels[n]);
                    cs.Add($"{WeaponDef.name}: Level {n + 1} applied");
                }

                // 2) Apply the constant per-pick bonus (ALWAYS, if present)
                if (ConstantBonusPerLevel != null && !ConstantBonusPerLevel.IsNoop())
                {
                    controller.ApplyUpgrade(WeaponDef, ConstantBonusPerLevel);
                    cs.Add($"{WeaponDef.name}: Constant bonus applied");
                }
            }
        }

        // 3) Record pick & cap logic
        int after = ctx.History.Count(Id) + 1;
        if (IsInfinite)
        {
            if (MaxPicks > 0 && after >= MaxPicks)
                ctx.History.MarkCapped(Id); // stop offering after MaxPicks
        }
        else
        {
            if (after >= MaxLevel)
                ctx.History.MarkCapped(Id);
        }
        ctx.History.RecordPick(Id);

        return cs;
    }

    // --- helpers -----------------------------------------------------------
    private static void AppendBonus(List<string> lines, WeaponLevelBonus b, string header)
    {
        if (!string.IsNullOrEmpty(header)) lines.Add(header + ":");

        if (b.DamageMultiplierBonus != 0f)
            lines.Add($"  Damage ×(1{SignedPct(b.DamageMultiplierBonus)})");
        if (b.CooldownReduction != 0f)
            lines.Add($"  Cooldown {SignedPct(-b.CooldownReduction)}");
        if (b.AreaScaleBonus != 0f)
            lines.Add($"  Area ×(1{SignedPct(b.AreaScaleBonus)})");
        if (b.SpeedMultiplierBonus != 0f)
            lines.Add($"  Speed ×(1{SignedPct(b.SpeedMultiplierBonus)})");

        if (b.ProjectileCountBonus != 0)
            lines.Add($"  +{b.ProjectileCountBonus} Projectiles");
        if (b.PierceCountBonus != 0)
            lines.Add($"  +{b.PierceCountBonus} Pierce");

        if (b.CritChanceBonus != 0f)
            lines.Add($"  Crit Chance {SignedPct(b.CritChanceBonus)}");
        if (b.CritDamageMultiplierBonus != 0f)
            lines.Add($"  Crit Damage ×(1{SignedPct(b.CritDamageMultiplierBonus)})");
    }

    private static string SignedPct(float x) => $"{(x >= 0f ? "+" : "")}{x * 100f:0.#}%";
}
