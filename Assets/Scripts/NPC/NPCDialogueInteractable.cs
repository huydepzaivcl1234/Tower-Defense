using DialogueEditor;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Bridges a world NPC to the imported DialogueEditor package.
/// Clicking this object starts its NPCConversation. While DialogueEditor's typewriter
/// reveals characters, configurable voice blips are played (Undertale-style).
/// Dialogue content remains fully owned/edited by DialogueEditor.
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

    [Header("Click")]
    [Range(0, 2)] public int mouseButton = 0;

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

    private bool ownsConversation;
    private string observedText = string.Empty;
    private int lastVisibleCharacters;
    private int eligibleCharacterCounter;

    private void Reset()
    {
        conversation = GetComponent<NPCConversation>();
        EnsureAudioSource();
    }

    private void Awake()
    {
        if (conversation == null)
            conversation = GetComponent<NPCConversation>();

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
    }

    private void OnValidate()
    {
        mouseButton = Mathf.Clamp(mouseButton, 0, 2);
        playEveryNCharacters = Mathf.Max(1, playEveryNCharacters);
        maxPitch = Mathf.Max(minPitch, maxPitch);

        if (conversation == null)
            conversation = GetComponent<NPCConversation>();

        if (voiceSource != null)
            ApplyAudioSettings();
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(mouseButton))
            TryStartConversation();
    }

    public void TryStartConversation()
    {
        if (!CanStartConversation())
            return;

        ConversationManager manager = ConversationManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[NPCDialogue] No DialogueEditor ConversationManager exists in this scene.", this);
            return;
        }

        if (conversation == null)
        {
            Debug.LogWarning("[NPCDialogue] NPCConversation reference is missing.", this);
            return;
        }

        if (blockIfConversationActive && manager.IsConversationActive)
            return;

        ownsConversation = true;
        observedText = string.Empty;
        lastVisibleCharacters = 0;
        eligibleCharacterCounter = 0;

        manager.StartConversation(conversation);
    }

    private bool CanStartConversation()
    {
        if (ignoreClicksOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

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

    private void Update()
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
    }
}
