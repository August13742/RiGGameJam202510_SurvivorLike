using System.Collections;
using Survivor.Control;
using Survivor.Drop;
using Survivor.Enemy.FSM;
using UnityEngine;

namespace Survivor.Game
{
    public sealed class BossRushDirector : MonoBehaviour
    {
        [Header("Fallback sequence (editor test)")]
        [SerializeField] private BossDef[] fallbackSequence;
        [SerializeField] private int fallbackStartingLevels = 0;
        [SerializeField] private int fallbackStartingGold = 0;

        [Header("Spawn")]
        [SerializeField] private Transform bossSpawnPoint;

        [Header("Special Drops")]
        [SerializeField] private DropItemDef initialLevelUpDropDef;
        [SerializeField] private DropItemDef fullHealDropDef;

        [Header("Countdown")]
        [SerializeField] private float bossCountdownSeconds = 3f;
        [SerializeField] private Rhythm.UI.CountDownText countdownPrefab;
        [SerializeField] private Transform uiCanvas;

        private BossDef[] _sequence;
        private int _currentIndex = -1;
        private BossController _currentBoss;

        private void Start()
        {
            var gm = GameModeManager.Instance;
            var sm = SessionManager.Instance;

            if (gm == null || gm.Mode != GameMode.BossRush)
            {
                enabled = false;
                return;
            }

            // Resolve run data
            var run = gm.SelectedBossRushRun;
            if (run != null && run.Sequence != null && run.Sequence.Length > 0)
            {
                _sequence = run.Sequence;
                sm?.ResetRun(run.StartingLevels, run.StartingGold);
            }
            else
            {
                _sequence = fallbackSequence;
                sm?.ResetRun(fallbackStartingLevels, fallbackStartingGold);
            }

            // Starting level-up orb
            if (LootManager.Instance != null && initialLevelUpDropDef != null && sm != null)
            {
                Vector2 p = sm.GetPlayerPosition();
                LootManager.Instance.SpawnSingle(initialLevelUpDropDef, p);
            }

            StartCoroutine(SpawnNextBossRoutine());
        }

        private IEnumerator SpawnNextBossRoutine()
        {
            _currentIndex++;

            if (_currentIndex >= _sequence.Length)
            {
                OnBossRushCompleted();
                yield break;
            }

            if (_currentBoss != null)
                _currentBoss.HP.Died -= OnBossDied;

            BossDef def = _sequence[_currentIndex];
            if (def == null || def.Prefab == null)
            {
                Debug.LogWarning($"[BossRush] BossDef at index {_currentIndex} is null.");
                yield return null;
                StartCoroutine(SpawnNextBossRoutine());
                yield break;
            }

            // Countdown before boss spawn
            if (countdownPrefab != null && uiCanvas != null && bossCountdownSeconds > 0f)
            {
                var cd = Instantiate(countdownPrefab, uiCanvas);
                cd.StartCountdown(bossCountdownSeconds, useUnscaled: false);

                // Use scaled time so countdown pauses with the game.
                yield return new WaitForSeconds(bossCountdownSeconds);
            }

            _currentBoss = Instantiate(def.Prefab, bossSpawnPoint.position, Quaternion.identity);
            _currentBoss.HP.Died += OnBossDied;
        }

        private void OnBossDied()
        {
            var sm = SessionManager.Instance;
            if (LootManager.Instance != null && fullHealDropDef != null && sm != null)
            {
                LootManager.Instance.SpawnSingle(fullHealDropDef, sm.GetPlayerPosition());
            }

            StartCoroutine(SpawnNextBossRoutine());
        }

        private void OnBossRushCompleted()
        {
            Debug.Log("<color=magenta>Boss Rush complete!</color>");

            var menu = FindFirstObjectByType<UI.SimplePauseMenu>();
            if (menu != null)
            {
                menu.ShowVictory();
            }
            else
            {
                GameModeManager.Instance?.ReturnToMenu();
            }
        }

    }
}
