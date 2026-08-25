using UnityEngine;

/// <summary>
/// Public no-argument methods intended to be called from DialogueEditor Option/Speech node Events.
/// Attach this to the NPC that offers quests, or any persistent quest giver object.
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

    [Header("Feedback")]
    public bool logAcceptedQuest = true;
    public bool logWhenNoQuestAvailable = true;

    private void Awake()
    {
        if (npcLifecycle == null)
            npcLifecycle = GetComponent<NPCQuestLifecycle>();
    }

    public void AcceptEasyQuest() => AcceptDifficulty(QuestDifficulty.Easy);
    public void AcceptMediumQuest() => AcceptDifficulty(QuestDifficulty.Medium);
    public void AcceptHardQuest() => AcceptDifficulty(QuestDifficulty.Hard);

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
            if (logWhenNoQuestAvailable)
                Debug.LogWarning($"QuestDialogueBridge: no available {difficulty} quest exists in QuestManager.questPool.", this);
            return;
        }

        if (!manager.AcceptQuest(quest))
        {
            if (logWhenNoQuestAvailable)
                Debug.LogWarning($"QuestDialogueBridge: quest '{quest.questTitle}' could not be accepted.", this);
            return;
        }

        if (npcLifecycle == null)
            npcLifecycle = GetComponent<NPCQuestLifecycle>();
        npcLifecycle?.TrackAcceptedQuest(quest);

        if (logAcceptedQuest)
            Debug.Log($"Quest accepted [{difficulty}]: {quest.questTitle} ({quest.objectiveType} 0/{Mathf.Max(1, quest.targetAmount)})", this);
    }
}
