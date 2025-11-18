using UnityEngine;
using Survivor.Weapon;

namespace Survivor.Progression
{
    [CreateAssetMenu(menuName = "Defs/Progression/Actions/Weapon Unlock")]
    public sealed class WeaponUnlockAction : UpgradeAction
    {
        public WeaponDef WeaponDef;

        public override bool IsAvailable(ProgressionContext ctx, UpgradeDef card)
        {
            if (!ctx.DroneManager || !WeaponDef) return false;
            if (!ctx.DroneManager.HasEmptyWeaponSlot()) return false;
            if (ctx.DroneManager.OwnsWeapon(WeaponDef)) return false;
            return true;
        }

        public override string[] GetPreviewLines(ProgressionContext ctx, UpgradeDef card)
        {
            return new[] { $"Unlock: {WeaponDef?.name ?? "Unknown"}" };
        }

        public override ChangeSet Apply(ProgressionContext ctx, UpgradeDef card)
        {
            var cs = new ChangeSet();
            if (ctx.DroneManager && WeaponDef)
            {
                ctx.DroneManager.UnlockWeaponAsDrone(WeaponDef);
                cs.Add($"Unlocked: {WeaponDef.name}");
            }
            return cs;
        }
    }
}
