using System.Collections;
using DialogueEditor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bridges a world NPC to the imported DialogueEditor package.
/// Clicking this NPC (including any child collider) starts its NPCConversation.
/// While DialogueEditor's typewriter reveals characters, configurable voice blips
/// are played in an Undertale-style rhythm.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NPCConversation))]
public class NPCDialogueInteractable : MonoBehaviour
{
    [Header("Dialogue")]
    public NPCConversation conversation;
    [Tooltip("Ignore clicks while another conversation is already active.")]
    public bool blockIfConversationActive = true;
    [Tooltip("Only allow talking after gameplay has started.")]
    public bool requireGameplayStarted = true;
    [Tooltip("Ignore world clicks while the pointer is over UI.")]
    public bool ignoreClicksOverUI = true;

    [Header("Click / Raycast")]
    [Range(0, 2)] public int mouseButton = 0;
    [Tooltip("Camera used for clicking. If empty, Camera.main is used.")]
    public Camera interactionCamera;
    [Min(0.1f)] public float maxClickDistance = 1000f;
    public LayerMask clickLayerMask = ~0;
    [Tooltip("Allow colliders on children of this NPC/model to count as clicking the NPC.")]
    public bool includeChildColliders = true;
    [Tooltip("Do not try to start a conversation while the cursor is currently locked.")]
    public bool requireUnlockedCursor = true;

    [Header("Dialogue Camera Focus")]
    public bool enableCameraFocus = true;
    [Tooltip("Camera animated during dialogue. If empty, Interaction Camera then Camera.main is used.")]
    public Camera dialogueCamera;
    [Tooltip("Reference transform for the focus offsets. Leave empty to use this NPC root. You can assign a Head/Face transform for precise framing.")]
    public Transform cameraFocusReference;
    [Tooltip("Local point the camera looks at. With NPC root as reference, Y is usually head height.")]
    public Vector3 focusPointLocalOffset = new Vector3(0f, 1.6f, 0f);
    [Tooltip("Local camera position relative to Camera Focus Reference. Positive Z normally places the camera in front of a correctly oriented NPC.")]
    public Vector3 cameraLocalOffset = new Vector3(0f, 1.6f, 2.7f);
    [Min(0f)] public float focusDuration = 0.55f;
    [Min(0f)] public float restoreDuration = 0.45f;
    public AnimationCurve focusCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve restoreCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool animateFieldOfView = true;
    [Range(10f, 100f)] public float focusedFieldOfView = 42f;
    public bool useUnscaledCameraTime = true;

    [Header("Undertale-style Voice Blip")]
    public bool enableVoiceBlip = true;
    [Tooltip("One or more short voice/blip clips. A random clip is chosen each time.")]
    public AudioClip[] voiceClips;
    [Range(0f, 1f)] public float voiceVolume = 0.55f;
    [Range(0.1f, 3f)] public float minPitch = 0.92f;
    [Range(0.1f, 3f)] public float maxPitch = 1.08f;
    [Min(1)] public int playEveryNCharacters = 2;
    public bool skipWhitespace = true;
    public bool skipPunctuation = true;

    [Header("Audio Source")]
    [Tooltip("Created automatically if empty.")]
    public AudioSource voiceSource;
    [Range(0f, 1f)] public float spatialBlend = 0f;
    public bool bypassReverbZones = true;

    public bool OwnsActiveConversation => ownsConversation &&
        ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive;

    private bool ownsConversation;
    private string observedText = string.Empty;
    private int lastVisibleCharacters;
    private int eligibleCharacterCounter;

    private Coroutine cameraRoutine;
    private Camera activeDialogueCamera;
    private bool cameraStateCaptured;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private float savedCameraFov;

    private void Reset()
    {
        conversation = GetComponent<NPCConversation>();
        interactionCamera = Camera.main;
        dialogueCamera = Camera.main;
        EnsureAudioSource();
    }

    private void Awake()
    {
        if (conversation == null)
            conversation = GetComponent<NPCConversation>();

        if (interactionCamera == null)
            interactionCamera = Camera.main;

        if (dialogueCamera == null)
            dialogueCamera = interactionCamera != null ? interactionCamera : Camera.main;

        EnsureAudioSource();
        ApplyAudioSettings();
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationEnded += HandleConversationEnded;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationEnded -= HandleConversationEnded;
        ownsConversation = false;
        RestoreCameraImmediately();
    }

