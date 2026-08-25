using System;
using System.Collections;
using System.Collections.Generic;
using DialogueEditor;
using UnityEngine;

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

    private void Awake()
    {
        if (conversationManager == null)
            conversationManager = GetComponent<ConversationManager>();

        CaptureShownPositions();
    }

    private void Start()
    {
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

    public void CancelConversationImmediately()
    {
        if (conversationManager == null || !conversationManager.IsConversationActive)
            return;

        cancelRequested = true;

        if (stopDialogueAudioOnCancel && conversationManager.AudioPlayer != null)
            conversationManager.AudioPlayer.Stop();

        // EndConversation fires the package's normal end event. We then hide the panels in the
        // same frame so ESC feels instant even though the package normally fades for 0.2 seconds.
        conversationManager.EndConversation();
        HideDialoguePanelsImmediately();
        AnimateHUD(false);
    }

    private void HandleConversationStarted()
    {
        cancelRequested = false;
        CaptureMissingShownPositions();
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

    private void HideDialoguePanelsImmediately()
    {
        if (conversationManager == null) return;
        if (conversationManager.DialoguePanel != null)
            conversationManager.DialoguePanel.gameObject.SetActive(false);
        if (conversationManager.OptionsPanel != null)
            conversationManager.OptionsPanel.gameObject.SetActive(false);
    }
}
