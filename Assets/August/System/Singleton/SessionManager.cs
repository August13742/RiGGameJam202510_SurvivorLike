using Survivor.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Survivor.Game
{
    [DisallowMultipleComponent]
    public sealed class SessionManager : MonoBehaviour
    {
        // -------- Player / Session state --------
        public static SessionManager Instance { get; private set; }

        [Header("Player / Session")]
        [SerializeField] private GameObject player;
        private HealthComponent _playerHealth;

        [SerializeField] SimplePauseMenu pauseMenu;
        public int Gold { get; private set; } = 0;
        public int PlayerLevel { get; private set; } = 0;

        [SerializeField] private int currentExp = 20;
        [SerializeField] private int currentExpReq = 5;
        [SerializeField] private int baseExpReq = 5;
        public int ExpGrowthPerLevel = 5;

        public Action LevelUp;
        public Action<int> GoldChanged;

        // -------- Statistics (authoritative) --------
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

        // -------- Achievement UI --------
        [Header("Achievement UI")]
        [SerializeField] private Transform uiCanvas;                // Canvas transform (parent)
        [SerializeField] private GameObject achievementPrefab;      // Prefab with child "AchieveText" (TMP)


        // Thresholds (edit in Inspector)
        [Header("Achievement Thresholds")]
        [SerializeField] private int[] enemyDownThresholds = new[] { 100, 1000, 10000, 100000 };
        [SerializeField] private int[] dmgDealtThresholds = new[] { 100, 1000, 10000, 100000 };
        [SerializeField] private int[] dmgTakenThresholds = new[] { 100, 1000, 10000 };

        // unlocked sets to avoid duplicate popups
        private readonly HashSet<int> _enemyDownUnlocked = new HashSet<int>();
        private readonly HashSet<int> _dmgDealtUnlocked = new HashSet<int>();
        private readonly HashSet<int> _dmgTakenUnlocked = new HashSet<int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            //DontDestroyOnLoad(this);
        }

        private void Start()
        {
            GetPlayerReference();
            if (player != null)
            {
                _playerHealth = player.GetComponent<HealthComponent>();
                if (_playerHealth == null)
                {
                    Debug.LogError("SessionManager: Player has no HealthComponent.");
                }
                else
                {
                    _playerHealth.Damaged += OnPlayerDamaged;
                    _playerHealth.Died += OnPlayerDied;
                }
            }

            // Kick an initial level-up after systems are awake.
            //Invoke(nameof(TriggerLevelUp), 0.1f);
        }
        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.Damaged -= OnPlayerDamaged;
                _playerHealth.Died -= OnPlayerDied;
            }
        }
        // ---------- Run bootstrap helpers ----------

        /// Hard reset between runs / scenes.
        /// Does NOT touch the player GameObject, only session values.
        public void ResetRun(int startingLevel = 0, int startingGold = 0)
        {
            // XP & level
            PlayerLevel = 0;
            currentExp = 0;
            currentExpReq = baseExpReq;

            // Currency
            Gold = Mathf.Max(0, startingGold);
            GoldChanged?.Invoke(Gold);

            // Stats / achievements
            ResetAllStats();

            // Pre-grant levels (this fires LevelUp and thus ProgressionManager)
            if (startingLevel > 0)
            {
                for (int i = 0; i < startingLevel; i++)
                {
                    TriggerLevelUp();
                }
            }
        }

        // ---------- Player reference ----------
        public GameObject GetPlayerReference()
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            return player;
        }
        public Vector3 GetPlayerPosition()
        {
            if (player == null) GetPlayerReference();
            return player.transform.position;
        }
        public float GetCurrentExpPercent()
        {
            if (currentExpReq <= 0) return 0f;
            return (float)currentExp / (float)currentExpReq;
        }

        private void OnPlayerDamaged(float amount, Vector3 pos, bool isCrit)
        {
            AugustsUtility.CameraShake.CameraShake2D.Shake(strength: 0.1f);
            IncrementDamageTaken(amount);
            VFX.ScreenDamageOverlay.Instance?.Flash();
        }

        private void OnPlayerDied()
        {
            // Full red overlay
            Survivor.VFX.ScreenDamageOverlay.Instance?.ShowFull();

            // Turn off player control just in case something uses unscaled time
            if (player != null)
            {
                var controller = player.GetComponent<Survivor.Control.PlayerController>();
                if (controller) controller.enabled = false;
            }

            // Show Game Over menu
            if (pauseMenu != null)
            {
                pauseMenu.ShowGameOver();
            }
            else
            {
                Debug.LogWarning("SessionManager: SimplePauseMenu not found for Game Over.");
            }
        }



        // ---------- Public stat mutation API (authoritative) ----------
        public void IncrementEnemyDowned(int amount = 1)
        {
            if (amount <= 0) return;
            enemyDowned = Mathf.Max(0, enemyDowned + amount);
            EvaluateAndShowAchievements_Int(enemyDowned, enemyDownThresholds, _enemyDownUnlocked, "enemyDown");
        }

        public void IncrementDamageDealt(float amount)
        {
            //return;
            if (amount <= 0f) return;
            damageDealt = Mathf.Max(0f, damageDealt + amount);
            // Cast to int for integer thresholds (e.g., 100, 1000, �c)
            //return;
            EvaluateAndShowAchievements_Int((int)damageDealt, dmgDealtThresholds, _dmgDealtUnlocked, "damage");
        }

        public void IncrementHeal(float amount)
        {
            if (amount <= 0f) return;
            healCount = Mathf.Max(0f, healCount + amount);
        }

        public void IncrementDamageTaken(float amount)
        {
            if (amount <= 0f) return;
            damageTaken = Mathf.Max(0f, damageTaken + amount);
            EvaluateAndShowAchievements_Int((int)damageTaken, dmgTakenThresholds, _dmgTakenUnlocked, "beDamaged");
        }

        public void IncrementGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            GoldChanged?.Invoke(amount);
            goldCollected = Mathf.Max(0, goldCollected + amount);
        }

        public void IncrementExperience(int amount)
        {
            if (amount <= 0) return;
            experienceCollected = Mathf.Max(0, experienceCollected + amount);
            currentExp += amount;
            CheckIfLevelUp();
        }

        // Optional reset
        public void ResetAllStats()
        {
            enemyDowned = 0;
            damageDealt = 0f;
            healCount = 0f;
            damageTaken = 0f;
            goldCollected = 0;
            experienceCollected = 0;

            _enemyDownUnlocked.Clear();
            _dmgDealtUnlocked.Clear();
            _dmgTakenUnlocked.Clear();
        }

        // ---------- EXP / level ----------
        private void CheckIfLevelUp()
        {
            if (currentExp >= currentExpReq)
            {
                int residual = currentExp - currentExpReq;
                TriggerLevelUp();
                currentExp = residual;
                // recurse if residual is still enough
                CheckIfLevelUp();
            }
        }

        public void TriggerLevelUp()
        {
            PlayerLevel += 1;
            currentExpReq += ExpGrowthPerLevel;
            Debug.Log($"<color=orange>LEVEL UP! Now Level {PlayerLevel}.</color>");
            LevelUp?.Invoke();
        }


        public void RestorePlayerHealth(float amount)
        {
            if (player == null)
            {
                GetPlayerReference();
            }
            if (_playerHealth == null && player != null)
            {
                _playerHealth = player.GetComponent<HealthComponent>();
            }

            if (_playerHealth != null)
            {
                _playerHealth.Heal(amount);
                DamageTextManager.Instance.ShowHeal(GetPlayerPosition(), amount);
                IncrementHeal(amount);
            }
        }

        // ---------- Achievement evaluation ----------
        private void EvaluateAndShowAchievements_Int(int current, int[] thresholds, HashSet<int> unlocked, string key)
        {
            //return;
            if (thresholds == null || thresholds.Length == 0) return;
            // evaluate in ascending order; fire the highest just-reached threshold
            for (int i = 0; i < thresholds.Length; i++)
            {
                int th = thresholds[i];
                if (current >= th && !unlocked.Contains(th))
                {
                    unlocked.Add(th);
                    SpawnPopup(th, key);
                }
            }
        }

        // ---------- Popup ----------
        private void SpawnPopup(int number, string kind)
        {
            if (uiCanvas == null || achievementPrefab == null)
            {
                Debug.LogWarning("SessionManager: Missing UI Canvas or Achievement Prefab.");
                return;
            }

            GameObject go = Instantiate(achievementPrefab, uiCanvas, false);
            var popup = go.GetComponent<AchievementPopup>();
            if (popup == null)
            {
                Debug.LogWarning("SessionManager: Prefab lacks AchievementPopup.");
                Destroy(go);
                return;
            }

            string msg = kind switch
            {
                "enemyDown" => number.ToString() + "Enemy Down!",
                "damage" => number.ToString() + "Damage Dealt!",
                "beDamaged" => number.ToString() + "Damage Taken!",
                _ => number.ToString(),
            };
            Sprite icon = null;

            popup.Configure(msg, icon);
            popup.PlayAndAutoDestroy();
        }

        // ---------- Public convenience (external callers) ----------
        public void AddExp(int amount) => IncrementExperience(amount);
        public void AddGold(int amount) => IncrementGold(amount);
    }
}
