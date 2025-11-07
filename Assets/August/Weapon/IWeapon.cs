namespace Survivor.Weapon
{
    /// <summary>
    /// Common interface for all runtime weapons.
    /// Implemented by WeaponBase<T> so WeaponController can call Equip/Tick polymorphically.
    /// </summary>
    public interface IWeapon 
    {
        void Equip(WeaponDef def,WeaponContext ctx);
        void Tick(float deltaTime);
    }
}
