using UnityEngine;
using Survivor.Game;
namespace Survivor.Drop
{
    public sealed class HealDrop : DropItemBase
    {

        protected override void Apply(GameObject player)
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.RestorePlayerHealth(amount);
            }
            // play sound, spawn tiny sparkle (pooled)
        }
    }
}
