using AugustsUtility.UX;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Survivor.Game
{

    public enum GameMode
    {
        BossRush,
        SingleBoss,
        Survivor // future
    }


    public sealed class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        [Header("Scenes")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameplayScene = "BossArena";
        [SerializeField] private string gameplaySceneEasy = "BossArenaEasy";
        [SerializeField] private float fadeDuration = 0.5f;

        [SerializeField] private MusicResource TitleBGM;
        [SerializeField] private MusicPlaylist BGMPlaylist;

        // current run config
        public GameMode Mode { get; private set; }

        public BossDef SelectedBoss { get; private set; }                 // for SingleBoss
        public int SelectedBossStartingLevels { get; private set; } = 4;

        public BossRushRunDef SelectedBossRushRun { get; private set; }   // for BossRush

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            AudioManager.Instance?.PlayMusic(TitleBGM);
        }
        // -------- Public API (called by menu UI) --------

        public void StartBossRush(BossRushRunDef runDef)
        {
            Mode = GameMode.BossRush;
            SelectedBossRushRun = runDef;
            SelectedBoss = null;
            StartCoroutine(LoadGameplayWithFade());
        }

        public void StartBossRushEasy(BossRushRunDef runDef)
        {
            Mode = GameMode.BossRush;
            SelectedBossRushRun = runDef;
            SelectedBoss = null;
            StartCoroutine(LoadGameplayEasyWithFade());
        }


        public void StartSingleBoss(BossDef boss, int startingLevels = 5)
        {
            Mode = GameMode.SingleBoss;
            SelectedBoss = boss;
            SelectedBossStartingLevels = startingLevels;
            SelectedBossRushRun = null;
            StartCoroutine(LoadGameplayWithFade());
        }

        public void ReturnToMenu()
        {
            StartCoroutine(LoadSceneWithFade(mainMenuScene));
        }

        // -------- Internals --------

        private System.Collections.IEnumerator LoadGameplayWithFade()
        {
            yield return LoadSceneWithFade(gameplayScene);
        }
        private System.Collections.IEnumerator LoadGameplayEasyWithFade()
        {
            yield return LoadSceneWithFade(gameplaySceneEasy); 
        }

        private System.Collections.IEnumerator LoadSceneWithFade(string sceneName)
        {
            // fade to black
            if (CrossfadeManager.Instance != null)
            {
                CrossfadeManager.Instance.FadeToBlack(fadeDuration);
            }
            yield return new WaitForSeconds(fadeDuration);
            AudioManager.Instance?.PlayPlaylist(BGMPlaylist);
            SceneManager.LoadScene(sceneName);

            // one frame for scene to init
            yield return null;

            if (CrossfadeManager.Instance != null)
            {
                CrossfadeManager.Instance.FadeFromBlack(fadeDuration);
            }
        }
    }
}
