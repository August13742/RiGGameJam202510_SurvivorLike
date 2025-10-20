using Survivor.Game;
using UnityEngine;

namespace Survivor.UI { 
    public class PlayerHealthUI : MonoBehaviour
    {
        [SerializeField] private GameObject hpFill;
        private HealthComponent playerHealthComponent;

        private void Start()
        {
            playerHealthComponent = SessionManager.Instance.GetPlayerReference().GetComponent<HealthComponent>();
            if (playerHealthComponent != null)
            {
                playerHealthComponent.HealthChanged += OnPlayerHealthChanged;
            
            }
            else Debug.LogError("Could not locate player HealthComponent");
        }

        private void OnPlayerHealthChanged(int current, int _prev)
        {
            hpFill.transform.localScale = new Vector3(playerHealthComponent.GetCurrentPercent(),1,1);
        }

    }
}