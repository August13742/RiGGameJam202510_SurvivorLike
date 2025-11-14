using UnityEngine;
using UnityEngine.InputSystem;

public class GamePause : MonoBehaviour
{
    public GameObject panel;
    private bool isPause = false;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool panelState = !panel.activeSelf;
            panel.SetActive(panelState);

            if (isPause)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
        isPause = true;
    }

    void ResumeGame()
    {
        Time.timeScale = 1f;
        isPause = false;
    }
}
