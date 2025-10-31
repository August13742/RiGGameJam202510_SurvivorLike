using Survivor.Game;
using Survivor.Weapon;
using UnityEngine;

namespace Survivor.Progression
{
    public sealed class ProgressionContext
    {
        public readonly SessionManager Session;
        public readonly GameObject PlayerGO;
        public readonly WeaponController WeaponController;
        public readonly SelectionHistory History;

        public ProgressionContext(SessionManager s, GameObject p, WeaponController wc, SelectionHistory h)
        {
            Session = s; PlayerGO = p; WeaponController = wc; History = h;
        }
        public bool HasEmptyWeaponSlot => WeaponController != null && WeaponController.HasEmptySlot;
        public int PlayerLevel => Session?.PlayerLevel ?? 0;
    }
}