    private void OnValidate()
    {
        mouseButton = Mathf.Clamp(mouseButton, 0, 2);
        maxClickDistance = Mathf.Max(0.1f, maxClickDistance);
        playEveryNCharacters = Mathf.Max(1, playEveryNCharacters);
        maxPitch = Mathf.Max(minPitch, maxPitch);
        focusDuration = Mathf.Max(0f, focusDuration);
        restoreDuration = Mathf.Max(0f, restoreDuration);

        if (conversation == null)
            conversation = GetComponent<NPCConversation>();

        if (voiceSource != null)
            ApplyAudioSettings();
    }

    private void Update()
    {
        HandleWorldClick();
        UpdateVoiceBlips();
    }

    private void HandleWorldClick()
    {
        if (!Input.GetMouseButtonDown(mouseButton))
            return;

        if (requireUnlockedCursor && Cursor.lockState == CursorLockMode.Locked)
            return;

        if (ignoreClicksOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Camera cam = interactionCamera != null ? interactionCamera : Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, clickLayerMask, QueryTriggerInteraction.Collide))
            return;

        Transform hitTransform = hit.collider != null ? hit.collider.transform : null;
        if (hitTransform == null)
            return;

        bool hitThisNpc = hitTransform == transform;
        if (!hitThisNpc && includeChildColliders)
            hitThisNpc = hitTransform.IsChildOf(transform);

        if (!hitThisNpc)
            return;

