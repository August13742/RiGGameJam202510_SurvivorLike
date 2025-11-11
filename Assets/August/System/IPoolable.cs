namespace Survivor.Game 
{
    public interface IPoolable
    {
        // Called by pool after activation
        void OnSpawned();
        // Called by pool just before deactivation
        void OnDespawned();
    }
}