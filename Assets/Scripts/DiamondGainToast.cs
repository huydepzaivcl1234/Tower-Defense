using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gameplay-only Diamond notification. Slides in, counts from the previous total to the new total,
/// optionally shows the amount gained, then slides back out.
/// Keeps Icon / Total / Gain in separate layout regions so long values cannot overlap each other.
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
    [Tooltip("Optional override Sprite. If empty, a Sprite assigned directly to Icon Image is preserved and used.")]
    public Sprite diamondIcon;
    public bool hideIconWhenNoSprite = true;
    public bool useCompactNumbers = true;
    public string totalPrefix = "";
    public string totalSuffix = "";
    public bool showGainText = true;
    public string gainPrefix = "+";
    public string gainSuffix = "";

    [Header("Safe Auto Layout")]
    [Tooltip("Keeps Icon, Total and +Gain in separate horizontal regions so they never overlap. Disable only if you want to position every child manually.")]
    public bool autoLayoutChildren = true;
    [Tooltip("Resize the toast width to fit its current text content.")]
    public bool autoFitRootWidth = true;
    [Min(0f)] public float horizontalPadding = 14f;
    [Min(1f)] public float iconSize = 34f;
    [Min(0f)] public float iconToTotalGap = 10f;
    [Min(0f)] public float totalToGainGap = 12f;
    [Min(0f)] public float textHorizontalPadding = 8f;
    [Min(1f)] public float minimumTotalWidth = 72f;
    [Min(1f)] public float minimumGainWidth = 54f;
    [Min(1f)] public float minimumRootWidth = 220f;
    [Min(1f)] public float maximumRootWidth = 520f;
    [Tooltip("0 keeps the current root height. Any value above 0 forces this height while Auto Layout is enabled.")]
    [Min(0f)] public float rootHeight = 62f;

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
        RefreshIcon();
        RefreshGainText();
        RefreshTotalText(displayedTotal);
        ApplySafeLayout();
        HideInstant();
    }

    private void OnEnable()
    {
        PlayerProfileManager.OnDiamondsGranted += HandleGranted;
        RefreshIcon();
        ApplySafeLayout();
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
        ApplySafeLayout();

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
            RefreshGainText();
            ApplySafeLayout();
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
            ApplySafeLayout();
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
                    ApplySafeLayout();
                },
                targetTotal,
                countDuration)
            .SetEase(countEase)
            .SetUpdate(useUnscaledTime)
            .OnComplete(() =>
            {
                displayedTotal = targetTotal;
                RefreshTotalText(displayedTotal);
                ApplySafeLayout();
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

        bool shouldShow = showGainText && accumulatedGain > 0;
        gainText.gameObject.SetActive(shouldShow);
        if (!shouldShow)
            return;

        string formatted = useCompactNumbers ? CompactNumber.Format(accumulatedGain) : accumulatedGain.ToString("N0");
        gainText.text = gainPrefix + formatted + gainSuffix;
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        Sprite resolved = diamondIcon != null ? diamondIcon : iconImage.sprite;
        if (diamondIcon != null && iconImage.sprite != diamondIcon)
            iconImage.sprite = diamondIcon;

        iconImage.preserveAspect = true;
        iconImage.enabled = resolved != null || !hideIconWhenNoSprite;
    }

    /// <summary>
    /// Optional designer-friendly layout guard. This only touches this toast's own children and
    /// never changes anchors/position of the toast itself. Disable Auto Layout to author manually.
    /// </summary>
    public void ApplySafeLayout()
    {
        if (!autoLayoutChildren || root == null)
            return;

        float padding = Mathf.Max(0f, horizontalPadding);
        float cursor = padding;
        bool hasIcon = iconImage != null && iconImage.enabled;
        bool hasGain = gainText != null && gainText.gameObject.activeSelf;

        if (hasIcon)
        {
            RectTransform ir = iconImage.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(Mathf.Max(1f, iconSize), Mathf.Max(1f, iconSize));
            ir.anchoredPosition = new Vector2(cursor + ir.sizeDelta.x * 0.5f, 0f);
            cursor += ir.sizeDelta.x + Mathf.Max(0f, iconToTotalGap);
        }

        float totalPreferred = totalText != null
            ? totalText.GetPreferredValues(totalText.text).x + Mathf.Max(0f, textHorizontalPadding) * 2f
            : 0f;
        float totalWidth = Mathf.Max(Mathf.Max(1f, minimumTotalWidth), totalPreferred);

        float gainWidth = 0f;
        if (hasGain)
        {
            float gainPreferred = gainText.GetPreferredValues(gainText.text).x + Mathf.Max(0f, textHorizontalPadding) * 2f;
            gainWidth = Mathf.Max(Mathf.Max(1f, minimumGainWidth), gainPreferred);
        }

        float desiredWidth = cursor + totalWidth + padding;
        if (hasGain)
            desiredWidth += Mathf.Max(0f, totalToGainGap) + gainWidth;

        if (autoFitRootWidth)
        {
            float minWidth = Mathf.Max(1f, minimumRootWidth);
            float maxWidth = Mathf.Max(minWidth, maximumRootWidth);
            Vector2 size = root.sizeDelta;
            size.x = Mathf.Clamp(desiredWidth, minWidth, maxWidth);
            if (rootHeight > 0f)
                size.y = rootHeight;
            root.sizeDelta = size;
        }
        else if (rootHeight > 0f)
        {
            Vector2 size = root.sizeDelta;
            size.y = rootHeight;
            root.sizeDelta = size;
        }

        float availableRight = root.rect.width - padding;

        if (hasGain)
        {
            float gainLeft = availableRight - gainWidth;
            RectTransform gr = gainText.rectTransform;
            gr.anchorMin = gr.anchorMax = new Vector2(0f, 0.5f);
            gr.pivot = new Vector2(0f, 0.5f);
            gr.sizeDelta = new Vector2(gainWidth, Mathf.Max(1f, root.rect.height));
            gr.anchoredPosition = new Vector2(gainLeft, 0f);
            gainText.alignment = TextAlignmentOptions.MidlineRight;

            totalWidth = Mathf.Max(1f, gainLeft - Mathf.Max(0f, totalToGainGap) - cursor);
        }
        else
        {
            totalWidth = Mathf.Max(1f, availableRight - cursor);
        }

        if (totalText != null)
        {
            RectTransform tr = totalText.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0f, 0.5f);
            tr.pivot = new Vector2(0f, 0.5f);
            tr.sizeDelta = new Vector2(totalWidth, Mathf.Max(1f, root.rect.height));
            tr.anchoredPosition = new Vector2(cursor, 0f);
            totalText.alignment = TextAlignmentOptions.MidlineLeft;
            totalText.enableWordWrapping = false;
            totalText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (gainText != null)
        {
            gainText.enableWordWrapping = false;
            gainText.overflowMode = TextOverflowModes.Ellipsis;
        }
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
        RefreshGainText();
        ApplySafeLayout();
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
        RefreshGainText();
        ApplySafeLayout();
    }
#endif
}
