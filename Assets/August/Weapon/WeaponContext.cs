using UnityEngine;

namespace Survivor.Weapon { 

    public sealed class WeaponContext
    {

        public Transform FireOrigin;
        public Transform Owner;                 // player
        public System.Func<Transform> Target;   // targeting strategy (nearest, lastMove, etc.)
        public Transform PoolRoot;              // static pools parent
        public WeaponStats Stats;               // live, upgrade-modified

        public System.Func<Transform> Nearest;
        public System.Func<int, Transform> RandomInRange; // arg = K
    }
}