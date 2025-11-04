using UnityEngine;
using Survivor.Weapon;

namespace Survivor.Progression
{
    [CreateAssetMenu(menuName = "Defs/Progression/Upgrades/WeaponUnlock")]
    public sealed class WeaponUnlockUpgradeDef : UpgradeDef
    {
        public WeaponDef WeaponDef;
        public override bool IsAvailable(ProgressionContext ctx)
        {
            if (ctx.History.IsCapped(Id)) return false;
            return ctx.DroneManager && ctx.DroneManager.HasEmptyWeaponSlot() && !ctx.DroneManager.OwnsWeapon(WeaponDef);
        }

        public override string[] GetPreviewLines(ProgressionContext ctx)
        {
            return new[] { $"Unlocked: {WeaponDef?.name ?? "Unknown"}" };
        }

        public override ChangeSet Apply(ProgressionContext ctx)
        {
            var cs = new ChangeSet();
            if (ctx.DroneManager && WeaponDef)
            {
                ctx.DroneManager.UnlockWeaponAsDrone(WeaponDef);
                cs.Add($"Unlocked: {WeaponDef.name}");
            }
            ctx.History.MarkCapped(Id);
            ctx.History.RecordPick(Id);
            return cs;
        }
    }

    
}
