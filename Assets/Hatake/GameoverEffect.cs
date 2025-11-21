using System.Collections;
using UnityEngine;
using UnityEngine.Rendering; 
using UnityEngine.Rendering.Universal;
using Survivor.Game;

public class GameOverEffectController : MonoBehaviour
{
    [SerializeField] private float effectTime = 2.5f;
    [SerializeField] private Volume globalVolume;
    [SerializeField, Range(0f, 1f)] private float maxAberration = 1.0f;

    private HealthComponent healthComponent;
    private ColorAdjustments colorAdjustments;
    private ChromaticAberration chromaticAberration;
    
    private Coroutine effectCoroutine;

    void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();

        if (globalVolume == null) globalVolume = FindAnyObjectByType<Volume>();

        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out colorAdjustments);
            
            if (!globalVolume.profile.TryGet(out chromaticAberration)){}
        }
    }

    private void OnEnable()
    {
        if (healthComponent != null) healthComponent.Died += OnDied;
    }

    private void OnDisable()
    {
        if (healthComponent != null) healthComponent.Died -= OnDied;
    }

    private void OnDied()
    {
        if (effectCoroutine != null) return;
        effectCoroutine = StartCoroutine(PlayGameOverEffect());
    }

    private IEnumerator PlayGameOverEffect()
    {
        if (colorAdjustments == null || chromaticAberration == null) yield break;

        float timer = 0f;
        // 彩度の初期値
        float startSaturation = colorAdjustments.saturation.value;
        // 色収差の初期値
        float startAberration = chromaticAberration.intensity.value;

        // 目標値
        float targetSaturation = -100f;
        float targetAberration = maxAberration;

        colorAdjustments.saturation.overrideState = true;
        chromaticAberration.intensity.overrideState = true;

        while (timer < effectTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, timer / effectTime);

            colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, targetSaturation, t);
            
            chromaticAberration.intensity.value = Mathf.Lerp(startAberration, targetAberration, t);
            
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        colorAdjustments.saturation.value = targetSaturation;
        chromaticAberration.intensity.value = targetAberration;
        
        effectCoroutine = null;
    }
}