using UnityEngine;
namespace Survivor.VFX
{
    public class AutoExpand : MonoBehaviour,IPoolable
    {
        public Vector3 InitialScale = new(0f, 0f, 1);
        public Vector3 FinalScale = new(0.3f,0.3f,1);
        public float Duration = 0.3f;

        void IPoolable.OnSpawned()
        {
            CustomTween.To(transform.localScale, FinalScale, Duration);
        }
        void IPoolable.OnDespawned()
        {
            transform.localScale = InitialScale;
        }
    }
}