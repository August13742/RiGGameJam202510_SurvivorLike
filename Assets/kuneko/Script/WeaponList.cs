using UnityEngine;
using UnityEngine.UI;

public class WeaponList : MonoBehaviour
{
    [Header("Writing")]
    [SerializeField] private Transform root;
    [SerializeField] private GameObject weaponUIPrefab;
    //private Survivor.Weapon.WeaponDef def;


    private GameObject weaponIcon;
    private Image _icon;

    private void Start()
    {
        
    }
    public void ShowInWeaponList(Sprite icon)
    {
        weaponIcon = Instantiate(weaponUIPrefab, root);
        _icon = weaponIcon.GetComponent<Image>();
        _icon.sprite = icon;
    }
}
