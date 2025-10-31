using UnityEngine;

namespace Survivor.Weapon
{
    public abstract class WeaponModDef : ScriptableObject
    {
        public string Id;
        public string Title;
        [TextArea] public string Description;
        public Sprite Icon;

        public virtual void OnEquip(IWeapon weapon) { }
        public virtual void OnTick(IWeapon weapon, float dt) { }
        public virtual void OnHit(IWeapon weapon, int dmg, Vector2 pos, bool crit) { }
        public virtual void OnCrit(IWeapon weapon, Vector2 pos) { }
        public virtual void OnKill(IWeapon weapon, Vector2 pos) { }
        public virtual void OnUnequip(IWeapon weapon) { }

        // Helper to get context from weapon
        protected WeaponContext GetContext(IWeapon weapon)
        {
            if (weapon is WeaponBase<WeaponDef> wb)
                return wb.GetContext();
            return null;
        }

        // Helper to get owner transform
        protected Transform GetOwner(IWeapon weapon)
        {
            return GetContext(weapon)?.Owner;
        }
    }
}