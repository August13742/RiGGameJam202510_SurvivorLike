using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeCard : MonoBehaviour
{
    [SerializeField] RawImage icon;
    [SerializeField] TMP_Text id;
    [SerializeField] TMP_Text desc;

    public void Initialise(RawImage Icon, string iid, string des)
    {
        icon = Icon;
        id.text = iid;
        desc.text = des;
    }
}
