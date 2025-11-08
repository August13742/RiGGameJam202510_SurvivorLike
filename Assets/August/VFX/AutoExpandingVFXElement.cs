using UnityEngine;
using AugustsUtility.Tween;
namespace Survivor.VFX
{
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class AutoExpandingVFXElement : MonoBehaviour,IPoolable
    {
        public Vector3 InitialScale = new(0f, 0f, 1);
        public Vector3 FinalScale = new(0.3f,0.3f,1);
        public float Duration = 0.3f;
        private SpriteRenderer sRenderer;
        private Color initialColour;
        private void Awake()
        {
            sRenderer = GetComponent<SpriteRenderer>();
            initialColour = sRenderer.color;
        }
        void IPoolable.OnSpawned()
        {
            transform.TweenLocalScale(FinalScale, Duration);
            sRenderer.TweenColorAlpha(0f, Duration, EasingFunctions.EaseInOutQuint);

        }
        void IPoolable.OnDespawned()
        {
            transform.localScale = InitialScale;
            sRenderer.color = initialColour;
        }
    }
}