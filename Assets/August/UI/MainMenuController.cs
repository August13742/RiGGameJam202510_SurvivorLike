using UnityEngine;
using UnityEngine.UI;
using Survivor.Game;
using Survivor.Progression.UI;

namespace Survivor.UI
{
    public sealed class SurvivorMainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuRoot;          // main menu panel
        [SerializeField] private BossSelectMenuUI bossSelectMenu;  // scene instance under Canvas

        [Header("Buttons")]
        [SerializeField] private Button bossRushButton;
        [SerializeField] private Button singleBossButton;
        [SerializeField] private Button quitButton;

        [Header("Defs")]
        [SerializeField] private BossRushRunDef bossRushRun;
        [SerializeField] private BossDef[] availableBosses;        // all bosses for single-boss mode

        [Header("Single Boss Settings")]
        [SerializeField] private int singleBossStartingLevels = 5;

        private void Start()
        {
            if (bossRushButton)
                bossRushButton.onClick.AddListener(OnBossRushClicked);

            if (singleBossButton)
                singleBossButton.onClick.AddListener(OnSingleBossClicked);

            if (quitButton)
                quitButton.onClick.AddListener(Application.Quit);
        }

        private void OnBossRushClicked()
        {
            GameModeManager.Instance.StartBossRush(bossRushRun);
        }

        private void OnSingleBossClicked()
        {
            if (!bossSelectMenu)
            {
                Debug.LogWarning("[MainMenu] BossSelectMenu not wired.");
                return;
            }
            bossSelectMenu.gameObject.SetActive(true);
            bossSelectMenu.Show(availableBosses, OnBossPicked, "Select Boss");
        }

        private void OnBossPicked(BossDef boss)
        {
            if (!boss)
            {
                Debug.LogWarning("[MainMenu] Boss pick was null.");
                // go back to main menu if something went wrong
                if (mainMenuRoot) mainMenuRoot.SetActive(true);
                return;
            }

            GameModeManager.Instance.StartSingleBoss(boss, singleBossStartingLevels);
        }
    }
}
