using Survivor.Progression;
using Survivor.Weapon;
using UnityEngine;

[CreateAssetMenu(menuName = "Defs/Progression/Upgrades/WeaponLevel")]
public sealed class WeaponLevelUpgradeDef : UpgradeDef
{
    public WeaponDef WeaponDef;
    public WeaponStats Delta; // e.g., +ProjectilesAdd, ×DamageMul, etc.
    public int MaxLevel = 8;

    public override bool IsAvailable(ProgressionContext ctx)
    {
        if (!ctx.DroneManager || !ctx.DroneManager.OwnsWeapon(WeaponDef)) return false;

        int n = ctx.History.Count(Id);
        return !ctx.History.IsCapped(Id) && n < MaxLevel;
    }

    public override string[] GetPreviewLines(ProgressionContext ctx)
    {
        var lines = new System.Collections.Generic.List<string>();
        
        if (WeaponDef != null)
        {
            string weaponName = WeaponDef.name;
            
            // Show current level
            int currentLevel = ctx.History.Count(Id);
            lines.Add($"{weaponName} Level {currentLevel + 1}");
            
            // Show delta modifiers
            if (Delta != null)
            {
                if (Delta.DamageMul != 1f)
                    lines.Add($"  Damage ×{Delta.DamageMul:0.##}");
                
                if (Delta.CooldownMul != 1f)
                    lines.Add($"  Cooldown ×{Delta.CooldownMul:0.##}");
                
                if (Delta.AreaMul != 1f)
                    lines.Add($"  Area ×{Delta.AreaMul:0.##}");
                
                if (Delta.ProjectilesAdd != 0)
                    lines.Add($"  +{Delta.ProjectilesAdd} Projectile(s)");
                
                if (Delta.PierceAdd != 0)
                    lines.Add($"  +{Delta.PierceAdd} Pierce");
                
                if (Delta.SpeedMul != 1f)
                    lines.Add($"  Speed ×{Delta.SpeedMul:0.##}");
                
                if (Delta.CritChance != 0.05f) // 0.05 is the default
                    lines.Add($"  Crit Chance {Delta.CritChance:0.##%}");
                
                if (Delta.CritMultiplier != 1.5f) // 1.5 is the default
                    lines.Add($"  Crit Multiplier ×{Delta.CritMultiplier:0.##}");
            }
        }
        
        return lines.ToArray();
    }

    public override ChangeSet Apply(ProgressionContext ctx)
    {
        var cs = new ChangeSet();
        if (ctx.DroneManager && WeaponDef)
        {
            WeaponController targetController = ctx.DroneManager.GetControllerForWeapon(WeaponDef);
            if (targetController != null)
            {
                targetController.ApplyUpgrade(WeaponDef, Delta);
                cs.Add($"{WeaponDef.name}: +level modifiers");
            }
            
        }
        int after = ctx.History.Count(Id) + 1;
        if (after >= MaxLevel) ctx.History.MarkCapped(Id);
        ctx.History.RecordPick(Id);
        return cs;
    }
}