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
            if (isPause)
            {
                ResumeGame();
                panel.SetActive(false);
            }
            else
            {
                if (Time.timeScale > 0f)
                {
                    PauseGame();
                    panel.SetActive(true);
                }
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
