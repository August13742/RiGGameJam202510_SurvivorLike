using UnityEngine;
using Survivor.Game;
namespace Survivor.Drop
{
    public sealed class GoldDrop : DropItemBase
    {

        protected override void Apply(GameObject player)
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.AddGold(amount);
            }
            // play sound, spawn tiny sparkle (pooled)
        }
    }
}
