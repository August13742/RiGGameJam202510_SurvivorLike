using System;
using UnityEngine;

namespace Survivor.Game
{
    public class SessionManager : MonoBehaviour
    {
        // Manages Session Progression(Timer, Dynamic Difficulty, etc.) Level
        private GameObject Player;
        public int Gold { get; private set; } = 0;
        public int PlayerLevel { get; private set; } = 0;

        private int currentExp = 0;
        private int currentExpReq = 5;
        public int ExpGrowthPerLevel = 5;
        public Action LevelUp;


        public Action<int> GoldChanged;
        public static SessionManager Instance { get; private set; }
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); return;
            }
            Instance = this;
            DontDestroyOnLoad(this);
        }
        private void Start()
        {
            // Trigger the first level up immediately for the initial weapon unlock.
            // A small delay ensures other scripts have time to run their Awake() and OnEnable().
            Invoke(nameof(TriggerLevelUp), 0.1f);
        }

        public GameObject GetPlayerReference()
        {
            if(Player == null)
            {
                Player = GameObject.FindGameObjectWithTag("Player");
            }
            return Player;
        }
        public void RestorePlayerHealth(int amount)
        {
            if (Player == null) GetPlayerReference();
            Player.GetComponent<HealthComponent>().Heal(amount);
        }

        public void AddGold(int amount)
        {
            if (amount < 0) return;
            Gold += amount;
            GoldChanged?.Invoke(amount); 
        }

        #region EXP manage
        public void AddExp(int amount)
        {
            if (amount < 0) return;
            currentExp += amount;
            CheckIfLevelUp();
        }
        private void CheckIfLevelUp()
        {
            if (currentExp >= currentExpReq)
            {
                int residual = currentExp - currentExpReq;
                TriggerLevelUp();
                currentExp = 0 + residual;

            }
        }
        public void TriggerLevelUp()
        {
            PlayerLevel += 1;
            currentExpReq += ExpGrowthPerLevel;
            Debug.Log($"<color=orange>LEVEL UP! Now Level {PlayerLevel}.</color>");
            LevelUp?.Invoke();
        }
    }
    #endregion
}