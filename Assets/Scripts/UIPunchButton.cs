using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Small tactile press feedback for UI Buttons. Visual-only.
/// Safe to add to any Button; disabled/interactable=false buttons won't punch.
/// </summary>
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class UIPunchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Range(0.82f, 0.98f)] public float pressedScale = 0.93f;
    [Min(0.03f)] public float pressDuration = 0.07f;
    [Min(0.03f)] public float releaseDuration = 0.11f;
    [Range(0f, 0.25f)] public float overshoot = 0.06f;

    private Button button;
    private Vector3 baseScale;
    private Tween scaleTween;

    private void Awake()
    {
        button = GetComponent<Button>();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        transform.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.interactable) return;
        scaleTween?.Kill();
        scaleTween = transform.DOScale(baseScale * pressedScale, pressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button == null || !button.interactable) return;
        PunchBack();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (transform.localScale != baseScale)
            PunchBack();
    }

    private void PunchBack()
    {
        scaleTween?.Kill();
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        if (overshoot > 0f)
            seq.Append(transform.DOScale(baseScale * (1f + overshoot), releaseDuration * 0.45f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(baseScale, releaseDuration * 0.55f).SetEase(Ease.OutBack));
        scaleTween = seq;
    }
}
