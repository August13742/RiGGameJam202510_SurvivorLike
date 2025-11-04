using UnityEngine;

public class AOEEffect : MonoBehaviour
{
    [Header("EffectOnTime")]
    [SerializeField] private float timeOut;

    [Header("PanelFadeTime")]
    [SerializeField] private float panelFadePrepareDuration;
    [SerializeField] private float panelFadeDuration;

    [SerializeField] private Transform player;
    [SerializeField] private AllEnemyAttack allEnemyAttack;

    private SpriteRenderer allOfEnemyEffectRenderer;
    private Color allOfEnemyEffectColor;
    private int panelFadeInCount = 0;
    private int panelFadeInCountLimit = 2;
    private float timeElapsed = 0;
    private float panelFadePrepareLimit = 0.3f;
    private float panelFadeLimit = 0.8f;
    private bool panelFadeInPrepare = false;
    private bool panelFadeOutPrepare = false;
    private bool panelFadeIn = false;
    private bool panelFadeOut = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        allOfEnemyEffectRenderer = GetComponent<SpriteRenderer>();
        allOfEnemyEffectColor = allOfEnemyEffectRenderer.color;
        allOfEnemyEffectColor.a = 0;
        allOfEnemyEffectRenderer.color = allOfEnemyEffectColor;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector2(player.transform.position.x, player.transform.position.y);
        timeElapsed += Time.deltaTime;

        if (timeElapsed >= timeOut)
        {
            Debug.Log("ŽžŠÔŒo‰ß");
            panelFadeInPrepare = true;
            timeElapsed = 0;
        }

        if (panelFadeInPrepare)
        {
            allOfEnemyEffectColor.a = Mathf.Lerp(0, panelFadePrepareLimit, timeElapsed / panelFadePrepareDuration);
            allOfEnemyEffectRenderer.color = allOfEnemyEffectColor;
            if (timeElapsed >= panelFadePrepareDuration)
            {
                panelFadeOutPrepare = true;
                panelFadeInPrepare = false;
                timeElapsed = 0;
            } 
        } else if (panelFadeOutPrepare)
        {
            allOfEnemyEffectColor.a = Mathf.Lerp(panelFadePrepareLimit, 0, timeElapsed / panelFadePrepareDuration);
            allOfEnemyEffectRenderer.color = allOfEnemyEffectColor;
            if ((timeElapsed >= panelFadePrepareDuration) && (panelFadeInCount < panelFadeInCountLimit))
            {
                panelFadeOutPrepare = false;
                panelFadeInPrepare = true;
                Debug.Log(panelFadeInCount);
                panelFadeInCount += 1;
                timeElapsed = 0;
            } else if ((timeElapsed >= panelFadePrepareDuration) && panelFadeInCount == panelFadeInCountLimit)
            {
                panelFadeOutPrepare = false;
                panelFadeIn = true;
                panelFadeInCount = 0;
                Debug.Log("panelFadeIn");
                allEnemyAttack.LayerEnemyAttack();
                timeElapsed = 0;
            }
        }

        if (panelFadeIn)
        {
            allOfEnemyEffectColor.a = Mathf.Lerp(0, panelFadeLimit, timeElapsed / panelFadeDuration);
            allOfEnemyEffectRenderer.color = allOfEnemyEffectColor;
            if (timeElapsed >= panelFadeDuration)
            {
                allOfEnemyEffectRenderer.color = allOfEnemyEffectColor;
                panelFadeIn = false;
                panelFadeOut = true;
                timeElapsed = 0;
            }
        } else if (panelFadeOut)
        {
            allOfEnemyEffectColor.a = Mathf.Lerp(panelFadeLimit, 0, timeElapsed / panelFadeDuration);
            allOfEnemyEffectRenderer.color = allOfEnemyEffectColor;
            if (timeElapsed >= panelFadeDuration)
            {
                allOfEnemyEffectColor.a = 0;
                allOfEnemyEffectRenderer.color = allOfEnemyEffectColor;
                panelFadeOut = false;
                timeElapsed = 0;
                Debug.Log("EffectOn");
            }
        }
    }
}
