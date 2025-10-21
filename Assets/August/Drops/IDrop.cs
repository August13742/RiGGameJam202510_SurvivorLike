using UnityEngine;
namespace Survivor.Drop
{
    public interface IDrop
    {
        void OnPickup(GameObject player);
    }
}