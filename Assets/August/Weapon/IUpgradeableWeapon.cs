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

    /// <summary>
    /// Applies stat modifiers specific to this weapon instance.
    /// </summary>
    void ApplyUpgrade(WeaponStats delta);

    /// <summary>
    /// Gets the current combined stats for this weapon.
    /// </summary>
    WeaponStats GetCurrentStats();
}