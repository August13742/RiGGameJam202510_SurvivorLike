using UnityEngine;

namespace Survivor.Weapon
{
    public sealed class WeaponContext
    {
        public Team Team = Team.Player;
        public Transform FireOrigin;
        public Transform Owner;                 // player
        public System.Func<Transform> Target;   // targeting strategy (nearest, lastMove, etc.)
        public Transform PoolRoot;              // static pools parent
        public System.Func<Transform> Nearest;
        public System.Func<int, Transform> RandomInRange; // arg = K
        public System.Func<Transform> SelfCentered;
    }
}