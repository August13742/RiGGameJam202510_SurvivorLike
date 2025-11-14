using UnityEngine;

public class EscapeButton : MonoBehaviour
{
    public GameObject panel;
    private bool isPause;

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        panel.SetActive(false);
    }
}