using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeaponList : MonoBehaviour
{
    [Header("Writing")]
    [SerializeField] private Transform root;
    [SerializeField] private GameObject weaponUIPrefab;
    //private Survivor.Weapon.WeaponDef def;


    private GameObject weaponIcon;
    private Image _icon;
    private List<GameObject> weaponIcons = new List<GameObject>();

    private void Start()
    {
        
    }
    public void ShowInWeaponList(Sprite icon)
    {   
        foreach (GameObject weapon in weaponIcons)
        {
            if (icon == weapon.GetComponent<Image>().sprite) return;
        }
        weaponIcon = Instantiate(weaponUIPrefab, root);
        weaponIcons.Add(weaponIcon);
        _icon = weaponIcon.GetComponent<Image>();
        _icon.sprite = icon;
    }
}
