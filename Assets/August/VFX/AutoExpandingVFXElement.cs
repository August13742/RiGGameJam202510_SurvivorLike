using UnityEngine;
using AugustsUtility.Tween;
using Survivor.Game;
namespace Survivor.VFX
{
    [RequireComponent(typeof(SpriteRenderer),typeof(PrefabStamp))]
    [DisallowMultipleComponent]
    public class AutoExpandingVFXElement : MonoBehaviour, IPoolable
    {
        public Vector3 InitialScale = new(0f, 0f, 1f);
        public Vector3 FinalScale = new(0.3f, 0.3f, 1f);
        public float Duration = 0.3f;

        [Header("Colors")]
        public Color NormalColor = Color.white;
        public Color CritColor = new(1f, 0.3f, 0.3f, 1f);

        private SpriteRenderer _sr;
        private PrefabStamp _stamp;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _stamp = GetComponent<PrefabStamp>();
        }

        /// <summary>Set one-shot parameters before spawn animation runs.</summary>
        public void Init(bool crit)
        {
            _sr.color = crit ? CritColor : NormalColor;
        }

        void IPoolable.OnSpawned()
        {
            // Reset start state and animate out
            transform.localScale = InitialScale;
            transform.TweenLocalScale(FinalScale, Duration);
            _sr.TweenColorAlpha(0f, Duration, EasingFunctions.EaseInOutQuint, Despawn);
        }
        void Despawn()
        {
            if (_stamp != null && (_stamp.OwnerPool != null)) _stamp.OwnerPool.Return(gameObject);
            else gameObject.SetActive(false);
        }
        void IPoolable.OnDespawned()
        {
            transform.localScale = InitialScale;
            // restore opaque so next spawn can fade again
            var c = _sr.color; c.a = 1f; _sr.color = c;
        }
    }
}
