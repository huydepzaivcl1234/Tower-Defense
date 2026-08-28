using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gameplay-only Diamond notification. Slides in, counts from the previous total to the new total,
/// optionally shows the amount gained, then slides back out.
/// </summary>
[DisallowMultipleComponent]
public class DiamondGainToast : MonoBehaviour
{
    [Header("References")]
    public RectTransform root;
    public CanvasGroup canvasGroup;
    [Tooltip("Main counter. This animates from the previous total Diamonds to the new total.")]
    public TMP_Text totalText;
    [Tooltip("Optional secondary text such as +3. Can be left empty.")]
    public TMP_Text gainText;
    public Image iconImage;

    [Header("Icon / Text")]
    [Tooltip("Your own Diamond Sprite. Nothing is hard-coded.")]
    public Sprite diamondIcon;
    public bool hideIconWhenNoSprite = true;
    public bool useCompactNumbers = true;
    public string totalPrefix = "";
    public string totalSuffix = "";
    public bool showGainText = true;
    public string gainPrefix = "+";
    public string gainSuffix = "";

    [Header("Count Animation")]
    [Min(0f)] public float countDelayAfterEnter = 0.05f;
    [Min(0f)] public float countDuration = 0.45f;
    public Ease countEase = Ease.OutCubic;

    [Header("Slide Motion")]
    [Tooltip("Where the HUD waits while hidden, relative to its shown position. Positive Y makes it enter downward from above.")]
    public Vector2 hiddenOffset = new Vector2(0f, 70f);
    [Min(0f)] public float enterDuration = 0.28f;
    [Min(0f)] public float holdDuration = 1.0f;
    [Min(0f)] public float exitDuration = 0.25f;
    public Ease enterEase = Ease.OutCubic;
    public Ease exitEase = Ease.InCubic;
    public bool useUnscaledTime = true;

    [Header("Repeated Gains")]
    [Tooltip("If another Diamond is collected while this HUD is visible, combine it into the current notification and count toward the newest total.")]
    public bool combineWhileVisible = true;

    private Vector2 shownPosition;
    private Sequence sequence;
    private Tween countTween;
    private bool initialized;
    private int accumulatedGain;
    private int displayedTotal;
    private int targetTotal;

    private void Awake()
    {
        if (root == null) root = transform as RectTransform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        CaptureShownPosition();
        displayedTotal = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.CurrentDiamonds : 0;
        targetTotal = displayedTotal;
        HideInstant();
    }

    private void OnEnable()
    {
        PlayerProfileManager.OnDiamondsGranted += HandleGranted;
    }

    private void OnDisable()
    {
        PlayerProfileManager.OnDiamondsGranted -= HandleGranted;
        KillAnimation();
    }

    private void HandleGranted(int amount, int total)
    {
        if (amount <= 0 || !IsGameplayVisible())
            return;

        ShowGain(amount, total);
    }

    private static bool IsGameplayVisible()
    {
        if (MainMenuController.Instance == null)
            return true;

        return MainMenuController.Instance.GameplayStarted && !MainMenuController.Instance.IsAnyMenuVisible;
    }

    public void ShowGain(int amount, int newTotal)
    {
        if (amount <= 0)
            return;

        CaptureShownPosition();
        bool alreadyVisible = sequence != null && sequence.IsActive();

        int previousTotal = Mathf.Max(0, newTotal - amount);
        if (!alreadyVisible || !combineWhileVisible)
        {
            accumulatedGain = amount;
            displayedTotal = previousTotal;
        }
        else
        {
            accumulatedGain += amount;
            displayedTotal = Mathf.Clamp(displayedTotal, 0, newTotal);
        }

        targetTotal = Mathf.Max(0, newTotal);
        RefreshGainText();
        RefreshTotalText(displayedTotal);
        RefreshIcon();

        KillAnimation(false);
        sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);

