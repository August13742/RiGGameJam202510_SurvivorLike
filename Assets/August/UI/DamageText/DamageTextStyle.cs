using UnityEngine;

namespace Survivor.UI
{

    [CreateAssetMenu(menuName ="Defs/UI/Damage Text Style")]
    public sealed class DamageTextStyle : ScriptableObject
    {
        [Header("Normal")]
        public Color normalColor = Color.white;
        public float normalScale = 1f;

        [Header("Crit")]
        public Color critColor = new (1f, 0.85f, 0.2f);
        public float critScale = 1.3f;

        [Header("Heal")]
        public Color healColor = new (0.2f, 1f, 0.2f);
        public float healScale = 1.0f;

        [Header("Motion")]
        public float riseSpeed = 2f;        // units/sec
        public float lifetime = 0.7f;       // seconds
        public float horizontalJitter = 0.25f;
        public AnimationCurve alphaOverLife = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public AnimationCurve scaleOverLife = AnimationCurve.EaseInOut(0, 1, 1, 1);
    }
}