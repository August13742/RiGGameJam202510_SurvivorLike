namespace Survivor.Weapon
{

    public interface IModTarget
    {
        WeaponContext GetContext();
        void SetDynamicMods(RuntimeModState mods);
        RuntimeModState GetAndMutateDynamicMods(System.Action<RuntimeModState> mut = null);
    }
}