using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Lively visual feedback for UI Buttons.
/// Keeps the existing tactile press punch and adds hover zoom + brightness.
/// Visual-only: does not change button gameplay/click behaviour.
/// External UI controllers may call SetBaseGraphicColor when they own a button's selected state.
/// </summary>
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class UIPunchButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Press Punch - existing behaviour")]
    [Range(0.82f, 0.98f)] public float pressedScale = 0.93f;
    [Min(0.03f)] public float pressDuration = 0.07f;
    [Min(0.03f)] public float releaseDuration = 0.11f;
    [Range(0f, 0.25f)] public float overshoot = 0.06f;

    [Header("Hover - additional feedback")]
    [Tooltip("Scale while the mouse is over this button. 1.06 = 6% larger.")]
    [Range(1f, 1.20f)] public float hoverScale = 1.06f;
    [Tooltip("How quickly the button zooms in/out on hover.")]
    [Min(0.03f)] public float hoverDuration = 0.12f;
    [Tooltip("How much brighter the button graphic becomes while hovered.")]
    [Range(1f, 1.50f)] public float hoverBrightness = 1.12f;

    [Header("Dynamic UI Safety")]
    [Tooltip("Prevents a button that appears underneath the current mouse position from instantly playing its hover animation. If the mouse is outside when it appears, hover works immediately.")]
    public bool suppressHoverUntilPointerExitAfterEnable = true;

    private Button button;
    private Graphic targetGraphic;
    private Vector3 baseScale;
    private Color baseGraphicColor = Color.white;

    private Tween scaleTween;
    private Tween colorTween;
    private bool isHovered;
    private bool isPressed;
    private bool hoverArmed;
    private bool isDialogueOption;
    private bool dialogueTransitionPrepared;

    private void Awake()
    {
        button = GetComponent<Button>();
        targetGraphic = button != null ? button.targetGraphic : null;
        baseScale = transform.localScale;

        DetectDialogueOption();
        PrepareDialogueOptionTransition();
        CaptureBaseGraphicColor();
        RefreshHoverArmedState();
    }

    private void OnEnable()
    {
        if (!isHovered && !isPressed)
            baseScale = transform.localScale;

        if (button == null)
            button = GetComponent<Button>();

        targetGraphic = button != null ? button.targetGraphic : null;

        DetectDialogueOption();
        PrepareDialogueOptionTransition();
        CaptureBaseGraphicColor();

        RefreshHoverArmedState();
        ResetVisualImmediate();
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        isHovered = false;
        isPressed = false;
        hoverArmed = false;
        transform.localScale = baseScale;
        if (targetGraphic != null)
            targetGraphic.color = baseGraphicColor;
    }

    /// <summary>
    /// Updates the non-hover color owned by an external UI state controller.
    /// </summary>
    public void SetBaseGraphicColor(Color color, bool applyImmediately = true)
    {
        if (button == null)
            button = GetComponent<Button>();

        targetGraphic = button != null ? button.targetGraphic : null;
        baseGraphicColor = color;

        if (!applyImmediately || targetGraphic == null)
            return;

        colorTween?.Kill();
        targetGraphic.color = isHovered ? Brightened(baseGraphicColor) : baseGraphicColor;
    }

    /// <summary>
    /// Forces a freshly spawned/reused button back to neutral. Hover is only blocked when the
    /// pointer is already inside this button at that moment.
    /// </summary>
    public void PrepareDynamicSpawnHover()
    {
        RefreshHoverArmedState();
        ResetVisualImmediate();
    }

    /// <summary>
    /// Backwards-compatible helper. This now uses actual pointer position instead of always
    /// forcing the next real hover to be ignored.
    /// </summary>
    public void SuppressHoverUntilPointerExit()
    {
        PrepareDynamicSpawnHover();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanAnimate())
            return;

        if (!hoverArmed)
        {
            ResetVisualImmediate();
            return;
        }

        if (targetGraphic != null && !isHovered)
            baseGraphicColor = targetGraphic.color;

        isHovered = true;
        AnimateBrightness(true);

        if (!isPressed)
            AnimateScale(GetRestScale(), hoverDuration, Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverArmed = true;
        isHovered = false;
        AnimateBrightness(false);

        if (!isPressed)
            AnimateScale(baseScale, hoverDuration, Ease.OutQuad);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate())
            return;

        hoverArmed = true;
        isPressed = true;
        scaleTween?.Kill();
        scaleTween = transform.DOScale(baseScale * pressedScale, pressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanAnimate())
            return;

        isPressed = false;
        PunchBack();
    }

    private bool CanAnimate()
    {
        return button != null && button.interactable;
    }

    private void DetectDialogueOption()
    {
        string objectName = gameObject.name;
        isDialogueOption = !string.IsNullOrEmpty(objectName) &&
            objectName.StartsWith("ConversationButton", System.StringComparison.OrdinalIgnoreCase);
    }

    private void PrepareDialogueOptionTransition()
    {
        if (!isDialogueOption || button == null || dialogueTransitionPrepared)
            return;

        // DialogueEditor's prefab already uses Button Color Tint. Running that together with
        // UIPunchButton causes the target Image to be tinted/faded while our tween is also
        // changing it. Keep one visual system only for dialogue choices.
        Color normal = button.colors.normalColor;
        button.transition = Selectable.Transition.None;

        targetGraphic = button.targetGraphic;
        if (targetGraphic != null)
            targetGraphic.color = normal;

        dialogueTransitionPrepared = true;
    }

    private void CaptureBaseGraphicColor()
    {
        if (targetGraphic != null && !isHovered)
            baseGraphicColor = targetGraphic.color;
    }

    private void RefreshHoverArmedState()
    {
        if (!suppressHoverUntilPointerExitAfterEnable)
        {
            hoverArmed = true;
            return;
        }

        // Only suppress the synthetic PointerEnter case: the control spawned directly under a
        // stationary pointer. If the pointer is outside, the very first real hover should work.
        hoverArmed = !IsPointerCurrentlyInside();
    }

    private bool IsPointerCurrentlyInside()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, eventCamera);
    }

    private Vector3 GetRestScale()
    {
        return baseScale * (isHovered ? hoverScale : 1f);
    }

    private void PunchBack()
    {
        scaleTween?.Kill();

        Vector3 rest = GetRestScale();
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        if (overshoot > 0f)
            seq.Append(transform.DOScale(rest * (1f + overshoot), releaseDuration * 0.45f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(rest, releaseDuration * 0.55f).SetEase(Ease.OutBack));
        scaleTween = seq;
    }

    private void AnimateScale(Vector3 target, float duration, Ease ease)
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(target, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void AnimateBrightness(bool bright)
    {
        if (targetGraphic == null)
            return;

        colorTween?.Kill();
        Color target = bright ? Brightened(baseGraphicColor) : baseGraphicColor;
        colorTween = targetGraphic.DOColor(target, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void ResetVisualImmediate()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        isHovered = false;
        isPressed = false;
        transform.localScale = baseScale;
        if (targetGraphic != null)
            targetGraphic.color = baseGraphicColor;
    }

    private Color Brightened(Color color)
    {
        return new Color(
            Mathf.Clamp01(color.r * hoverBrightness),
            Mathf.Clamp01(color.g * hoverBrightness),
            Mathf.Clamp01(color.b * hoverBrightness),
            color.a);
    }
}
