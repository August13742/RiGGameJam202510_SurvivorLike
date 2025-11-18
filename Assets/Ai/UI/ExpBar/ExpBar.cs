using Survivor.Game;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ExpBar : MonoBehaviour
{

    [SerializeField] private GameObject expFill;
    [SerializeField] private SessionManager SessionManager;
    private float currentExpPercent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SessionManager = SessionManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        currentExpPercent = SessionManager.GetCurrentExpPercent(); /*SessionManagerのコードでfloatにキャストすればうまくいく*/
            
        expFill.transform.localScale = new Vector3(currentExpPercent, 1, 1);
    }
}
