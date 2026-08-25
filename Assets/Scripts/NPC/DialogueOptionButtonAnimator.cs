using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Deterministic visual feedback for runtime-created DialogueEditor option buttons.
/// This component intentionally ignores EventSystem hover callbacks and polls the actual pointer
/// position instead. It owns scale + RGB only; DialogueEditor remains the sole owner of alpha/fades.
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
            ResetSpawnState();
    }

    private void OnDisable()
    {
        KillTweens();
        hovered = false;
        pressed = false;
        pointerWasInside = false;
        mustExitBeforeHover = false;

        if (rectTransform != null)
            rectTransform.localScale = baseScale;
        ApplyRgbImmediate(baseRgb);
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

        // Exactly one transition system owns the option visuals.
        button.transition = Selectable.Transition.None;

        rectTransform.localScale = baseScale;

        if (targetGraphic != null)
        {
            Color source = targetGraphic.color;
            baseRgb = new Color(source.r, source.g, source.b, 1f);
        }

        configured = true;
        ResetSpawnState();
    }

    private void Update()
    {
        if (!configured || button == null || rectTransform == null || !button.interactable)
            return;

        bool inside = IsPointerInside();

        // Spawned under the cursor: remain strictly neutral until a real exit occurs.
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
        hovered = false;
        pressed = false;
        if (rectTransform != null)
            rectTransform.localScale = baseScale;
        ApplyRgbImmediate(baseRgb);

        bool inside = IsPointerInside();
        pointerWasInside = inside;
        mustExitBeforeHover = inside;
    }

    private void BeginHover()
    {
        hovered = true;
        AnimateScale(baseScale * hoverScale, hoverDuration, Ease.OutBack);
        AnimateRgb(Brightened(baseRgb), hoverDuration);
    }

    private void EndHover()
    {
        hovered = false;
        if (!pressed)
            AnimateScale(baseScale, hoverDuration, Ease.OutQuad);
        AnimateRgb(baseRgb, hoverDuration);
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

        AnimateRgb(pointerStillInside ? Brightened(baseRgb) : baseRgb, hoverDuration);
    }

    private void ForceNeutral()
    {
        KillTweens();
        hovered = false;
        pressed = false;
        if (rectTransform != null)
            rectTransform.localScale = baseScale;
        ApplyRgbImmediate(baseRgb);
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

    private void AnimateRgb(Color rgb, float duration)
    {
        if (targetGraphic == null)
            return;

        colorTween?.Kill();
        Color current = targetGraphic.color;
        Color target = new Color(rgb.r, rgb.g, rgb.b, current.a);
        colorTween = targetGraphic.DOColor(target, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnUpdate(PreserveCurrentAlpha);
    }

    private void ApplyRgbImmediate(Color rgb)
    {
        if (targetGraphic == null)
            return;

        Color current = targetGraphic.color;
        targetGraphic.color = new Color(rgb.r, rgb.g, rgb.b, current.a);
    }

    private void PreserveCurrentAlpha()
    {
        // DialogueEditor's SetAlpha() owns alpha during fade transitions. DOTween may have captured
        // an older alpha when the tween started, so overwrite only RGB while preserving live alpha.
        if (targetGraphic == null)
            return;

        Color current = targetGraphic.color;
        // Nothing else required here: Apply/target methods always preserve current alpha. This hook
        // keeps the tween updated in unscaled time without introducing an alpha owner.
        targetGraphic.color = current;
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
            1f);
    }
}
