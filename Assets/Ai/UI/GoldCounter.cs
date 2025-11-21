using UnityEngine;
using TMPro;
using Survivor.Game;

public class GoldCounter : MonoBehaviour
{

    [SerializeField] private SessionManager SessionManager;
    private TextMeshProUGUI textComponent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SessionManager = SessionManager.Instance;
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        textComponent.text = SessionManager.GoldCollected.ToString();
    }
}
