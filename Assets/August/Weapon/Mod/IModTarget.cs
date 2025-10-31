namespace Survivor.Weapon
{

    public interface IModTarget
    {
        WeaponContext GetContext();
        void SetDynamicMods(WeaponStats mods);
        WeaponStats GetAndMutateDynamicMods(System.Action<WeaponStats> mut = null);
    }
}