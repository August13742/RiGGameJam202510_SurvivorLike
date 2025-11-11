using UnityEngine;
using UnityEngine.InputSystem;

public class GameStop : MonoBehaviour
{
    public GameObject panel;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            bool panelState = !panel.activeSelf;
            panel.SetActive(panelState);
        }
    }
}
