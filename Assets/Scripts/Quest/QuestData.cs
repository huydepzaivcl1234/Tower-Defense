using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestDifficulty
{
    Easy,
    Medium,
    Hard
}

public enum QuestObjectiveType
{
    KillEnemies,
    SpendGold,
    UpgradeTowers
}

public enum QuestRewardType
{
    Gold,
    Relic,
    Item
}

[Serializable]
public class QuestReward
{
    public QuestRewardType type = QuestRewardType.Gold;

    [Min(0)] public int amount = 1;

    [Tooltip("Used when Type = Relic.")]
    public RelicData relic;

    [Tooltip("Reserved for the future inventory/item system. No gameplay grant is performed yet for Item rewards.")]
    public UnityEngine.Object itemAsset;
}

[CreateAssetMenu(fileName = "QuestData", menuName = "Tower Defense/Quest/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    public string questId = "quest_id";
    public string questTitle = "New Quest";
    [TextArea(2, 5)] public string description;

    [Header("Difficulty")]
    public QuestDifficulty difficulty = QuestDifficulty.Easy;

    [Header("Objective")]
    public QuestObjectiveType objectiveType = QuestObjectiveType.KillEnemies;
    [Min(1)] public int targetAmount = 1;

    [Header("Rewards")]
    public List<QuestReward> rewards = new List<QuestReward>();

    [Header("Rules")]
    [Tooltip("If false, this quest cannot be accepted again after completing it during the current run.")]
    public bool repeatable = false;

    [Tooltip("Relative chance when a quest is randomly selected from other quests of the same difficulty.")]
    [Min(0f)] public float selectionWeight = 1f;
}
