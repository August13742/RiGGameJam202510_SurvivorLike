using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AugustsUtility.Tween;
public sealed class AchievementPopup : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image icon;                 // optional
    [SerializeField] private CanvasGroup canvasGroup;    // optional fade; can be null

    [Header("Motion")]
    [SerializeField] private float yOffset = 25f;        // designer-tuned
    [SerializeField] private float upDuration = 0.65f;
    [SerializeField] private float holdDuration = 1.35f;
    [SerializeField] private float downDuration = 0.5f;

    [Header("Fade (optional)")]
    [SerializeField] private bool fadeInOut = true;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    private RectTransform _rt;
    private Vector2 _start;

    private void Awake()
    {
        _rt = transform as RectTransform;
        if (_rt == null)
        {
            Debug.LogWarning("AchievementPopup expects a RectTransform.");
        }
    }

    /// <summary>Inject message and optional sprite before playing.</summary>
    public void Configure(string message, Sprite sprite = null)
    {
        if (label != null) label.text = message;
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
    }

    /// <summary>Kick the animation and auto-destroy when done.</summary>
    public void PlayAndAutoDestroy()
    {
        _start = _rt.anchoredPosition;

        // Vertical move up

        Vector2 from = _start;
        Vector2 to = new (from.x, from.y + yOffset);
        _rt.TweenAnchoredPosition(to, upDuration, EasingFunctions.EaseOutCubic);


        

        // Optional fade-in
        if (fadeInOut && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.TweenAlpha(1f, fadeInDuration, EasingFunctions.EaseOutCubic);
        }

        // Chain the downward motion + fade-out -> destroy
        StartCoroutine(DownAndDestroyAfterHold());
    }

    private IEnumerator DownAndDestroyAfterHold()
    {
        yield return new WaitForSeconds(holdDuration);

            Vector2 from = _rt.anchoredPosition;
            Vector2 to = new Vector2(from.x, _start.y);
        _rt.TweenAnchoredPosition(to, downDuration, EasingFunctions.EaseOutCubic, onComplete: () => Destroy(gameObject));



        if (fadeInOut && canvasGroup != null)
        {
            canvasGroup.TweenAlpha(0f, fadeOutDuration, EasingFunctions.EaseOutCubic);
        }
    }
}
