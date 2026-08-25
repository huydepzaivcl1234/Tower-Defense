using System;
using System.Collections;
using System.Collections.Generic;
using DialogueEditor;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialogueHUDPresentationController : MonoBehaviour
{
    [Serializable]
    public class HUDSlideTarget
    {
        public RectTransform target;
        [Tooltip("Offset from the normal anchored position while dialogue is active.")]
        public Vector2 hiddenOffset;

        [NonSerialized] public Vector2 shownPosition;
        [NonSerialized] public bool captured;
    }

    [Header("Dialogue")]
    public ConversationManager conversationManager;
    public bool hideDialogueUIWhenIdle = true;

    [Header("Option Layout")]
    [Tooltip("Arrange dialogue choices left-to-right under the speech box.")]
    public bool horizontalOptions = true;
    [Tooltip("Automatically distribute all visible choices symmetrically around the center of the panel.")]
    public bool autoDistributeOptions = true;
    [Min(0f)] public float optionSpacing = 12f;
    public RectOffset optionPadding;
    public TextAnchor optionChildAlignment = TextAnchor.MiddleCenter;
    [Tooltip("Automatically control option width.")]
    public bool controlOptionWidth = true;
    [Tooltip("Maximum width of a single option. This keeps one choice centered instead of stretching across the whole panel.")]
    [Min(1f)] public float maxSingleOptionWidth = 350f;
    [Tooltip("Minimum width used when the panel has enough room.")]
    [Min(1f)] public float minOptionWidth = 120f;
    [Tooltip("Automatically apply Option Height to visible choices.")]
    public bool controlOptionHeight = true;
    [Min(1f)] public float optionHeight = 50f;
    [Tooltip("Optional explicit size for Panel_Options. X/Y <= 0 keeps its existing size.")]
    public Vector2 optionPanelSize = Vector2.zero;
    [Tooltip("Optional anchored-position offset added to Panel_Options while horizontal layout is active.")]
    public Vector2 optionPanelPositionOffset = Vector2.zero;

    [Header("Option Hover / Punch")]
    [Tooltip("Automatically add the same UIPunchButton feedback used by the rest of the game UI.")]
    public bool useGameButtonAnimation = true;
    [Range(0.82f, 0.98f)] public float optionPressedScale = 0.93f;
    [Min(0.03f)] public float optionPressDuration = 0.07f;
    [Min(0.03f)] public float optionReleaseDuration = 0.11f;
    [Range(0f, 0.25f)] public float optionReleaseOvershoot = 0.06f;
    [Range(1f, 1.20f)] public float optionHoverScale = 1.06f;
    [Min(0.03f)] public float optionHoverDuration = 0.12f;
    [Range(1f, 1.50f)] public float optionHoverBrightness = 1.12f;

    [Header("ESC Cancel")]
    public bool allowEscapeCancel = true;
    public KeyCode cancelKey = KeyCode.Escape;
    public bool stopDialogueAudioOnCancel = true;

    [Header("HUD Slide Animation")]
    public List<HUDSlideTarget> hudTargets = new List<HUDSlideTarget>();
    [Min(0f)] public float slideOutDuration = 0.35f;
    [Min(0f)] public float slideInDuration = 0.35f;
    public AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool useUnscaledTime = true;

    private Coroutine hudRoutine;
    private Coroutine restoreRoutine;
    private bool cancelRequested;
    private Vector2 optionPanelBasePosition;
    private Vector2 optionPanelBaseSize;
    private bool optionPanelTransformCaptured;

    private void Reset()
    {
        EnsureOptionPadding();
    }

    private void Awake()
    {
        EnsureOptionPadding();

        if (conversationManager == null)
            conversationManager = GetComponent<ConversationManager>();

        CaptureShownPositions();
        CaptureOptionPanelTransform();
        ApplyOptionLayout();
    }

    private void Start()
    {
        EnsureOptionPadding();
        ApplyOptionLayout();

        if (hideDialogueUIWhenIdle && (conversationManager == null || !conversationManager.IsConversationActive))
            HideDialoguePanelsImmediately();
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += HandleConversationStarted;
        ConversationManager.OnConversationEnded += HandleConversationEnded;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= HandleConversationStarted;
        ConversationManager.OnConversationEnded -= HandleConversationEnded;
    }

    private void OnValidate()
    {
        optionSpacing = Mathf.Max(0f, optionSpacing);
        optionHeight = Mathf.Max(1f, optionHeight);
        maxSingleOptionWidth = Mathf.Max(1f, maxSingleOptionWidth);
        minOptionWidth = Mathf.Clamp(minOptionWidth, 1f, maxSingleOptionWidth);
        slideOutDuration = Mathf.Max(0f, slideOutDuration);
        slideInDuration = Mathf.Max(0f, slideInDuration);

        if (!Application.isPlaying)
            return;

        EnsureOptionPadding();
        CaptureOptionPanelTransform();
        ApplyOptionLayout();
    }

    private void Update()
    {
        if (conversationManager != null && conversationManager.IsConversationActive && horizontalOptions)
            ArrangeOptionsHorizontally();

        if (!allowEscapeCancel || conversationManager == null || !conversationManager.IsConversationActive)
            return;

        if (Input.GetKeyDown(cancelKey))
            CancelConversationImmediately();
    }

    public void CaptureShownPositions()
    {
        if (hudTargets == null) return;

        for (int i = 0; i < hudTargets.Count; i++)
        {
            HUDSlideTarget item = hudTargets[i];
            if (item == null || item.target == null) continue;
            item.shownPosition = item.target.anchoredPosition;
            item.captured = true;
        }
    }

    public void ApplyOptionLayout()
    {
        if (conversationManager == null || conversationManager.OptionsPanel == null)
            return;

        EnsureOptionPadding();

        RectTransform panel = conversationManager.OptionsPanel;
        CaptureOptionPanelTransform();

        VerticalLayoutGroup vertical = panel.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
            vertical.enabled = !horizontalOptions;

        HorizontalLayoutGroup oldHorizontal = panel.GetComponent<HorizontalLayoutGroup>();
        if (oldHorizontal != null)
            oldHorizontal.enabled = false;

        Vector2 size = optionPanelBaseSize;
        if (optionPanelSize.x > 0f) size.x = optionPanelSize.x;
        if (optionPanelSize.y > 0f) size.y = optionPanelSize.y;
        panel.sizeDelta = size;
        panel.anchoredPosition = optionPanelBasePosition + optionPanelPositionOffset;

        if (horizontalOptions)
            ArrangeOptionsHorizontally();
    }

    private void ArrangeOptionsHorizontally()
    {
        if (conversationManager == null || conversationManager.OptionsPanel == null)
            return;

        RectTransform panel = conversationManager.OptionsPanel;
        EnsureOptionPadding();

        List<RectTransform> visibleOptions = new List<RectTransform>();
        for (int i = 0; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            if (child == null || !child.gameObject.activeSelf)
                continue;

            RectTransform rect = child as RectTransform;
            if (rect != null)
                visibleOptions.Add(rect);
        }

        int count = visibleOptions.Count;
        if (count == 0)
            return;

        float left = optionPadding != null ? optionPadding.left : 0f;
        float right = optionPadding != null ? optionPadding.right : 0f;
        float top = optionPadding != null ? optionPadding.top : 0f;
        float bottom = optionPadding != null ? optionPadding.bottom : 0f;

        float panelWidth = panel.rect.width;
        float panelHeight = panel.rect.height;
        if (panelWidth <= 0f) panelWidth = panel.sizeDelta.x;
        if (panelHeight <= 0f) panelHeight = panel.sizeDelta.y;

        float usableWidth = Mathf.Max(1f, panelWidth - left - right);
        float width;

        if (!controlOptionWidth)
        {
            width = visibleOptions[0].sizeDelta.x;
        }
        else if (count == 1)
        {
            width = Mathf.Min(maxSingleOptionWidth, usableWidth);
        }
        else
        {
            float equalWidth = (usableWidth - optionSpacing * (count - 1)) / count;
            width = Mathf.Max(1f, equalWidth);

            // When there is plenty of room, keep buttons readable without making them absurdly wide,
            // then distribute the complete group symmetrically around the panel center.
            if (autoDistributeOptions)
                width = Mathf.Clamp(width, Mathf.Min(minOptionWidth, usableWidth / count), maxSingleOptionWidth);
        }

        float totalWidth = width * count + optionSpacing * (count - 1);
        if (totalWidth > usableWidth && count > 0)
        {
            width = Mathf.Max(1f, (usableWidth - optionSpacing * (count - 1)) / count);
            totalWidth = width * count + optionSpacing * (count - 1);
        }

        float startCenterX = -totalWidth * 0.5f + width * 0.5f + (left - right) * 0.5f;
        float y = GetAlignedY(panelHeight, optionHeight, top, bottom);

        for (int i = 0; i < count; i++)
        {
            RectTransform rect = visibleOptions[i];

            // Center pivot is critical: UIPunchButton now grows/shrinks equally in every direction
            // instead of appearing to zoom toward the right side.
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 size = rect.sizeDelta;
            if (controlOptionWidth)
                size.x = width;
            if (controlOptionHeight)
                size.y = optionHeight;
            rect.sizeDelta = size;

            float x = startCenterX + i * (width + optionSpacing);
            rect.anchoredPosition = new Vector2(x, y);

            ConfigureOptionAnimation(rect.gameObject);
        }
    }

    private void ConfigureOptionAnimation(GameObject optionObject)
    {
        if (!useGameButtonAnimation || optionObject == null)
            return;

        Button button = optionObject.GetComponent<Button>();
        if (button == null)
            return;

        UIPunchButton punch = optionObject.GetComponent<UIPunchButton>();
        if (punch == null)
            punch = optionObject.AddComponent<UIPunchButton>();

        punch.pressedScale = optionPressedScale;
        punch.pressDuration = optionPressDuration;
        punch.releaseDuration = optionReleaseDuration;
        punch.overshoot = optionReleaseOvershoot;
        punch.hoverScale = optionHoverScale;
        punch.hoverDuration = optionHoverDuration;
        punch.hoverBrightness = optionHoverBrightness;
    }

    private float GetAlignedY(float panelHeight, float childHeight, float top, float bottom)
    {
        switch (optionChildAlignment)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.UpperCenter:
            case TextAnchor.UpperRight:
                return panelHeight * 0.5f - top - childHeight * 0.5f;

            case TextAnchor.LowerLeft:
            case TextAnchor.LowerCenter:
            case TextAnchor.LowerRight:
                return -panelHeight * 0.5f + bottom + childHeight * 0.5f;

            default:
                return (bottom - top) * 0.5f;
        }
    }

    public void CancelConversationImmediately()
    {
        if (conversationManager == null || !conversationManager.IsConversationActive)
            return;

        cancelRequested = true;

        if (stopDialogueAudioOnCancel && conversationManager.AudioPlayer != null)
            conversationManager.AudioPlayer.Stop();

        conversationManager.EndConversation();
        HideDialoguePanelsImmediately();
        AnimateHUD(false);
    }

    private void HandleConversationStarted()
    {
        cancelRequested = false;
        CaptureMissingShownPositions();
        ApplyOptionLayout();
        AnimateHUD(true);
    }

    private void HandleConversationEnded()
    {
        if (cancelRequested)
        {
            cancelRequested = false;
            return;
        }

        if (restoreRoutine != null)
            StopCoroutine(restoreRoutine);
        restoreRoutine = StartCoroutine(RestoreHUDAfterDialogueActuallyCloses());
    }

    private IEnumerator RestoreHUDAfterDialogueActuallyCloses()
    {
        while (conversationManager != null && conversationManager.IsConversationActive)
            yield return null;

        HideDialoguePanelsImmediately();
        AnimateHUD(false);
        restoreRoutine = null;
    }

    private void AnimateHUD(bool hide)
    {
        if (hudRoutine != null)
            StopCoroutine(hudRoutine);
        hudRoutine = StartCoroutine(AnimateHUDRoutine(hide));
    }

    private IEnumerator AnimateHUDRoutine(bool hide)
    {
        float duration = hide ? slideOutDuration : slideInDuration;
        AnimationCurve curve = hide ? slideOutCurve : slideInCurve;

        int count = hudTargets != null ? hudTargets.Count : 0;
        Vector2[] from = new Vector2[count];
        Vector2[] to = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            HUDSlideTarget item = hudTargets[i];
            if (item == null || item.target == null) continue;
            if (!item.captured)
            {
                item.shownPosition = item.target.anchoredPosition;
                item.captured = true;
            }

            from[i] = item.target.anchoredPosition;
            to[i] = hide ? item.shownPosition + item.hiddenOffset : item.shownPosition;
        }

        if (duration <= 0f)
        {
            ApplyPositions(to);
            hudRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(t) : t;

            for (int i = 0; i < count; i++)
            {
                HUDSlideTarget item = hudTargets[i];
                if (item == null || item.target == null) continue;
                item.target.anchoredPosition = Vector2.LerpUnclamped(from[i], to[i], eased);
            }

            yield return null;
        }

        ApplyPositions(to);
        hudRoutine = null;
    }

    private void ApplyPositions(Vector2[] positions)
    {
        int count = Mathf.Min(positions.Length, hudTargets != null ? hudTargets.Count : 0);
        for (int i = 0; i < count; i++)
        {
            HUDSlideTarget item = hudTargets[i];
            if (item == null || item.target == null) continue;
            item.target.anchoredPosition = positions[i];
        }
    }

    private void CaptureMissingShownPositions()
    {
        if (hudTargets == null) return;
        for (int i = 0; i < hudTargets.Count; i++)
        {
            HUDSlideTarget item = hudTargets[i];
            if (item == null || item.target == null || item.captured) continue;
            item.shownPosition = item.target.anchoredPosition;
            item.captured = true;
        }
    }

    private void CaptureOptionPanelTransform()
    {
        if (optionPanelTransformCaptured || conversationManager == null || conversationManager.OptionsPanel == null)
            return;

        optionPanelBasePosition = conversationManager.OptionsPanel.anchoredPosition;
        optionPanelBaseSize = conversationManager.OptionsPanel.sizeDelta;
        optionPanelTransformCaptured = true;
    }

    private void EnsureOptionPadding()
    {
        if (optionPadding != null)
            return;

        optionPadding = new RectOffset();
        optionPadding.left = 8;
        optionPadding.right = 8;
        optionPadding.top = 4;
        optionPadding.bottom = 4;
    }

    private void HideDialoguePanelsImmediately()
    {
        if (conversationManager == null) return;
        if (conversationManager.DialoguePanel != null)
            conversationManager.DialoguePanel.gameObject.SetActive(false);
        if (conversationManager.OptionsPanel != null)
            conversationManager.OptionsPanel.gameObject.SetActive(false);
    }
}
