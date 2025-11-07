using UnityEngine;
using UnityEngine.InputSystem;

public class GameStop : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("E");
        }
    }
}
