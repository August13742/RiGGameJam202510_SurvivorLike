using System;
using UnityEngine;

namespace Survivor.Game
{
    public class SessionManager : MonoBehaviour
    {
        
        // Manages Session Progression(Timer, Dynamic Difficulty, etc.) Level
        private GameObject Player;
        private HealthComponent playerHealthComponent;
        private Achievements achievements;
        public int Gold { get; private set; } = 0;
        public int PlayerLevel { get; private set; } = 0;

        private int currentExp = 0;
        private int currentExpReq = 5;
        public int ExpGrowthPerLevel = 5;
        public Action LevelUp;


        public Action<int> GoldChanged;

        #region PlayerStatistics
        [Header("Statistics")]
        [SerializeField] private int enemyDowned = 0;
        [SerializeField] private float damageDealt = 0f;
        [SerializeField] private float healCount = 0f;
        [SerializeField] private float damageTaken = 0f;
        [SerializeField] private int goldCollected = 0;
        [SerializeField] private int experienceCollected = 0;

        public int EnemyDowned => enemyDowned;
        public float DamageDealt => damageDealt;
        public float HealCount => healCount;
        public float DamageTaken => damageTaken;
        public int GoldCollected => goldCollected;
        public int ExperienceCollected => experienceCollected;

        // --- Public API for modification ---
        public void IncrementEnemyDowned(int amount = 1)
        {
            enemyDowned = Mathf.Max(0, enemyDowned + amount);
            achievements.AddEnemyCount(enemyDowned);
            Debug.Log(enemyDowned);
        }

        public void IncrementDamageDealt(float amount)
        {
            damageDealt = Mathf.Max(0f, damageDealt + amount);
            achievements.AddDamageCount(damageDealt);
        }

        public void IncrementHeal(float amount)
        {
            healCount = Mathf.Max(0f, healCount + amount);
        }

        public void IncrementDamageTaken(float amount)
        {
            damageTaken = Mathf.Max(0f, damageTaken + amount);
            achievements.AddBeDamageCount(damageTaken);
        }

        public void IncrementGold(int amount)
        {
            goldCollected = Mathf.Max(0, goldCollected + amount);
        }

        public void IncrementExperience(int amount)
        {
            experienceCollected = Mathf.Max(0, experienceCollected + amount);
        }

        // Optional: reset for session restart
        public void ResetAll()
        {
            enemyDowned = 0;
            damageDealt = 0f;
            healCount = 0f;
            damageTaken = 0f;
            goldCollected = 0;
            experienceCollected = 0;
        }

        #endregion

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
            GetPlayerReference();
            playerHealthComponent = Player.GetComponent<HealthComponent>();
            achievements = GetComponent<Achievements>();
            if (playerHealthComponent == null) Debug.LogError("Session Manager could not get player health component reference");
            playerHealthComponent.Damaged += OnPlayerDamaged;
            // Trigger the first level up immediately for the initial weapon unlock.
            // A small delay ensures other scripts have time to run their Awake() and OnEnable().
            Invoke(nameof(TriggerLevelUp), 0.1f);

        }

        private void OnPlayerDamaged(float amount, Vector3 pos, bool isCrit)
        {
            IncrementDamageTaken(amount);
        }

        public GameObject GetPlayerReference() 
        {
            if(Player == null)
            {
                Player = GameObject.FindGameObjectWithTag("Player");
            }
            return Player;
        }
        public void RestorePlayerHealth(int amount)// ensure this is the ONLY method to heal player
        {
            if (Player == null) { GetPlayerReference(); playerHealthComponent = Player.GetComponent<HealthComponent>(); }

            playerHealthComponent.Heal(amount);
            IncrementHeal(amount);
        }

        public void AddGold(int amount)
        {
            if (amount < 0) return;
            Gold += amount;
            GoldChanged?.Invoke(amount);
            IncrementGold(amount);
        }

        #region EXP manage
        public void AddExp(int amount)
        {
            if (amount < 0) return;
            currentExp += amount;
            CheckIfLevelUp();
            IncrementExperience(amount);
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