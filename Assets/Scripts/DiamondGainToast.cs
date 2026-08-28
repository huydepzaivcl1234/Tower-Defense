using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Animated gameplay notification for newly granted Diamonds.</summary>
[DisallowMultipleComponent]
public class DiamondGainToast : MonoBehaviour
{
    [Header("References")]
    public RectTransform root;
    public CanvasGroup canvasGroup;
    public TMP_Text amountText;
    public Image iconImage;

    [Header("Presentation")]
    public Sprite diamondIcon;
    public string prefix = "+";
    public string suffix = "";
    public bool useCompactNumbers = true;
    public bool hideIconWhenNoSprite = true;

    [Header("Motion")]
    public Vector2 hiddenOffset = new Vector2(0f, 65f);
    [Min(0f)] public float enterDuration = 0.28f;
    [Min(0f)] public float holdDuration = 1.1f;
    [Min(0f)] public float exitDuration = 0.25f;
    public Ease enterEase = Ease.OutCubic;
    public Ease exitEase = Ease.InCubic;
    public bool useUnscaledTime = true;

    [Header("Stacking")]
    [Tooltip("When another Diamond gain happens while visible, add it to the current toast instead of restarting with only the newest amount.")]
    public bool combineWhileVisible = true;

    private Vector2 shownPosition;
    private Sequence sequence;
    private int pendingAmount;
    private bool initialized;

    private void Awake()
    {
        if (root == null) root = transform as RectTransform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        CaptureShownPosition();
        HideInstant();
    }

    private void OnEnable()
    {
        PlayerProfileManager.OnDiamondsGranted += HandleGranted;
    }

    private void OnDisable()
    {
        PlayerProfileManager.OnDiamondsGranted -= HandleGranted;
        sequence?.Kill();
        sequence = null;
    }

    private void HandleGranted(int amount, int total)
    {
        if (amount <= 0) return;
        if (!IsGameplayVisible()) return;
        ShowAmount(amount);
    }

    private static bool IsGameplayVisible()
    {
        if (MainMenuController.Instance == null) return true;
        return MainMenuController.Instance.GameplayStarted && !MainMenuController.Instance.IsAnyMenuVisible;
    }

    public void ShowAmount(int amount)
    {
        if (amount <= 0) return;
        CaptureShownPosition();

        bool alreadyVisible = sequence != null && sequence.IsActive();
        pendingAmount = combineWhileVisible && alreadyVisible ? pendingAmount + amount : amount;
        RefreshText();

        if (iconImage != null)
        {
            iconImage.sprite = diamondIcon;
            iconImage.preserveAspect = true;
            iconImage.enabled = diamondIcon != null || !hideIconWhenNoSprite;
        }

        sequence?.Kill();
        sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);

        if (root != null)
        {
            if (!alreadyVisible)
                root.anchoredPosition = shownPosition + hiddenOffset;
            sequence.Join(root.DOAnchorPos(shownPosition, Mathf.Max(0f, enterDuration)).SetEase(enterEase));
        }

        if (canvasGroup != null)
        {
            if (!alreadyVisible) canvasGroup.alpha = 0f;
            sequence.Join(canvasGroup.DOFade(1f, Mathf.Max(0f, enterDuration)));
        }

        if (holdDuration > 0f) sequence.AppendInterval(holdDuration);

        if (root != null)
            sequence.Join(root.DOAnchorPos(shownPosition + hiddenOffset, Mathf.Max(0f, exitDuration)).SetEase(exitEase));
        if (canvasGroup != null)
            sequence.Join(canvasGroup.DOFade(0f, Mathf.Max(0f, exitDuration)).SetEase(exitEase));

        sequence.OnComplete(() =>
        {
            pendingAmount = 0;
            sequence = null;
        });
    }

    private void RefreshText()
    {
        if (amountText == null) return;
        string value = useCompactNumbers ? CompactNumber.Format(pendingAmount) : pendingAmount.ToString("N0");
        amountText.text = prefix + value + suffix;
    }

    private void CaptureShownPosition()
    {
        if (initialized || root == null) return;
        shownPosition = root.anchoredPosition;
        initialized = true;
    }

    public void HideInstant()
    {
        sequence?.Kill();
        sequence = null;
        pendingAmount = 0;
        if (root != null) root.anchoredPosition = shownPosition + hiddenOffset;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (root == null) root = transform as RectTransform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (iconImage != null)
        {
            iconImage.sprite = diamondIcon;
            iconImage.preserveAspect = true;
            iconImage.enabled = diamondIcon != null || !hideIconWhenNoSprite;
        }
    }
#endif
}
