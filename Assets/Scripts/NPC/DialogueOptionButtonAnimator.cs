using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deterministic visual feedback for runtime-created DialogueEditor option buttons.
/// It ignores EventSystem hover callbacks and polls the actual pointer position instead.
/// This component owns scale + RGB brightness only. DialogueEditor remains the sole owner of alpha/fades.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class DialogueOptionButtonAnimator : MonoBehaviour
{
    private Button button;
    private RectTransform rectTransform;
    private Graphic targetGraphic;
    private Canvas parentCanvas;

    private readonly Vector3 baseScale = Vector3.one;
    private Color baseRgb = Color.white;
    private float brightnessFactor = 1f;

    private float pressedScale = 0.93f;
    private float pressDuration = 0.07f;
    private float releaseDuration = 0.11f;
    private float overshoot = 0.06f;
    private float hoverScale = 1.06f;
    private float hoverDuration = 0.12f;
    private float hoverBrightness = 1.12f;

    private bool configured;
    private bool pointerWasInside;
    private bool mustExitBeforeHover;
    private bool pressed;

    private Tween scaleTween;
    private Tween brightnessTween;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        if (configured)
            ResetSpawnState();
    }

    private void OnDisable()
    {
        KillTweens();
        pressed = false;
        pointerWasInside = false;
        mustExitBeforeHover = false;
        brightnessFactor = 1f;

        if (rectTransform != null)
            rectTransform.localScale = baseScale;
        ApplyBrightnessImmediate();
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

        // One visual transition owner only.
        button.transition = Selectable.Transition.None;
        rectTransform.localScale = baseScale;

        if (targetGraphic != null)
        {
            Color c = targetGraphic.color;
            baseRgb = new Color(c.r, c.g, c.b, 1f);
        }

        configured = true;
        ResetSpawnState();
    }

    private void Update()
    {
        if (!configured || button == null || rectTransform == null || !button.interactable)
            return;

        bool inside = IsPointerInside();

        // A freshly-created option underneath a stationary cursor stays neutral until the cursor exits.
        if (mustExitBeforeHover)
        {
            if (inside)
            {
                ForceNeutral();
                pointerWasInside = true;
                return;
            }

            mustExitBeforeHover = false;
            pointerWasInside = false;
        }

        if (inside && !pointerWasInside)
            BeginHover();
        else if (!inside && pointerWasInside)
            EndHover();

        if (inside && Input.GetMouseButtonDown(0))
            BeginPress();

        if (pressed && Input.GetMouseButtonUp(0))
            EndPress(inside);

        pointerWasInside = inside;
    }

    private void ResetSpawnState()
    {
        KillTweens();
        pressed = false;
        brightnessFactor = 1f;

        if (rectTransform != null)
            rectTransform.localScale = baseScale;
        ApplyBrightnessImmediate();

        bool inside = IsPointerInside();
        pointerWasInside = inside;
        mustExitBeforeHover = inside;
    }

    private void BeginHover()
    {
        AnimateScale(baseScale * hoverScale, hoverDuration, Ease.OutBack);
        AnimateBrightness(hoverBrightness, hoverDuration);
    }

    private void EndHover()
    {
        if (!pressed)
            AnimateScale(baseScale, hoverDuration, Ease.OutQuad);
        AnimateBrightness(1f, hoverDuration);
    }

    private void BeginPress()
    {
        pressed = true;
        AnimateScale(baseScale * pressedScale, pressDuration, Ease.OutQuad);
    }

    private void EndPress(bool pointerStillInside)
    {
        pressed = false;
        Vector3 rest = pointerStillInside ? baseScale * hoverScale : baseScale;

        scaleTween?.Kill();
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        if (overshoot > 0f)
            sequence.Append(rectTransform.DOScale(rest * (1f + overshoot), releaseDuration * 0.45f).SetEase(Ease.OutQuad));
        sequence.Append(rectTransform.DOScale(rest, releaseDuration * 0.55f).SetEase(Ease.OutBack));
        scaleTween = sequence;

        AnimateBrightness(pointerStillInside ? hoverBrightness : 1f, hoverDuration);
    }

    private void ForceNeutral()
    {
        KillTweens();
        pressed = false;
        brightnessFactor = 1f;

        if (rectTransform != null)
            rectTransform.localScale = baseScale;
        ApplyBrightnessImmediate();
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

    private void AnimateBrightness(float targetFactor, float duration)
    {
        brightnessTween?.Kill();
        brightnessTween = DOTween.To(
                () => brightnessFactor,
                value =>
                {
                    brightnessFactor = value;
                    ApplyBrightnessImmediate();
                },
                targetFactor,
                duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void ApplyBrightnessImmediate()
    {
        if (targetGraphic == null)
            return;

        // Preserve live alpha exactly as DialogueEditor currently owns it during fade-in/fade-out.
        Color current = targetGraphic.color;
        targetGraphic.color = new Color(
            Mathf.Clamp01(baseRgb.r * brightnessFactor),
            Mathf.Clamp01(baseRgb.g * brightnessFactor),
            Mathf.Clamp01(baseRgb.b * brightnessFactor),
            current.a);
    }

    private void KillTweens()
    {
        scaleTween?.Kill();
        brightnessTween?.Kill();
        scaleTween = null;
        brightnessTween = null;
    }
}
