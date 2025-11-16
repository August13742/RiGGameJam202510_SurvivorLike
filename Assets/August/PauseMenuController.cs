using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Survivor.UI
{
    public sealed class SimplePauseMenu : MonoBehaviour
    {
        private enum MenuMode
        {
            None,
            Pause,
            GameOver,
            Victory
        }

        [Header("Wiring")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        [Header("Scenes")]
        [SerializeField] private string mainMenuScene = "MainMenu";

        private bool _active;
        private MenuMode _mode = MenuMode.None;

        private void Start()
        {
            if (panel) panel.SetActive(false);

            if (resumeButton) resumeButton.onClick.AddListener(Resume);
            if (restartButton) restartButton.onClick.AddListener(Restart);
            if (quitButton) quitButton.onClick.AddListener(QuitToMenu);

            if (titleText) titleText.text = "Paused";
        }

        private void Update()
        {
            // Only ESC-toggle in "normal" gameplay Å® pause mode
            if (_mode == MenuMode.GameOver || _mode == MenuMode.Victory)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_active) Resume();
                else Pause();
            }
        }

        private void Pause()
        {
            _mode = MenuMode.Pause;
            _active = true;
            Time.timeScale = 0f;

            if (panel) panel.SetActive(true);
            if (titleText) titleText.text = "Paused";
            if (resumeButton) resumeButton.gameObject.SetActive(true);
        }

        private void Resume()
        {
            _mode = MenuMode.None;
            _active = false;
            Time.timeScale = 1f;

            if (panel) panel.SetActive(false);
        }

        private void Restart()
        {
            _mode = MenuMode.None;
            _active = false;
            Time.timeScale = 1f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void QuitToMenu()
        {
            _mode = MenuMode.None;
            _active = false;
            Time.timeScale = 1f;

            SceneManager.LoadScene(mainMenuScene);
        }

        // ---------- External APIs ----------

        public void ShowGameOver()
        {
            _mode = MenuMode.GameOver;
            _active = true;
            Time.timeScale = 0f;

            if (panel) panel.SetActive(true);
            if (titleText) titleText.text = "Game Over:(";

            // No resume on death
            if (resumeButton) resumeButton.gameObject.SetActive(false);
        }

        public void ShowVictory()
        {
            _mode = MenuMode.Victory;
            _active = true;
            Time.timeScale = 0f;

            if (panel) panel.SetActive(true);
            if (titleText) titleText.text = "Victory!";

            // No resume after clear either
            if (resumeButton) resumeButton.gameObject.SetActive(false);
        }
    }
}
