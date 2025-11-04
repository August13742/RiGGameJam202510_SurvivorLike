using Survivor.Game;
using Survivor.Weapon;
using UnityEngine;

namespace Survivor.Progression
{
    public sealed class ProgressionContext
    {
        public readonly SessionManager Session;
        public readonly GameObject PlayerGO;
        public readonly DroneManager DroneManager;
        public readonly SelectionHistory History;

        public ProgressionContext(SessionManager s, GameObject p, DroneManager dm, SelectionHistory h)
        {
            Session = s; PlayerGO = p; DroneManager = dm; History = h;
        }
        public bool HasEmptyWeaponSlot => DroneManager != null && DroneManager.HasEmptyWeaponSlot();
        public int PlayerLevel => Session?.PlayerLevel ?? 0;
    }
}
