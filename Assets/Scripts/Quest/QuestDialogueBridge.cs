using DialogueEditor;
using UnityEngine;

/// <summary>
/// Public no-argument methods intended to be called from DialogueEditor Option/Speech node Events.
/// Also synchronizes generic quest availability parameters into the active DialogueEditor conversation.
/// </summary>
[DisallowMultipleComponent]
public class QuestDialogueBridge : MonoBehaviour
{
    [Header("Quest Source")]
    [Tooltip("Optional explicit manager. If empty, QuestManager.Instance is used.")]
    public QuestManager questManager;

    [Header("NPC Lifecycle")]
    [Tooltip("Optional. If assigned, this NPC disappears after giving a quest and respawns when that quest is completed.")]
    public NPCQuestLifecycle npcLifecycle;

    [Header("Dialogue Availability Parameters")]
    public bool autoSyncAvailability = true;
    public string hasAnyQuestParameter = "HasAnyQuest";
    public string hasEasyQuestParameter = "HasEasyQuest";
    public string hasMediumQuestParameter = "HasMediumQuest";
    public string hasHardQuestParameter = "HasHardQuest";

    [Header("Runtime Availability (read-only)")]
    [SerializeField] private bool hasAnyQuest;
    [SerializeField] private bool hasEasyQuest;
    [SerializeField] private bool hasMediumQuest;
    [SerializeField] private bool hasHardQuest;

    [Header("Feedback")]
    public bool logAcceptedQuest = true;
    public bool logWhenNoQuestAvailable = true;

    public bool HasAnyQuest => hasAnyQuest;
    public bool HasEasyQuest => hasEasyQuest;
    public bool HasMediumQuest => hasMediumQuest;
    public bool HasHardQuest => hasHardQuest;

    private void Awake()
    {
        if (npcLifecycle == null)
            npcLifecycle = GetComponent<NPCQuestLifecycle>();
    }

    public void AcceptEasyQuest() => AcceptDifficulty(QuestDifficulty.Easy);
    public void AcceptMediumQuest() => AcceptDifficulty(QuestDifficulty.Medium);
    public void AcceptHardQuest() => AcceptDifficulty(QuestDifficulty.Hard);

    /// <summary>
    /// Recalculates quest availability without caring about objective type, then writes the values
    /// to the currently active DialogueEditor conversation when possible.
    /// </summary>
    public void RefreshQuestAvailability()
    {
        QuestManager manager = questManager != null ? questManager : QuestManager.Instance;

        hasEasyQuest = manager != null && manager.HasAvailableQuest(QuestDifficulty.Easy);
        hasMediumQuest = manager != null && manager.HasAvailableQuest(QuestDifficulty.Medium);
        hasHardQuest = manager != null && manager.HasAvailableQuest(QuestDifficulty.Hard);
        hasAnyQuest = hasEasyQuest || hasMediumQuest || hasHardQuest;

        if (!autoSyncAvailability)
            return;

        ConversationManager conversationManager = ConversationManager.Instance;
        if (conversationManager == null || !conversationManager.IsConversationActive)
            return;

        SetBoolSafe(conversationManager, hasAnyQuestParameter, hasAnyQuest);
        SetBoolSafe(conversationManager, hasEasyQuestParameter, hasEasyQuest);
        SetBoolSafe(conversationManager, hasMediumQuestParameter, hasMediumQuest);
        SetBoolSafe(conversationManager, hasHardQuestParameter, hasHardQuest);
    }

    private static void SetBoolSafe(ConversationManager manager, string parameterName, bool value)
    {
        if (manager == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        manager.SetBool(parameterName, value);
    }

    public void AcceptDifficulty(QuestDifficulty difficulty)
    {
        QuestManager manager = questManager != null ? questManager : QuestManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("QuestDialogueBridge: no QuestManager exists in the scene.", this);
            return;
        }

        QuestData quest = manager.RollQuest(difficulty);
        if (quest == null)
        {
            RefreshQuestAvailability();
            if (logWhenNoQuestAvailable)
                Debug.LogWarning($"QuestDialogueBridge: no available {difficulty} quest exists in QuestManager.questPool.", this);
            return;
        }

        if (!manager.AcceptQuest(quest))
        {
            RefreshQuestAvailability();
            if (logWhenNoQuestAvailable)
                Debug.LogWarning($"QuestDialogueBridge: quest '{quest.questTitle}' could not be accepted.", this);
            return;
        }

        if (npcLifecycle == null)
            npcLifecycle = GetComponent<NPCQuestLifecycle>();
        npcLifecycle?.TrackAcceptedQuest(quest);

        RefreshQuestAvailability();

        if (logAcceptedQuest)
            Debug.Log($"Quest accepted [{difficulty}]: {quest.questTitle} ({quest.objectiveType} 0/{Mathf.Max(1, quest.targetAmount)})", this);
    }
}
