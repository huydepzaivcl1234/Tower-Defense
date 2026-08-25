using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deterministic hover/click feedback for runtime-created DialogueEditor option buttons.
/// It intentionally does NOT use IPointerEnter/IPointerExit because dynamically spawned UI can
/// receive synthetic pointer-enter events on the same frame it appears. Instead, it polls the
/// actual mouse position against the RectTransform and only transitions on a real outside->inside
/// state change.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class DialogueOptionButtonAnimator : MonoBehaviour
{
    private Button button;
    private RectTransform rectTransform;
    private Graphic targetGraphic;
    private Canvas parentCanvas;

    private Vector3 baseScale = Vector3.one;
    private Color baseColor = Color.white;

    private float pressedScale = 0.93f;
    private float pressDuration = 0.07f;
    private float releaseDuration = 0.11f;
    private float overshoot = 0.06f;
    private float hoverScale = 1.06f;
    private float hoverDuration = 0.12f;
    private float hoverBrightness = 1.12f;

    private bool configured;
    private bool wasInside;
    private bool requireExitBeforeHover;
    private bool hovered;
    private bool pressed;

    private Tween scaleTween;
    private Tween colorTween;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        if (configured)
            ResetPointerState();
    }

    private void OnDisable()
    {
        KillTweens();
        hovered = false;
        pressed = false;
        wasInside = false;
        requireExitBeforeHover = false;

        if (rectTransform != null)
            rectTransform.localScale = baseScale;
        if (targetGraphic != null)
            targetGraphic.color = baseColor;
    }

    public void Configure(
        float newPressedScale,
        float newPressDuration,
        float newReleaseDuration,
        float newOvershoot,
        float newHoverScale,
        float newHoverDuration,
        float newHoverBrightness)
    {
        CacheReferences();
        if (button == null || rectTransform == null)
            return;

        pressedScale = newPressedScale;
        pressDuration = Mathf.Max(0.01f, newPressDuration);
        releaseDuration = Mathf.Max(0.01f, newReleaseDuration);
        overshoot = Mathf.Max(0f, newOvershoot);
        hoverScale = Mathf.Max(1f, newHoverScale);
        hoverDuration = Mathf.Max(0.01f, newHoverDuration);
        hoverBrightness = Mathf.Max(1f, newHoverBrightness);

        // Dialogue options use this animator as the only visual transition system.
        // Disabling Selectable's ColorTint prevents it from fighting our color tween.
        if (button.transition != Selectable.Transition.None)
            button.transition = Selectable.Transition.None;

        baseScale = Vector3.one;
        rectTransform.localScale = baseScale;

        if (targetGraphic != null)
        {
            // Force the visual back to the Button's normal state before taking the base color.
            // This removes any transient highlighted color the prefab may have received on spawn.
            Color normal = button.colors.normalColor;
            normal.a = targetGraphic.color.a;
            targetGraphic.color = normal;
            baseColor = normal;
        }

        configured = true;
        ResetPointerState();
    }

    private void Update()
    {
        if (!configured || button == null || rectTransform == null || !button.interactable)
            return;

        bool inside = IsPointerInside();

        // If the option appeared underneath a stationary cursor, it MUST remain neutral until
        // that cursor genuinely leaves the rect. This makes Spawn -> Hover impossible.
        if (requireExitBeforeHover)
        {
            if (!inside)
            {
                requireExitBeforeHover = false;
                wasInside = false;
            }
            else
            {
                ForceNeutral();
                wasInside = true;
                return;
            }
        }

        if (inside && !wasInside)
            BeginHover();
        else if (!inside && wasInside)
            EndHover();

        if (inside && Input.GetMouseButtonDown(0))
            BeginPress();

        if (pressed && Input.GetMouseButtonUp(0))
            EndPress(inside);

        wasInside = inside;
    }

    private void ResetPointerState()
    {
        KillTweens();
        hovered = false;
        pressed = false;

        if (rectTransform != null)
            rectTransform.localScale = baseScale;
        if (targetGraphic != null)
            targetGraphic.color = baseColor;

        bool inside = IsPointerInside();
        wasInside = inside;
        requireExitBeforeHover = inside;
    }

    private void BeginHover()
    {
        hovered = true;
        AnimateScale(baseScale * hoverScale, hoverDuration, Ease.OutBack);
        AnimateColor(Brightened(baseColor), hoverDuration);
    }

    private void EndHover()
    {
        hovered = false;
        if (!pressed)
            AnimateScale(baseScale, hoverDuration, Ease.OutQuad);
        AnimateColor(baseColor, hoverDuration);
    }

    private void BeginPress()
    {
        pressed = true;
        AnimateScale(baseScale * pressedScale, pressDuration, Ease.OutQuad);
    }

    private void EndPress(bool pointerStillInside)
    {
        pressed = false;
        hovered = pointerStillInside;

        Vector3 rest = pointerStillInside ? baseScale * hoverScale : baseScale;
        scaleTween?.Kill();

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        if (overshoot > 0f)
            sequence.Append(rectTransform.DOScale(rest * (1f + overshoot), releaseDuration * 0.45f).SetEase(Ease.OutQuad));
        sequence.Append(rectTransform.DOScale(rest, releaseDuration * 0.55f).SetEase(Ease.OutBack));
        scaleTween = sequence;

        AnimateColor(pointerStillInside ? Brightened(baseColor) : baseColor, hoverDuration);
    }

    private void ForceNeutral()
    {
        if (hovered || pressed || rectTransform.localScale != baseScale)
        {
            KillTweens();
            hovered = false;
            pressed = false;
            rectTransform.localScale = baseScale;
            if (targetGraphic != null)
                targetGraphic.color = baseColor;
        }
    }

    private bool IsPointerInside()
    {
        if (rectTransform == null)
            return false;

        Camera eventCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = parentCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, eventCamera);
    }

    private void CacheReferences()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (rectTransform == null)
            rectTransform = transform as RectTransform;
        if (targetGraphic == null && button != null)
            targetGraphic = button.targetGraphic;
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
    }

    private void AnimateScale(Vector3 target, float duration, Ease ease)
    {
        if (rectTransform == null)
            return;

        scaleTween?.Kill();
        scaleTween = rectTransform.DOScale(target, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void AnimateColor(Color target, float duration)
    {
        if (targetGraphic == null)
            return;

        colorTween?.Kill();
        colorTween = targetGraphic.DOColor(target, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void KillTweens()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        scaleTween = null;
        colorTween = null;
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
