using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-created full-screen fade overlay for menu/game transitions.
/// Uses unscaled time so it works while the main menu has Time.timeScale = 0.
/// </summary>
public class MenuScreenFader : MonoBehaviour
{
    public static MenuScreenFader Instance { get; private set; }

    private CanvasGroup canvasGroup;
    private Image fadeImage;
    private Coroutine activeRoutine;

    public bool IsFading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildOverlayIfNeeded();
        SetAlphaImmediate(0f, false);
    }

    public static MenuScreenFader GetOrCreate()
    {
        if (Instance != null) return Instance;

        MenuScreenFader existing = UnityEngine.Object.FindFirstObjectByType<MenuScreenFader>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        GameObject go = new GameObject("MenuScreenFader");
        return go.AddComponent<MenuScreenFader>();
    }

    public void SetFadeColor(Color color)
    {
        BuildOverlayIfNeeded();
        if (fadeImage != null) fadeImage.color = color;
    }

    public void PlayTransition(float fadeOutDuration, float holdDuration, float fadeInDuration, Action onBlack, Action onComplete = null)
    {
        BuildOverlayIfNeeded();

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(TransitionRoutine(
            Mathf.Max(0f, fadeOutDuration),
            Mathf.Max(0f, holdDuration),
            Mathf.Max(0f, fadeInDuration),
            onBlack,
            onComplete));
    }

    private IEnumerator TransitionRoutine(float fadeOutDuration, float holdDuration, float fadeInDuration, Action onBlack, Action onComplete)
    {
        IsFading = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        yield return FadeTo(1f, fadeOutDuration);

        onBlack?.Invoke();

        if (holdDuration > 0f)
        {
            float holdTimer = 0f;
            while (holdTimer < holdDuration)
            {
                holdTimer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        yield return FadeTo(0f, fadeInDuration);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        IsFading = false;
        activeRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;

        if (duration <= 0.0001f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // SmoothStep keeps the start/end soft instead of linear/harsh.
            t = t * t * (3f - 2f * t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void SetAlphaImmediate(float alpha, bool blockInput)
    {
        BuildOverlayIfNeeded();
        canvasGroup.alpha = Mathf.Clamp01(alpha);
        canvasGroup.blocksRaycasts = blockInput;
        canvasGroup.interactable = blockInput;
    }

    private void BuildOverlayIfNeeded()
    {
        if (canvasGroup != null && fadeImage != null) return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Transform child = transform.Find("FadeOverlay");
        GameObject overlay;
        if (child != null)
        {
            overlay = child.gameObject;
        }
        else
        {
            overlay = new GameObject("FadeOverlay", typeof(RectTransform));
            overlay.transform.SetParent(transform, false);
        }

        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        fadeImage = overlay.GetComponent<Image>();
        if (fadeImage == null) fadeImage = overlay.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;
    }
}
