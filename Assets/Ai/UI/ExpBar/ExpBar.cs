using Survivor.Game;
using UnityEngine;

public class ExpBar : MonoBehaviour
{

    [SerializeField] private GameObject expFill;
    [SerializeField] private SessionManager SessionManager;
    private float currentExpPercent;

    private void Awake()
    {
        expFill.transform.localScale = new Vector3(0, 1, 1);
    }
    void Start()
    {
        SessionManager = SessionManager.Instance;
        SessionManager.ExpChanged += OnExpChanged;
    }
    void OnExpChanged(int amt)
    {
        currentExpPercent = SessionManager.GetCurrentExpPercent(); /*SessionManager�̃R�[�h��float�ɃL���X�g����΂��܂�����*/

        expFill.transform.localScale = new Vector3(currentExpPercent, 1, 1);
    }
    private void OnDestroy()
    {
        SessionManager.ExpChanged -= OnExpChanged;
    }
}
