using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        _start = _rt != null ? _rt.anchoredPosition : (Vector2)transform.localPosition;

        // Vertical move up
        if (_rt != null)
        {
            Vector2 from = _start;
            Vector2 to = new Vector2(from.x, from.y + yOffset);
            CustomTween.To(from, to, upDuration,
                ease: EasingFunctions.EaseOutCubic,
                onUpdate: v => _rt.anchoredPosition = v);
        }
        else
        {
            Vector3 from = transform.localPosition;
            Vector3 to = new Vector3(from.x, from.y + yOffset, from.z);
            CustomTween.To(from, to, upDuration,
                EasingFunctions.EaseOutCubic,
                v => transform.localPosition = v);
        }

        // Optional fade-in
        if (fadeInOut && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            CustomTween.To(0f, 1f, Mathf.Max(0.01f, fadeInDuration),
                ease: EasingFunctions.EaseOutCubic,
                onUpdate: a => canvasGroup.alpha = a);
        }

        // Chain the downward motion + fade-out -> destroy
        StartCoroutine(DownAndDestroyAfterHold());
    }

    private IEnumerator DownAndDestroyAfterHold()
    {
        yield return new WaitForSeconds(holdDuration);

        if (_rt != null)
        {
            Vector2 from = _rt.anchoredPosition;
            Vector2 to = new Vector2(from.x, _start.y);
            CustomTween.To(from, to, downDuration,
                ease: EasingFunctions.EaseOutCubic,
                onUpdate: v => _rt.anchoredPosition = v,
                onComplete: () => Destroy(gameObject));
        }
        else
        {
            Vector3 from = transform.localPosition;
            Vector3 to = new Vector3(from.x, _start.y, from.z);
            CustomTween.To(from, to, downDuration,
                EasingFunctions.EaseOutCubic,
                v => transform.localPosition = v,
                onComplete: () => Destroy(gameObject));
        }

        if (fadeInOut && canvasGroup != null)
        {
            CustomTween.To(canvasGroup.alpha, 0f, Mathf.Max(0.01f, fadeOutDuration),
                ease: EasingFunctions.EaseOutCubic,
                onUpdate: a => canvasGroup.alpha = a);
        }
    }
}
