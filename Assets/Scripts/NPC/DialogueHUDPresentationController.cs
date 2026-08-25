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
    [Tooltip("If enabled, dialogue choices are arranged left-to-right under the speech box.")]
    public bool horizontalOptions = true;
    [Min(0f)] public float optionSpacing = 12f;
    public RectOffset optionPadding = new RectOffset(8, 8, 4, 4);
    public TextAnchor optionChildAlignment = TextAnchor.MiddleCenter;
    public bool controlOptionWidth = true;
    public bool controlOptionHeight = true;
    public bool expandOptionWidth = true;
    public bool expandOptionHeight = false;
    [Tooltip("Optional explicit size for Panel_Options. X/Y <= 0 keeps its existing size.")]
    public Vector2 optionPanelSize = Vector2.zero;
    [Tooltip("Optional anchored-position offset added to Panel_Options while horizontal layout is active.")]
    public Vector2 optionPanelPositionOffset = Vector2.zero;

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

    private void Awake()
    {
        if (conversationManager == null)
            conversationManager = GetComponent<ConversationManager>();

        CaptureShownPositions();
        CaptureOptionPanelTransform();
        ApplyOptionLayout();
    }

    private void Start()
    {
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
        slideOutDuration = Mathf.Max(0f, slideOutDuration);
        slideInDuration = Mathf.Max(0f, slideInDuration);

        if (!Application.isPlaying)
            return;

        CaptureOptionPanelTransform();
        ApplyOptionLayout();
    }

    private void Update()
    {
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

        RectTransform panel = conversationManager.OptionsPanel;
        CaptureOptionPanelTransform();

        VerticalLayoutGroup vertical = panel.GetComponent<VerticalLayoutGroup>();
        HorizontalLayoutGroup horizontal = panel.GetComponent<HorizontalLayoutGroup>();

        if (horizontalOptions)
        {
            if (vertical != null)
                vertical.enabled = false;

            if (horizontal == null)
                horizontal = panel.gameObject.AddComponent<HorizontalLayoutGroup>();

            horizontal.enabled = true;
            horizontal.spacing = optionSpacing;
            horizontal.padding = CopyRectOffset(optionPadding);
            horizontal.childAlignment = optionChildAlignment;
            horizontal.childControlWidth = controlOptionWidth;
            horizontal.childControlHeight = controlOptionHeight;
            horizontal.childForceExpandWidth = expandOptionWidth;
            horizontal.childForceExpandHeight = expandOptionHeight;
        }
        else
        {
            if (horizontal != null)
                horizontal.enabled = false;
            if (vertical != null)
                vertical.enabled = true;
        }

        Vector2 size = optionPanelBaseSize;
        if (optionPanelSize.x > 0f) size.x = optionPanelSize.x;
        if (optionPanelSize.y > 0f) size.y = optionPanelSize.y;
        panel.sizeDelta = size;
        panel.anchoredPosition = optionPanelBasePosition + optionPanelPositionOffset;
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

    private static RectOffset CopyRectOffset(RectOffset source)
    {
        if (source == null)
            return new RectOffset();
        return new RectOffset(source.left, source.right, source.top, source.bottom);
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