        TryStartConversation();
    }

    public void TryStartConversation()
    {
        if (!CanStartConversation())
            return;

        ConversationManager manager = ConversationManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[NPCDialogue] No DialogueEditor ConversationManager exists in this scene. Run Tower Defense > NPC > Setup Selected Dummy Dialogue again.", this);
            return;
        }

        if (conversation == null)
        {
            Debug.LogWarning("[NPCDialogue] NPCConversation reference is missing.", this);
            return;
        }

        if (blockIfConversationActive && manager.IsConversationActive)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ownsConversation = true;
        observedText = string.Empty;
        lastVisibleCharacters = 0;
        eligibleCharacterCounter = 0;

        BeginCameraFocus();
        manager.StartConversation(conversation);
    }

    private bool CanStartConversation()
    {
        if (requireGameplayStarted && MainMenuController.Instance != null && !MainMenuController.Instance.GameplayStarted)
            return false;

        if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
            return false;

        if (EndGameUIController.Instance != null && EndGameUIController.Instance.rootPanel != null &&
            EndGameUIController.Instance.rootPanel.activeInHierarchy)
            return false;

        if (RelicChoiceUI.Instance != null && RelicChoiceUI.Instance.IsVisible)
            return false;

        return true;
    }

    private void UpdateVoiceBlips()
    {
        if (!ownsConversation || !enableVoiceBlip)
            return;

        ConversationManager manager = ConversationManager.Instance;
        if (manager == null || !manager.IsConversationActive)
            return;

        TMP_Text text = manager.DialogueText;
        if (text == null)
            return;

        string currentText = text.text ?? string.Empty;
        int visible = Mathf.Max(0, text.maxVisibleCharacters);

        if (!string.Equals(currentText, observedText, System.StringComparison.Ordinal))
        {
            observedText = currentText;
            lastVisibleCharacters = 0;
            eligibleCharacterCounter = 0;
        }

        if (visible <= lastVisibleCharacters)
            return;

        int end = Mathf.Min(visible, currentText.Length);
        for (int i = Mathf.Clamp(lastVisibleCharacters, 0, end); i < end; i++)
        {
            char character = currentText[i];
            if (!ShouldVoiceCharacter(character))
                continue;

            eligibleCharacterCounter++;
            if ((eligibleCharacterCounter - 1) % playEveryNCharacters == 0)
                PlayVoiceBlip();
        }

        lastVisibleCharacters = visible;
    }

    private bool ShouldVoiceCharacter(char character)
    {
        if (skipWhitespace && char.IsWhiteSpace(character))
            return false;

        if (skipPunctuation && char.IsPunctuation(character))
            return false;

        return true;
    }

    private void PlayVoiceBlip()
    {
        if (voiceClips == null || voiceClips.Length == 0)
            return;

        AudioClip clip = null;
        int start = Random.Range(0, voiceClips.Length);
        for (int i = 0; i < voiceClips.Length; i++)
        {
            clip = voiceClips[(start + i) % voiceClips.Length];
            if (clip != null) break;
        }

        if (clip == null)
            return;

        EnsureAudioSource();
        if (voiceSource == null)
            return;

        if (!voiceSource.enabled)
            voiceSource.enabled = true;
        if (!voiceSource.gameObject.activeInHierarchy)
            return;

        ApplyAudioSettings();
        voiceSource.pitch = Random.Range(minPitch, maxPitch);
        voiceSource.PlayOneShot(clip, voiceVolume);
    }

    private void BeginCameraFocus()
    {
        if (!enableCameraFocus)
            return;

        Camera cam = dialogueCamera != null ? dialogueCamera : (interactionCamera != null ? interactionCamera : Camera.main);
        if (cam == null)
            return;

        activeDialogueCamera = cam;
        savedCameraPosition = cam.transform.position;
        savedCameraRotation = cam.transform.rotation;
        savedCameraFov = cam.fieldOfView;
        cameraStateCaptured = true;

        Transform reference = cameraFocusReference != null ? cameraFocusReference : transform;
        Vector3 targetPoint = reference.TransformPoint(focusPointLocalOffset);
        Vector3 targetPosition = reference.TransformPoint(cameraLocalOffset);
        Vector3 lookDirection = targetPoint - targetPosition;
        Quaternion targetRotation = lookDirection.sqrMagnitude > 0.000001f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : cam.transform.rotation;
        float targetFov = animateFieldOfView ? focusedFieldOfView : cam.fieldOfView;

        StartCameraRoutine(targetPosition, targetRotation, targetFov, focusDuration, focusCurve, false);
    }

    private void RestoreCameraSmooth()
    {
        if (!cameraStateCaptured || activeDialogueCamera == null)
            return;

        StartCameraRoutine(savedCameraPosition, savedCameraRotation, savedCameraFov, restoreDuration, restoreCurve, true);
    }

    private void RestoreCameraImmediately()
    {
        if (!cameraStateCaptured || activeDialogueCamera == null)
            return;

        if (cameraRoutine != null)
        {
            StopCoroutine(cameraRoutine);
            cameraRoutine = null;
        }

        activeDialogueCamera.transform.position = savedCameraPosition;
        activeDialogueCamera.transform.rotation = savedCameraRotation;
        if (animateFieldOfView)
            activeDialogueCamera.fieldOfView = savedCameraFov;

        cameraStateCaptured = false;
        activeDialogueCamera = null;
    }

    private void StartCameraRoutine(Vector3 targetPosition, Quaternion targetRotation, float targetFov,
        float duration, AnimationCurve curve, bool clearCaptureWhenDone)
    {
        if (cameraRoutine != null)
            StopCoroutine(cameraRoutine);

        cameraRoutine = StartCoroutine(CameraTweenRoutine(targetPosition, targetRotation, targetFov,
            duration, curve, clearCaptureWhenDone));
    }

    private IEnumerator CameraTweenRoutine(Vector3 targetPosition, Quaternion targetRotation, float targetFov,
        float duration, AnimationCurve curve, bool clearCaptureWhenDone)
    {
        Camera cam = activeDialogueCamera;
        if (cam == null)
            yield break;

        Vector3 startPosition = cam.transform.position;
        Quaternion startRotation = cam.transform.rotation;
        float startFov = cam.fieldOfView;

        if (duration <= 0f)
        {
            cam.transform.position = targetPosition;
            cam.transform.rotation = targetRotation;
            if (animateFieldOfView)
                cam.fieldOfView = targetFov;

            FinishCameraTween(clearCaptureWhenDone);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration && cam != null)
        {
            elapsed += useUnscaledCameraTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(t) : t;

            cam.transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, eased);
            cam.transform.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, eased);
            if (animateFieldOfView)
                cam.fieldOfView = Mathf.LerpUnclamped(startFov, targetFov, eased);

            yield return null;
        }

        if (cam != null)
        {
            cam.transform.position = targetPosition;
            cam.transform.rotation = targetRotation;
            if (animateFieldOfView)
                cam.fieldOfView = targetFov;
        }

        FinishCameraTween(clearCaptureWhenDone);
    }

    private void FinishCameraTween(bool clearCaptureWhenDone)
    {
        cameraRoutine = null;
        if (!clearCaptureWhenDone)
            return;

        cameraStateCaptured = false;
        activeDialogueCamera = null;
    }

    private void EnsureAudioSource()
    {
        if (voiceSource != null)
            return;

        voiceSource = GetComponent<AudioSource>();
        if (voiceSource == null)
            voiceSource = gameObject.AddComponent<AudioSource>();
    }

    private void ApplyAudioSettings()
    {
        if (voiceSource == null)
            return;

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = spatialBlend;
        voiceSource.bypassReverbZones = bypassReverbZones;
    }

    private void HandleConversationEnded()
    {
        if (!ownsConversation)
            return;

        ownsConversation = false;
        observedText = string.Empty;
        lastVisibleCharacters = 0;
        eligibleCharacterCounter = 0;

        RestoreCameraSmooth();
    }
}
