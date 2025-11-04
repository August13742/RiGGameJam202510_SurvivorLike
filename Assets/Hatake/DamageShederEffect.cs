using System.Collections;
using Survivor.Game;
using UnityEngine;

public class DamageShederEffect : MonoBehaviour
{
    [SerializeField] private float effectTime = 0.5f;

    private HealthComponent healthComponent;
    private Renderer targetRenderer;
    private Material materialInstance;

    private Coroutine flashCoroutine;

    private static readonly int DamageIntensityProperty = Shader.PropertyToID("_DamageIntensity");

    void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
        targetRenderer = GetComponentInChildren<Renderer>();

        materialInstance = targetRenderer.material;

        materialInstance.SetFloat(DamageIntensityProperty, 0.0f);
    }

    // Update is called once per frame
    private void OnEnable()
    {
        healthComponent.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        healthComponent.Damaged -= OnDamaged;
    }

    private void OnDamaged(float amount, Vector3 worldPos, bool isCrit)
    {
        Debug.Log("damage");

        flashCoroutine = StartCoroutine(ResetSpeedAfterDelay());
    }

    private IEnumerator ResetSpeedAfterDelay()
    {
        float timer = 0f;

        materialInstance.SetFloat(DamageIntensityProperty, 1.0f);

        while (timer < effectTime) 
        {
            float intensity = 1.0f - ( timer / effectTime );
            materialInstance.SetFloat(DamageIntensityProperty, intensity);

            timer += Time.deltaTime;
            yield return null;
        }

        materialInstance.SetFloat(DamageIntensityProperty, 0.0f);
        flashCoroutine = null;
    }
}
