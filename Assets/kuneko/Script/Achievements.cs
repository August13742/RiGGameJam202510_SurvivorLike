using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

public class Achievements : MonoBehaviour
{
    //public static Achievements Instance { get; private set; }
    [Header("UICanvas")]
    [SerializeField] private Transform uICanvas;

    [SerializeField] private GameObject achievement;

    //[HideInInspector] public int EnemyDownCount = 0;
    //[HideInInspector] public float DamageCount = 0;
    //[HideInInspector] public float BeDamagedCount = 0;

    private Vector2 defaultPos = new Vector2(225f, -345f);
    private float defaultYPos = -345f;
    private float moveYPos = -140f;

    private bool oneHundredEnemyDown = false;
    private bool oneHundredDamage = false;
    private bool oneHundredBeDamaged = false;
    private bool oneThousandEnemyDown = false;
    private bool oneThousandDamage = false;
    private bool oneThousandBeDamaged = false;
    private bool tenThousandEnemyDown = false;
    private bool tenThousandDamage = false;
    private bool tenThousandBeDamaged = false;
    private bool oneHundredThousandEnemyDown = false;
    private bool oneHundredThousandDamage = false;

    private void Start()
    {
        achievement.transform.localPosition = defaultPos;
    }

    /// <summary>
    /// 倒した敵の数に応じたアチーブメントを表示
    /// </summary>
    /// <param name="enemyDown"></param>
    public void AddEnemyCount(int enemyDown)
    {
        //EnemyDownCount += enemyDown;
        if (enemyDown >= 100 && !oneHundredEnemyDown)
        {
            Debug.Log("100体倒した");
            StartCoroutine(ShowPopUp(100, "enemyDown"));
            oneHundredEnemyDown = true;
        } else if (enemyDown >= 1000 && !oneThousandEnemyDown)
        {
            StartCoroutine(ShowPopUp(1000, "enemyDown"));
            oneThousandEnemyDown = true;
        } else if (enemyDown >= 10000 && !tenThousandEnemyDown)
        {
            StartCoroutine(ShowPopUp(10000, "enemyDown"));
            tenThousandEnemyDown = true;
        } else if (enemyDown >= 100000 && !oneHundredThousandEnemyDown)
        {
            StartCoroutine(ShowPopUp(100000, "enemyDown"));
            oneHundredThousandEnemyDown = true;
        }
    }

    /// <summary>
    /// 与えたダメージに応じたアチーブメントを表示
    /// </summary>
    /// <param name="damage"></param>
    public void AddDamageCount(float damage)
    {
        //DamageCount += damage;
        if (damage == 100 && !oneHundredDamage)
        {
            StartCoroutine(ShowPopUp(100, "damage"));
            oneHundredDamage = true;
        } else if (damage >= 1000 && !oneThousandDamage)
        {
            StartCoroutine(ShowPopUp(1000, "damage"));
            oneThousandDamage = true;
        } else if (damage >= 10000 && !tenThousandDamage)
        {
            StartCoroutine(ShowPopUp(10000, "damage"));
            tenThousandDamage = true;
        } else if (damage >= 100000 && !oneHundredThousandDamage)
        {
            StartCoroutine(ShowPopUp(100000, "damage"));
            oneHundredThousandDamage = true;
        }
    }

    /// <summary>
    /// 受けたダメージに応じたアチーブメントを表示
    /// </summary>
    /// <param name="beDamaged"></param>
    public void AddBeDamageCount(float beDamaged)
    {
        //BeDamagedCount += beDamaged;
        if (beDamaged == 100 && !oneHundredBeDamaged)
        {
            StartCoroutine(ShowPopUp(100, "beDamaged"));
            oneHundredBeDamaged = true;
        } else if (beDamaged >= 1000 && !oneThousandBeDamaged)
        {
            StartCoroutine(ShowPopUp(1000, "beDamaged"));
            oneThousandBeDamaged = true;
        } else if (beDamaged >= 10000 && !tenThousandBeDamaged)
        {
            StartCoroutine(ShowPopUp(10000, "beDamaged"));
            tenThousandBeDamaged = true;
        }
    }

    /// <summary>
    /// ポップアップを表示
    /// </summary>
    /// <returns></returns>
    IEnumerator ShowPopUp(int num, string str)
    {
        var instance = Instantiate(achievement, uICanvas, false);
        var child = instance.transform.Find("AchieveText").gameObject;
        TextMeshProUGUI TMPro = child.GetComponent<TextMeshProUGUI>();
        if (str == "enemyDown")
        {
            TMPro.text = num + "体の敵を倒した!";
        } else if (str == "damage")
        {
            TMPro.text = num + "ダメージを与えた!";
        } else if (str == "beDamaged")
        {
            TMPro.text = num + "ダメージくらった!";
        }
        instance.transform.DOLocalMoveY(moveYPos, 1f);
        yield return new WaitForSeconds(3f);
        instance.transform.DOLocalMoveY(defaultYPos, 1f).OnComplete(() =>
            {
                Destroy(instance);
            });
    }
}
