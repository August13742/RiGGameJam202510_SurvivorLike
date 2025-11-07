using UnityEngine;

namespace Survivor.UI
{
    [CreateAssetMenu(menuName = "Defs/UI/Damage Text Style")]
    public sealed class DamageTextStyle : ScriptableObject
    {
        [Header("Normal")]
        public Color normalColor = Color.white;
        public float normalScale = 1f;

        [Header("Crit")]
        public Color critColor = new(1f, 0.85f, 0.2f);
        public float critScale = 1.3f;

        [Header("Heal")]
        public Color healColor = new(0.2f, 1f, 0.2f);
        public float healScale = 1.0f;

        [Header("Motion")]
        [Min(0f)] public float riseSpeed = 2f;        // units/sec
        [Min(0.05f)] public float lifetime = 0.7f;    // seconds
        [Min(0f)] public float horizontalJitter = 0.25f;

        [Tooltip("Alpha vs lifetime (0..1). Should be monotone and within [0,1].")]
        public AnimationCurve alphaOverLife = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Tooltip("Scale vs lifetime (0..1). Elastic overshoot looks good here.")]
        public AnimationCurve scaleOverLife; // set in OnValidate

        private void OnValidate()
        {
            if (alphaOverLife == null || alphaOverLife.length == 0)
                alphaOverLife = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

            if (scaleOverLife == null || scaleOverLife.length == 0)
                scaleOverLife = EasingFunctions.Sample(EasingFunctions.EaseOutElastic, samples: 48);
        }
    }
}
