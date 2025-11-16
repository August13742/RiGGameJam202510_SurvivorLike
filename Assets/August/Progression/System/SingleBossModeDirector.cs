using System.Collections;
using Survivor.Drop;
using Survivor.Enemy.FSM;
using UnityEngine;

namespace Survivor.Game
{
    public sealed class SingleBossModeDirector : MonoBehaviour
    {
        [Header("Fallback (editor test)")]
        [SerializeField] private BossDef fallbackBoss;
        [SerializeField] private int fallbackStartingLevels = 5;

        [Header("Spawn / map")]
        [SerializeField] private Transform bossSpawnPoint;

        [Header("Starting Level-Up Items")]
        [SerializeField] private DropItemDef levelUpDropDef;
        [SerializeField] private float spawnRadius = 1.5f;

        [Header("Countdown")]
        [SerializeField] private float startCountdownSeconds = 3f;
        [SerializeField] private Rhythm.UI.CountDownText countdownPrefab;
        [SerializeField] private Transform uiCanvas;

        private BossDef _bossDef;
        private BossController _bossInstance;
        private bool _bossStarted;
        private bool _runFinished;
        private int _targetLevel;

        private void Start()
        {
            var gm = GameModeManager.Instance;

            if (gm == null || gm.Mode != GameMode.SingleBoss)
            {
                enabled = false;
                return;
            }

            _bossDef = gm.SelectedBoss ?? fallbackBoss;
            _targetLevel = gm.SelectedBoss != null
                ? gm.SelectedBossStartingLevels
                : fallbackStartingLevels;

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.ResetRun(startingLevel: 0, startingGold: 0);
            }

            SpawnStartingLevelUpItems();
        }

        private void SpawnStartingLevelUpItems()
        {
            if (LootManager.Instance == null || levelUpDropDef == null)
            {
                Debug.LogWarning("[SingleBoss] Missing LootManager or levelUpDropDef.");
                return;
            }

            var sm = SessionManager.Instance;
            if (sm == null) return;

            Vector2 center = sm.GetPlayerPosition();

            int count = Mathf.Max(1, _targetLevel);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = spawnRadius > 0f
                    ? Random.insideUnitCircle * spawnRadius
                    : Vector2.zero;

                LootManager.Instance.SpawnSingle(levelUpDropDef, center + offset);
            }
        }

        private void Update()
        {
            if (_bossStarted || _runFinished) return;
            var sm = SessionManager.Instance;
            if (!sm) return;

            if (sm.PlayerLevel >= _targetLevel)
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    StartBossWithCountdown();
                }
            }
        }

        private void StartBossWithCountdown()
        {
            if (_bossStarted) return;
            _bossStarted = true;

            StartCoroutine(StartBossRoutine());
        }

        private IEnumerator StartBossRoutine()
        {
            if (countdownPrefab != null && uiCanvas != null && startCountdownSeconds > 0f)
            {
                var cd = Instantiate(countdownPrefab, uiCanvas);
                cd.StartCountdown(startCountdownSeconds, useUnscaled: false);

                // scaled time so countdown pauses with upgrades
                yield return new WaitForSeconds(startCountdownSeconds);
            }

            SpawnBoss();
        }

        private void SpawnBoss()
        {
            if (_bossDef == null || _bossDef.Prefab == null)
            {
                Debug.LogError("[SingleBoss] BossDef or Prefab missing.");
                return;
            }

            _bossInstance = Instantiate(_bossDef.Prefab, bossSpawnPoint.position, Quaternion.identity);
            _bossInstance.HP.Died += OnBossDied;
        }

        private void OnBossDied()
        {
            if (_runFinished) return;
            _runFinished = true;
            Debug.Log("<color=cyan>Single Boss defeated.</color>");
            GameModeManager.Instance?.ReturnToMenu();
        }
    }
}
