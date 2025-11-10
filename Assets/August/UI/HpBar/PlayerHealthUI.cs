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


        private void Start()
        {
            playerHealthComponent = SessionManager.Instance.GetPlayerReference().GetComponent<HealthComponent>();
            if (playerHealthComponent != null)
            {
                playerHealthComponent.HealthChanged += OnPlayerHealthChanged;
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

        private void OnPlayerHealthChanged(float current, float previous)
        {
            // Cancel any existing tween or delayed tween
            activeTween?.Kill();
            if (delayCoroutine != null)
            {
                StopCoroutine(delayCoroutine);
            }

            bool tookDamage = current < previous;

            if (tookDamage)
            {
                // On damage: Green bar snaps immediately
                float previousPercent = Mathf.Clamp01(previous / playerHealthComponent.Max);
                healthFill.fillAmount = playerHealthComponent.GetCurrentPercent();
                
                // Start delayed tween for red bar
                delayCoroutine = StartCoroutine(DelayedCatchupTween(previousPercent, false));
            }
            else // Healed
            {
                // On heal: Red bar snaps immediately
                float previousPercent = Mathf.Clamp01(previous / playerHealthComponent.Max);
                damageFill.fillAmount = playerHealthComponent.GetCurrentPercent();
                
                // Start delayed tween for green bar
                delayCoroutine = StartCoroutine(DelayedCatchupTween(previousPercent, true));
            }
            UpdateText();
        }

        private IEnumerator DelayedCatchupTween(float previousPercent, bool isHealing)
        {
            // Wait for the delay period
            yield return new WaitForSeconds(catchupDelay);
            
            // Get the CURRENT health after the delay (captures any changes during delay)
            float currentPercent = playerHealthComponent.GetCurrentPercent();

            if (isHealing)
            {
                // Tween green bar up
                activeTween = Tween.TweenValue(
                    previousPercent,
                    currentPercent,
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
                    previousPercent,
                    currentPercent,
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