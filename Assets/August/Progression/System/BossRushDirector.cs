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

        // cached from BossRushRunDef (or defaults)
        private bool _enableHpScaling;
        private float _hpScalePerIndex;

        private void Start()
        {
            var gm = GameModeManager.Instance;
            var sm = SessionManager.Instance;

            if (gm == null || gm.Mode != GameMode.BossRush)
            {
                enabled = false;
                return;
            }

            var run = gm.SelectedBossRushRun;

            if (run != null && run.Sequence != null && run.Sequence.Length > 0)
            {
                _sequence = run.Sequence;
                sm?.ResetRun(run.StartingLevels, run.StartingGold);

                // pull scaling config from SO
                _enableHpScaling = run.EnableHpScaling;
                _hpScalePerIndex = run.HpScalePerIndex;
            }
            else
            {
                _sequence = fallbackSequence;
                sm?.ResetRun(fallbackStartingLevels, fallbackStartingGold);

                // sensible defaults if no run def
                _enableHpScaling = false;
                _hpScalePerIndex = 0f;
            }

            StartCoroutine(BossRushRoutine());
        }

        private IEnumerator BossRushRoutine()
        {
            var sm = SessionManager.Instance;

            // Small delay to not fight crossfade
            yield return new WaitForSeconds(1f);

            // Starting level-up orb at boss spawn
            if (LootManager.Instance != null && initialLevelUpDropDef != null && sm != null && bossSpawnPoint != null)
            {
                LootManager.Instance.SpawnSingle(initialLevelUpDropDef, bossSpawnPoint.position);
            }

            yield return StartCoroutine(SpawnNextBossRoutine());
        }

        private IEnumerator SpawnNextBossRoutine()
        {
            _currentIndex++;

            if (_currentIndex >= _sequence.Length)
            {
                yield return new WaitForSeconds(3f);
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
                yield return StartCoroutine(SpawnNextBossRoutine());
                yield break;
            }

            // Countdown before boss spawn
            if (countdownPrefab != null && uiCanvas != null && bossCountdownSeconds > 0f)
            {
                var cd = Instantiate(countdownPrefab, uiCanvas);
                cd.StartCountdown(bossCountdownSeconds, useUnscaled: false);
                yield return new WaitForSeconds(bossCountdownSeconds);
            }

            _currentBoss = Instantiate(def.Prefab, bossSpawnPoint.position, Quaternion.identity);
            _currentBoss.HP.Died += OnBossDied;

            // ---- HP SCALING FROM RUN DEF ----
            if (_enableHpScaling && _currentBoss.HP != null)
            {
                float baseMax = _currentBoss.HP.Max;
                float scale = 1f + _hpScalePerIndex * _currentIndex;
                if (scale < 0f) scale = 0f; // in case someone sets negative values for experiments

                float newMax = baseMax * scale;
                _currentBoss.HP.SetMaxHP(newMax, true);

                Debug.Log($"[BossRush] Boss {_currentIndex} '{def.name}' HP scaled: {baseMax} -> {newMax} (x{scale:0.00})");
            }
        }

        private void OnBossDied(Vector3 killDir, float overkill)
        {
            StartCoroutine(BossDeathRoutine());
        }

        private IEnumerator BossDeathRoutine()
        {
            var sm = SessionManager.Instance;

            yield return new WaitForSeconds(3f);

            if (LootManager.Instance != null && fullHealDropDef != null && sm != null && bossSpawnPoint != null)
            {
                LootManager.Instance.SpawnSingle(fullHealDropDef, bossSpawnPoint.position);
            }

            yield return StartCoroutine(SpawnNextBossRoutine());
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
