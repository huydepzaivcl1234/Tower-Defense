using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DialogueEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Public no-argument methods intended to be called from DialogueEditor Option/Speech node Events.
/// Also synchronizes generic quest availability parameters and automatically injects runtime
/// conditions into quest difficulty options so unavailable Easy/Medium/Hard choices are never shown.
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
    [Tooltip("Automatically hide Easy / Medium / Hard quest options when that difficulty has no available quest. The option is detected from its UnityEvent method, not its displayed text.")]
    public bool autoHideUnavailableDifficultyOptions = true;
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
    public bool logAutoHiddenOptions = false;

    public bool HasAnyQuest => hasAnyQuest;
    public bool HasEasyQuest => hasEasyQuest;
    public bool HasMediumQuest => hasMediumQuest;
    public bool HasHardQuest => hasHardQuest;

    private NPCDialogueInteractable dialogueInteractable;
    private Coroutine delayedRefreshRoutine;

    private static readonly FieldInfo ActiveConversationField =
        typeof(ConversationManager).GetField("m_conversation", BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        if (npcLifecycle == null)
            npcLifecycle = GetComponent<NPCQuestLifecycle>();
        dialogueInteractable = GetComponent<NPCDialogueInteractable>();
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += HandleConversationStarted;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationStarted -= HandleConversationStarted;
        if (delayedRefreshRoutine != null)
        {
            StopCoroutine(delayedRefreshRoutine);
            delayedRefreshRoutine = null;
        }
    }

    private void HandleConversationStarted()
    {
        if (!autoSyncAvailability && !autoHideUnavailableDifficultyOptions)
            return;

        // StartConversation() has already deserialized m_conversation before this event fires,
        // so availability can be written and runtime-only conditions can be injected immediately.
        RefreshAvailabilityValues();

        ConversationManager manager = ConversationManager.Instance;
        Conversation activeConversation = GetActiveConversation(manager);
        if (activeConversation != null && ConversationContainsEventsOwnedByThisBridge(activeConversation.Root))
        {
            if (autoSyncAvailability)
                WriteAvailabilityToConversation(manager, requireActiveState: false);

            if (autoHideUnavailableDifficultyOptions)
                InjectDifficultyAvailabilityConditions(activeConversation.Root);
        }

        // Keep the delayed refresh too, because other dialogue events can change quest state in
        // the same opening frame and this keeps inspector/runtime values synchronized afterwards.
        if (autoSyncAvailability)
        {
            if (delayedRefreshRoutine != null)
                StopCoroutine(delayedRefreshRoutine);
            delayedRefreshRoutine = StartCoroutine(RefreshAfterConversationStarts());
        }
    }

    private IEnumerator RefreshAfterConversationStarts()
    {
        yield return null;
        delayedRefreshRoutine = null;

        if (dialogueInteractable == null)
            dialogueInteractable = GetComponent<NPCDialogueInteractable>();

        if (dialogueInteractable != null && dialogueInteractable.OwnsActiveConversation)
            RefreshQuestAvailability();
    }

    public void AcceptEasyQuest() => AcceptDifficulty(QuestDifficulty.Easy);
    public void AcceptMediumQuest() => AcceptDifficulty(QuestDifficulty.Medium);
    public void AcceptHardQuest() => AcceptDifficulty(QuestDifficulty.Hard);

    public bool IsDifficultyAvailable(QuestDifficulty difficulty)
    {
        RefreshAvailabilityValues();
        switch (difficulty)
        {
            case QuestDifficulty.Easy: return hasEasyQuest;
            case QuestDifficulty.Medium: return hasMediumQuest;
            case QuestDifficulty.Hard: return hasHardQuest;
            default: return false;
        }
    }

    /// <summary>
    /// Recalculates quest availability without caring about objective type, then writes the values
    /// to the currently active DialogueEditor conversation when possible.
    /// </summary>
    public void RefreshQuestAvailability()
    {
        RefreshAvailabilityValues();

        if (!autoSyncAvailability)
            return;

        ConversationManager manager = ConversationManager.Instance;
        WriteAvailabilityToConversation(manager, requireActiveState: true);
    }

    private void RefreshAvailabilityValues()
    {
        QuestManager manager = questManager != null ? questManager : QuestManager.Instance;

        hasEasyQuest = manager != null && manager.HasAvailableQuest(QuestDifficulty.Easy);
        hasMediumQuest = manager != null && manager.HasAvailableQuest(QuestDifficulty.Medium);
        hasHardQuest = manager != null && manager.HasAvailableQuest(QuestDifficulty.Hard);
        hasAnyQuest = hasEasyQuest || hasMediumQuest || hasHardQuest;
    }

    private void WriteAvailabilityToConversation(ConversationManager manager, bool requireActiveState)
    {
        if (manager == null)
            return;
        if (requireActiveState && !manager.IsConversationActive)
            return;

        SetBoolSafe(manager, hasAnyQuestParameter, hasAnyQuest);
        SetBoolSafe(manager, hasEasyQuestParameter, hasEasyQuest);
        SetBoolSafe(manager, hasMediumQuestParameter, hasMediumQuest);
        SetBoolSafe(manager, hasHardQuestParameter, hasHardQuest);
    }

    private static void SetBoolSafe(ConversationManager manager, string parameterName, bool value)
    {
        if (manager == null || string.IsNullOrWhiteSpace(parameterName))
            return;
        manager.SetBool(parameterName, value);
    }

    private static Conversation GetActiveConversation(ConversationManager manager)
    {
        if (manager == null || ActiveConversationField == null)
            return null;
        return ActiveConversationField.GetValue(manager) as Conversation;
    }

    private bool ConversationContainsEventsOwnedByThisBridge(SpeechNode root)
    {
        if (root == null)
            return false;

        HashSet<ConversationNode> visited = new HashSet<ConversationNode>();
        Stack<ConversationNode> stack = new Stack<ConversationNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            ConversationNode node = stack.Pop();
            if (node == null || !visited.Add(node))
                continue;

            OptionNode option = node as OptionNode;
            if (option != null && EventTargetsThisBridge(option.Event, out _))
                return true;

            if (node.Connections == null)
                continue;

            for (int i = 0; i < node.Connections.Count; i++)
            {
                Connection connection = node.Connections[i];
                if (connection is SpeechConnection speechConnection && speechConnection.SpeechNode != null)
                    stack.Push(speechConnection.SpeechNode);
                else if (connection is OptionConnection optionConnection && optionConnection.OptionNode != null)
                    stack.Push(optionConnection.OptionNode);
            }
        }

        return false;
    }

    private void InjectDifficultyAvailabilityConditions(SpeechNode root)
    {
        if (root == null)
            return;

        HashSet<ConversationNode> visited = new HashSet<ConversationNode>();
        Stack<ConversationNode> stack = new Stack<ConversationNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            ConversationNode node = stack.Pop();
            if (node == null || !visited.Add(node))
                continue;

            if (node.Connections == null)
                continue;

            for (int i = 0; i < node.Connections.Count; i++)
            {
                Connection connection = node.Connections[i];

                if (connection is OptionConnection optionConnection && optionConnection.OptionNode != null)
                {
                    QuestDifficulty difficulty;
                    if (TryGetQuestDifficulty(optionConnection.OptionNode.Event, out difficulty))
                    {
                        string parameterName = GetDifficultyParameterName(difficulty);
                        AddRequiredTrueCondition(optionConnection, parameterName);

                        if (logAutoHiddenOptions && !IsCachedDifficultyAvailable(difficulty))
                            Debug.Log($"[QuestDialogue] Hidden unavailable {difficulty} option '{optionConnection.OptionNode.Text}'.", this);
                    }

                    stack.Push(optionConnection.OptionNode);
                }
                else if (connection is SpeechConnection speechConnection && speechConnection.SpeechNode != null)
                {
                    stack.Push(speechConnection.SpeechNode);
                }
            }
        }
    }

    private bool TryGetQuestDifficulty(UnityEvent optionEvent, out QuestDifficulty difficulty)
    {
        difficulty = QuestDifficulty.Easy;
        if (optionEvent == null)
            return false;

        int count = optionEvent.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (optionEvent.GetPersistentTarget(i) != this)
                continue;

            string method = optionEvent.GetPersistentMethodName(i);
            if (method == nameof(AcceptEasyQuest))
            {
                difficulty = QuestDifficulty.Easy;
                return true;
            }
            if (method == nameof(AcceptMediumQuest))
            {
                difficulty = QuestDifficulty.Medium;
                return true;
            }
            if (method == nameof(AcceptHardQuest))
            {
                difficulty = QuestDifficulty.Hard;
                return true;
            }
        }

        return false;
    }

    private bool EventTargetsThisBridge(UnityEvent optionEvent, out string methodName)
    {
        methodName = null;
        if (optionEvent == null)
            return false;

        int count = optionEvent.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (optionEvent.GetPersistentTarget(i) != this)
                continue;

            methodName = optionEvent.GetPersistentMethodName(i);
            if (methodName == nameof(AcceptEasyQuest) ||
                methodName == nameof(AcceptMediumQuest) ||
                methodName == nameof(AcceptHardQuest))
                return true;
        }

        return false;
    }

    private string GetDifficultyParameterName(QuestDifficulty difficulty)
    {
        switch (difficulty)
        {
            case QuestDifficulty.Easy: return hasEasyQuestParameter;
            case QuestDifficulty.Medium: return hasMediumQuestParameter;
            case QuestDifficulty.Hard: return hasHardQuestParameter;
            default: return hasAnyQuestParameter;
        }
    }

    private bool IsCachedDifficultyAvailable(QuestDifficulty difficulty)
    {
        switch (difficulty)
        {
            case QuestDifficulty.Easy: return hasEasyQuest;
            case QuestDifficulty.Medium: return hasMediumQuest;
            case QuestDifficulty.Hard: return hasHardQuest;
            default: return false;
        }
    }

    private static void AddRequiredTrueCondition(Connection connection, string parameterName)
    {
        if (connection == null || string.IsNullOrWhiteSpace(parameterName))
            return;

        if (connection.Conditions == null)
            connection.Conditions = new List<Condition>();

        for (int i = 0; i < connection.Conditions.Count; i++)
        {
            BoolCondition existing = connection.Conditions[i] as BoolCondition;
            if (existing != null && existing.ParameterName == parameterName &&
                existing.CheckType == BoolCondition.eCheckType.equal && existing.RequiredValue)
                return;
        }

        BoolCondition condition = new BoolCondition
        {
            ParameterName = parameterName,
            CheckType = BoolCondition.eCheckType.equal,
            RequiredValue = true
        };
        connection.Conditions.Add(condition);
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