        if (root != null)
        {
            if (!alreadyVisible)
                root.anchoredPosition = shownPosition + hiddenOffset;
            sequence.Append(root.DOAnchorPos(shownPosition, Mathf.Max(0f, enterDuration)).SetEase(enterEase));
        }

        if (canvasGroup != null)
        {
            if (!alreadyVisible)
                canvasGroup.alpha = 0f;
            if (root != null)
                sequence.Join(canvasGroup.DOFade(1f, Mathf.Max(0f, enterDuration)));
            else
                sequence.Append(canvasGroup.DOFade(1f, Mathf.Max(0f, enterDuration)));
        }

        if (countDelayAfterEnter > 0f)
            sequence.AppendInterval(countDelayAfterEnter);

        sequence.AppendCallback(StartCountTween);
        if (countDuration > 0f)
            sequence.AppendInterval(countDuration);

        if (holdDuration > 0f)
            sequence.AppendInterval(holdDuration);

        if (root != null)
        {
            sequence.Append(root.DOAnchorPos(shownPosition + hiddenOffset, Mathf.Max(0f, exitDuration)).SetEase(exitEase));
            if (canvasGroup != null)
                sequence.Join(canvasGroup.DOFade(0f, Mathf.Max(0f, exitDuration)).SetEase(exitEase));
        }
        else if (canvasGroup != null)
        {
            sequence.Append(canvasGroup.DOFade(0f, Mathf.Max(0f, exitDuration)).SetEase(exitEase));
        }

        sequence.OnComplete(() =>
        {
            displayedTotal = targetTotal;
            RefreshTotalText(displayedTotal);
            accumulatedGain = 0;
            sequence = null;
        });
    }

    private void StartCountTween()
    {
        countTween?.Kill();

        if (countDuration <= 0f || displayedTotal == targetTotal)
        {
            displayedTotal = targetTotal;
            RefreshTotalText(displayedTotal);
            return;
        }

        float value = displayedTotal;
        countTween = DOTween.To(
                () => value,
                x =>
                {
                    value = x;
                    displayedTotal = Mathf.RoundToInt(x);
                    RefreshTotalText(displayedTotal);
                },
                targetTotal,
                countDuration)
            .SetEase(countEase)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() =>
            {
                displayedTotal = targetTotal;
                RefreshTotalText(displayedTotal);
                countTween = null;
            });
    }

    private void RefreshTotalText(int value)
    {
        if (totalText == null)
            return;

        string formatted = useCompactNumbers ? CompactNumber.Format(value) : value.ToString("N0");
        totalText.text = totalPrefix + formatted + totalSuffix;
    }

    private void RefreshGainText()
    {
        if (gainText == null)
            return;

        gainText.gameObject.SetActive(showGainText);
        if (!showGainText)
            return;

        string formatted = useCompactNumbers ? CompactNumber.Format(accumulatedGain) : accumulatedGain.ToString("N0");
        gainText.text = gainPrefix + formatted + gainSuffix;
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        iconImage.sprite = diamondIcon;
        iconImage.preserveAspect = true;
        iconImage.enabled = diamondIcon != null || !hideIconWhenNoSprite;
    }

    private void CaptureShownPosition()
    {
        if (initialized || root == null)
            return;

        shownPosition = root.anchoredPosition;
        initialized = true;
    }

    private void KillAnimation(bool killSequence = true)
    {
        countTween?.Kill();
        countTween = null;

        if (killSequence)
        {
            sequence?.Kill();
            sequence = null;
        }
        else if (sequence != null)
        {
            sequence.Kill();
            sequence = null;
        }
    }

    public void HideInstant()
    {
        KillAnimation();
        accumulatedGain = 0;
        if (root != null)
            root.anchoredPosition = shownPosition + hiddenOffset;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (root == null) root = transform as RectTransform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        RefreshIcon();
        if (gainText != null)
            gainText.gameObject.SetActive(showGainText);
    }
#endif
}
