using UnityEngine;

namespace Survivor.Game
{
    public sealed class EXPDrop : DropItemBase
    {
        [SerializeField] private int ExpValue = 1;

        protected override void Apply(GameObject player)
        {
            SessionManager.Instance?.AddExp(ExpValue * amount);
            // play sound, spawn tiny sparkle (pooled)
        }
    }
}
