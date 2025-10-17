using System;
using UnityEngine;

namespace Survivor.Game
{
    public class SessionManager : MonoBehaviour
    {
        // Manages Session Progression(Timer, Dynamic Difficulty, etc.) Level
        public int Gold { get; private set; } = 0;
        public int PlayerLevel { get; private set; } = 0;
        private int currentExp = 0;
        private int currentExpReq = 5;
        public int ExpGrowthPerLevel = 5;

        public Action<int> GoldChanged;
        public Action LevelUp;
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
        private void TriggerLevelUp()
        {
            PlayerLevel += 1;
            currentExpReq += ExpGrowthPerLevel;
            LevelUp?.Invoke();
        }
    }
    #endregion
}