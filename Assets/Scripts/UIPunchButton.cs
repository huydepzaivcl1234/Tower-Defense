using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Lively visual feedback for UI Buttons.
/// Keeps the existing tactile press punch and adds hover zoom + brightness.
/// Visual-only: does not change button gameplay/click behaviour.
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

    private Button button;
    private Graphic targetGraphic;
    private Vector3 baseScale;
    private Color baseGraphicColor = Color.white;

    private Tween scaleTween;
    private Tween colorTween;
    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        button = GetComponent<Button>();
        targetGraphic = button != null ? button.targetGraphic : null;
        baseScale = transform.localScale;
        if (targetGraphic != null)
            baseGraphicColor = targetGraphic.color;
    }

    private void OnEnable()
    {
        // Scene/prefab scale can legitimately be something other than 1.
        // Only recapture while not in an animated state so repeated enable/disable never compounds scale.
        if (!isHovered && !isPressed)
            baseScale = transform.localScale;

        if (button == null) button = GetComponent<Button>();
        targetGraphic = button != null ? button.targetGraphic : null;
        if (targetGraphic != null)
            baseGraphicColor = targetGraphic.color;
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        isHovered = false;
        isPressed = false;
        transform.localScale = baseScale;
        if (targetGraphic != null)
            targetGraphic.color = baseGraphicColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanAnimate()) return;

        isHovered = true;
        AnimateBrightness(true);

        // Do not fight the press tween while the mouse button is currently held.
        if (!isPressed)
            AnimateScale(GetRestScale(), hoverDuration, Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        AnimateBrightness(false);

        // Mouse-out should always return smoothly to the exact original scale.
        if (!isPressed)
            AnimateScale(baseScale, hoverDuration, Ease.OutQuad);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate()) return;

        isPressed = true;
        scaleTween?.Kill();
        scaleTween = transform.DOScale(baseScale * pressedScale, pressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanAnimate()) return;

        isPressed = false;
        PunchBack();
    }

    private bool CanAnimate()
    {
        return button != null && button.interactable;
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
        if (targetGraphic == null) return;

        colorTween?.Kill();
        Color target = bright ? Brightened(baseGraphicColor) : baseGraphicColor;
        colorTween = targetGraphic.DOColor(target, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private Color Brightened(Color color)
    {
        // Keep alpha untouched; brighten RGB only and clamp to legal UI colour range.
        return new Color(
            Mathf.Clamp01(color.r * hoverBrightness),
            Mathf.Clamp01(color.g * hoverBrightness),
            Mathf.Clamp01(color.b * hoverBrightness),
            color.a);
    }
}
