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