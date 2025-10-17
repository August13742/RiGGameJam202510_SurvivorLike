using UnityEngine;
namespace Survivor.Game
{
    public interface IDrop
    {
        void OnPickup(GameObject player);
    }
}