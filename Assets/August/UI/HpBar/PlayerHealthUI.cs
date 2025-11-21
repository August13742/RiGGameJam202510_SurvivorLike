using AugustsUtility.Tween;
using Survivor.Game;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Survivor.UI { 
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image healthFill; // The primary (green) health bar
        [SerializeField] private Image damageFill; // The secondary (red) catch-up bar
        [SerializeField] private TMPro.TMP_Text textField;

        [Header("Animation Settings")]
        [SerializeField] private float fillDuration = 0.6f; // How long the catch-up tween takes
        [SerializeField] private float catchupDelay = 0.3f; // Delay before catchup animation starts


        private HealthComponent playerHealthComponent;
        private ITween activeTween; // Store the active tween to kill it if interrupted
        private Coroutine delayCoroutine; // Store the delay coroutine to stop it if interrupted
        private float _previousHealth; // Track previous health value


        private void Start()
        {
            playerHealthComponent = SessionManager.Instance.GetPlayerReference().GetComponent<HealthComponent>();
            if (playerHealthComponent != null)
            {
                playerHealthComponent.HealthChanged += OnPlayerHealthChanged;
                _previousHealth = playerHealthComponent.Current;
                Initialise(playerHealthComponent.GetCurrentPercent());

            }
            else
            {
                Debug.LogError("Could not locate player HealthComponent.");
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (playerHealthComponent != null)
            {
                playerHealthComponent.HealthChanged -= OnPlayerHealthChanged;
            }
            activeTween?.Kill();
            if (delayCoroutine != null)
            {
                StopCoroutine(delayCoroutine);
            }
        }


        private void Initialise(float initialPercent)
        {
            healthFill.fillAmount = initialPercent;
            damageFill.fillAmount = initialPercent;
            UpdateText();
        }

        private void UpdateText()
        {
            textField.text = $"{(int)playerHealthComponent.Current}/{(int)playerHealthComponent.Max}";
        }

        private void OnPlayerHealthChanged(float current, float max)
        {
            // Cancel any existing tween or delayed tween
            activeTween?.Kill();
            if (delayCoroutine != null)
            {
                StopCoroutine(delayCoroutine);
            }

            bool tookDamage = current < _previousHealth;
            float previousPercent = Mathf.Clamp01(_previousHealth / max);
            float currentPercent = Mathf.Clamp01(current / max);

            if (tookDamage)
            {
                // On damage: Green bar snaps immediately
                healthFill.fillAmount = currentPercent;
                
                // Start delayed tween for red bar
                delayCoroutine = StartCoroutine(DelayedCatchupTween(previousPercent, currentPercent, false));
            }
            else if (current > _previousHealth) // Healed
            {
                // On heal: Red bar snaps immediately
                damageFill.fillAmount = currentPercent;
                
                // Start delayed tween for green bar
                delayCoroutine = StartCoroutine(DelayedCatchupTween(previousPercent, currentPercent, true));
            }

            _previousHealth = current;
            UpdateText();
        }

        private IEnumerator DelayedCatchupTween(float fromPercent, float toPercent, bool isHealing)
        {
            // Wait for the delay period
            yield return new WaitForSeconds(catchupDelay);

            if (isHealing)
            {
                // Tween green bar up
                activeTween = Tween.TweenValue(
                    fromPercent,
                    toPercent,
                    fillDuration,
                    (v) => healthFill.fillAmount = v,
                    Lerp.Get<float>(),
                    EasingFunctions.EaseInOutQuint
                );
            }
            else
            {
                // Tween red bar down
                activeTween = Tween.TweenValue(
                    fromPercent,
                    toPercent,
                    fillDuration,
                    (v) => damageFill.fillAmount = v,
                    Lerp.Get<float>(),
                    EasingFunctions.EaseInOutQuint
                );
            }
            
            delayCoroutine = null;
        }

    }
}