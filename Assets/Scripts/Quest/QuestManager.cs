using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActiveQuest
{
    public QuestData data;
    public int progress;
    public bool completed;
    public bool rewardsGranted;
}

[DisallowMultipleComponent]
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Pool")]
    public List<QuestData> questPool = new List<QuestData>();

    [Header("Runtime Rules")]
    [Min(1)] public int maxActiveQuests = 3;
    public bool autoGrantRewardsOnComplete = true;
    public bool preventDuplicateActiveQuest = true;

    [Header("Runtime (read-only)")]
    [SerializeField] private List<ActiveQuest> activeQuests = new List<ActiveQuest>();
    [SerializeField] private List<QuestData> completedNonRepeatable = new List<QuestData>();

    public IReadOnlyList<ActiveQuest> ActiveQuests => activeQuests;

    public static event Action<ActiveQuest> OnQuestAccepted;
    public static event Action<ActiveQuest> OnQuestProgressChanged;
    public static event Action<ActiveQuest> OnQuestCompleted;
    public static event Action<ActiveQuest> OnQuestRewardsGranted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        Enemy.OnAnyEnemyDied += HandleEnemyDied;
        GameManager.OnGoldSpent += HandleGoldSpent;
    }

    private void OnDisable()
    {
        Enemy.OnAnyEnemyDied -= HandleEnemyDied;
        GameManager.OnGoldSpent -= HandleGoldSpent;
    }

    public bool AcceptRandomQuest(QuestDifficulty difficulty)
    {
        QuestData selected = RollQuest(difficulty);
        return selected != null && AcceptQuest(selected);
    }

    public bool AcceptQuest(QuestData quest)
    {
        if (quest == null) return false;
        if (activeQuests.Count >= Mathf.Max(1, maxActiveQuests)) return false;

        if (preventDuplicateActiveQuest)
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                if (activeQuests[i] != null && activeQuests[i].data == quest && !activeQuests[i].completed)
                    return false;
            }
        }

        if (!quest.repeatable && completedNonRepeatable.Contains(quest))
            return false;

        ActiveQuest runtime = new ActiveQuest
        {
            data = quest,
            progress = 0,
            completed = false,
            rewardsGranted = false
        };

        activeQuests.Add(runtime);
        OnQuestAccepted?.Invoke(runtime);
        return true;
    }

    public QuestData RollQuest(QuestDifficulty difficulty)
    {
        List<QuestData> candidates = new List<QuestData>();
        float totalWeight = 0f;

        for (int i = 0; i < questPool.Count; i++)
        {
            QuestData quest = questPool[i];
            if (quest == null || quest.difficulty != difficulty) continue;
            if (!quest.repeatable && completedNonRepeatable.Contains(quest)) continue;

            if (preventDuplicateActiveQuest)
            {
                bool alreadyActive = false;
                for (int j = 0; j < activeQuests.Count; j++)
                {
                    if (activeQuests[j] != null && activeQuests[j].data == quest && !activeQuests[j].completed)
                    {
                        alreadyActive = true;
                        break;
                    }
                }
                if (alreadyActive) continue;
            }

            float weight = Mathf.Max(0f, quest.selectionWeight);
            if (weight <= 0f) continue;
            candidates.Add(quest);
            totalWeight += weight;
        }

        if (candidates.Count == 0 || totalWeight <= 0f) return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cursor = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cursor += Mathf.Max(0f, candidates[i].selectionWeight);
            if (roll <= cursor) return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    private void HandleEnemyDied(Enemy enemy) => AddProgress(QuestObjectiveType.KillEnemies, 1);

    private void HandleGoldSpent(int amount)
    {
        if (amount > 0) AddProgress(QuestObjectiveType.SpendGold, amount);
    }

    public void NotifyTowerUpgraded(Tower tower)
    {
        if (tower != null) AddProgress(QuestObjectiveType.UpgradeTowers, 1);
    }

    public void AddProgress(QuestObjectiveType objectiveType, int amount)
    {
        if (amount <= 0) return;

        for (int i = 0; i < activeQuests.Count; i++)
        {
            ActiveQuest quest = activeQuests[i];
            if (quest == null || quest.data == null || quest.completed) continue;
            if (quest.data.objectiveType != objectiveType) continue;

            int target = Mathf.Max(1, quest.data.targetAmount);
            quest.progress = Mathf.Min(target, quest.progress + amount);
            OnQuestProgressChanged?.Invoke(quest);
            if (quest.progress >= target) CompleteQuest(quest);
        }
    }

    private void CompleteQuest(ActiveQuest quest)
    {
        if (quest == null || quest.data == null || quest.completed) return;
        quest.completed = true;

        if (!quest.data.repeatable && !completedNonRepeatable.Contains(quest.data))
            completedNonRepeatable.Add(quest.data);

        OnQuestCompleted?.Invoke(quest);
        if (autoGrantRewardsOnComplete) GrantRewards(quest);
    }

    public void GrantRewards(ActiveQuest quest)
    {
        if (quest == null || quest.data == null || !quest.completed || quest.rewardsGranted) return;

        if (quest.data.rewards != null)
        {
            for (int i = 0; i < quest.data.rewards.Count; i++)
            {
                QuestReward reward = quest.data.rewards[i];
                if (reward == null) continue;

                switch (reward.type)
                {
                    case QuestRewardType.Gold:
                        if (GameManager.Instance != null && reward.amount > 0)
                            GameManager.Instance.AddGold(reward.amount, false);
                        break;

                    case QuestRewardType.Relic:
                        if (RelicManager.Instance != null && reward.relic != null)
                        {
                            int count = Mathf.Max(1, reward.amount);
                            for (int n = 0; n < count; n++)
                                RelicManager.Instance.QueueDroppedReward(reward.relic.rarity, false);
                        }
                        break;

                    case QuestRewardType.Item:
                        Debug.Log($"Quest reward Item is reserved for the future inventory system: {reward.itemAsset}");
                        break;
                }
            }
        }

        quest.rewardsGranted = true;
        OnQuestRewardsGranted?.Invoke(quest);
    }
}
