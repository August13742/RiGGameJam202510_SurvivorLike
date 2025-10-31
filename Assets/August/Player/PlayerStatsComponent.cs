using UnityEngine;

namespace Survivor.Progression
{
    [DisallowMultipleComponent]
    public sealed class PlayerStatsComponent : MonoBehaviour
    {
        public float MoveSpeedMul = 1f;
        public float PickupRadiusMul = 1f;
    }
}
