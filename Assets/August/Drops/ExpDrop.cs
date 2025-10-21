using UnityEngine;
using Survivor.Game;
namespace Survivor.Drop
{
    public sealed class ExpDrop : DropItemBase
    {
        [SerializeField] private int ExpValue = 1;

        protected override void Apply(GameObject player)
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.AddExp(ExpValue * amount);
            }
            // play sound, spawn tiny sparkle (pooled)
        }
    }
}
