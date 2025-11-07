using Survivor.Weapon;

/// <summary>
/// Interface for weapons that can be upgraded individually.
/// </summary>
public interface IUpgradeableWeapon : IWeapon 
{
    /// <summary>
    /// Returns true if this weapon instance uses the specified definition.
    /// </summary>
    bool Owns(WeaponDef def);

    void ApplyUpgrade(WeaponLevelBonus bonus);
    EffectiveWeaponStats GetCurrentStats();
}