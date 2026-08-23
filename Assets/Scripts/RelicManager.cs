using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the current run's relic state. Relics are permanent until the scene/run is restarted.
/// Supports normal between-wave choices plus world-drop rewards that are queued and opened only
/// when the player presses the Relic Available notification.
/// </summary>
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    [Header("Timing")]
    [Min(1)] public int wavesPerChoice = 3;
    [Range(1, 5)] public int choicesPerRoll = 3;
    [Tooltip("Do not offer a relic after the final wave because the run is already over.")]
    public bool skipAfterFinalWave = true;

    [Header("Pool")]
    public List<RelicData> relicPool = new List<RelicData>();

    [Header("UI")]
    public RelicChoiceUI choiceUI;
    public RelicRewardNotificationUI rewardNotificationUI;

    [Header("World Drop")]
    [Tooltip("Optional custom prefab for relic drops. Leave empty to use the built-in glowing orb fallback.")]
    public GameObject relicDropPrefab;
    [Tooltip("Vertical offset above the enemy death position.")]
    public float relicDropHeight = 0.65f;

    private readonly Dictionary<RelicData, int> stacks = new Dictionary<RelicData, int>();
    private readonly Queue<QueuedRelicReward> queuedRewards = new Queue<QueuedRelicReward>();
    private bool isChoosing;
    private bool activeChoiceCameFromQueue;

    private float towerDamagePercent;
    private float towerAttackSpeedPercent;
    private float towerRangePercent;
    private float goldGainPercent;
    private float buildCostDiscountPercent;
    private float upgradeCostDiscountPercent;

    public bool IsChoosing => isChoosing;
    public int PendingRewardCount => queuedRewards.Count;
    public float TowerDamageMultiplier => Mathf.Max(0f, 1f + towerDamagePercent);
    public float TowerAttackSpeedMultiplier => Mathf.Max(0.01f, 1f + towerAttackSpeedPercent);
    public float TowerRangeMultiplier => Mathf.Max(0.01f, 1f + towerRangePercent);
    public float GoldGainMultiplier => Mathf.Max(0f, 1f + goldGainPercent);

    private struct QueuedRelicReward
    {
        public RelicRarity minimumRarity;
        public string title;

        public QueuedRelicReward(RelicRarity rarity, string rewardTitle)
        {
            minimumRarity = rarity;
            title = rewardTitle;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        RefreshRewardNotification();
    }

    private void OnEnable()
    {
        WaveManager.OnWaveCleared += HandleWaveCleared;
        Enemy.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveCleared -= HandleWaveCleared;
        Enemy.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null || enemy.data == null) return;
        TrySpawnEnemyRelicDrops(enemy.data, enemy.transform.position);
    }

    private void HandleWaveCleared()
    {
        if (isChoosing || WaveManager.Instance == null) return;

        int wave = WaveManager.Instance.CurrentWaveNumber;
        if (wave <= 0 || wave % Mathf.Max(1, wavesPerChoice) != 0) return;
        if (skipAfterFinalWave && wave >= WaveManager.Instance.TotalWaves) return;

        OpenChoice();
    }

    /// <summary>Normal scheduled relic choice. Opens immediately, preserving the existing system.</summary>
    public void OpenChoice()
    {
        OpenChoiceInternal(RelicRarity.Common, "CHOOSE A RELIC", false);
    }

    /// <summary>
    /// Called when the player hovers a world relic drop. The reward is collected instantly but the
    /// choice panel does NOT interrupt gameplay; instead a notification button becomes available.
    /// </summary>
    public void QueueDroppedReward(RelicRarity minimumRarity, bool bossReward)
    {
        string title = bossReward ? "CHOOSE A BOSS RELIC" : "CHOOSE A RELIC";
        queuedRewards.Enqueue(new QueuedRelicReward(minimumRarity, title));
        RefreshRewardNotification();
    }

    public void OpenNextQueuedReward()
    {
        if (isChoosing || queuedRewards.Count == 0) return;
        QueuedRelicReward reward = queuedRewards.Peek();
        OpenChoiceInternal(reward.minimumRarity, reward.title, true);
    }

    private void OpenChoiceInternal(RelicRarity minimumRarity, string title, bool fromQueue)
    {
        List<RelicData> rolled = RollChoices(choicesPerRoll, minimumRarity);
        if (rolled.Count == 0)
        {
            if (fromQueue && queuedRewards.Count > 0)
                queuedRewards.Dequeue();
            RefreshRewardNotification();
            Debug.LogWarning($"No available relics could be rolled for minimum rarity {minimumRarity}.");
            return;
        }

        isChoosing = true;
        activeChoiceCameFromQueue = fromQueue;
        if (choiceUI != null)
            choiceUI.Show(rolled, title);
        else
            Debug.LogWarning("RelicManager rolled relics but no RelicChoiceUI is assigned.");
    }

    public void ChooseRelic(RelicData relic)
    {
        if (!isChoosing || relic == null) return;

        ApplyRelic(relic);
        isChoosing = false;
        if (choiceUI != null) choiceUI.Hide();

        if (activeChoiceCameFromQueue && queuedRewards.Count > 0)
            queuedRewards.Dequeue();

        activeChoiceCameFromQueue = false;
        RefreshRewardNotification();
    }

    /// <summary>Handles both each enemy's normal chance and guaranteed boss rewards.</summary>
    public void TrySpawnEnemyRelicDrops(EnemyData enemyData, Vector3 deathPosition)
    {
        if (enemyData == null) return;

        bool normalDrop = enemyData.relicDropChance > 0f && Random.value <= enemyData.relicDropChance;
        if (normalDrop)
            SpawnWorldReward(deathPosition, enemyData.minimumDropRarity, false);

        if (enemyData.isBoss && enemyData.bossGuaranteedRelic)
            SpawnWorldReward(deathPosition + new Vector3(0.45f, 0f, 0.15f), enemyData.bossGuaranteedMinimumRarity, true);
    }

    private void SpawnWorldReward(Vector3 position, RelicRarity minimumRarity, bool bossReward)
    {
        Vector3 spawnPos = position + Vector3.up * relicDropHeight;
        GameObject go;

        if (relicDropPrefab != null)
        {
            go = Instantiate(relicDropPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = bossReward ? "BossRelicDrop" : "RelicDrop";
            go.transform.position = spawnPos;
            go.transform.localScale = Vector3.one * (bossReward ? 0.85f : 0.62f);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = GetRarityColor(minimumRarity);
                Material material = renderer.material;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color")) material.color = color;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 1.5f);
                }
            }
        }

        RelicDropPickup pickup = go.GetComponent<RelicDropPickup>();
        if (pickup == null) pickup = go.AddComponent<RelicDropPickup>();
        pickup.Configure(minimumRarity, bossReward);
    }

    public static Color GetRarityColor(RelicRarity rarity)
    {
        switch (rarity)
        {
            case RelicRarity.Uncommon: return new Color(0.25f, 0.95f, 0.38f, 1f);
            case RelicRarity.Rare: return new Color(0.20f, 0.55f, 1f, 1f);
            case RelicRarity.Epic: return new Color(0.72f, 0.30f, 1f, 1f);
            case RelicRarity.Legendary: return new Color(1f, 0.67f, 0.12f, 1f);
            default: return new Color(0.88f, 0.92f, 1f, 1f);
        }
    }

    private void RefreshRewardNotification()
    {
        if (rewardNotificationUI != null)
            rewardNotificationUI.SetPendingCount(queuedRewards.Count);
    }

    private void ApplyRelic(RelicData relic)
    {
        int currentStacks = GetStacks(relic);
        if (currentStacks >= Mathf.Max(1, relic.maxStacks)) return;
        stacks[relic] = currentStacks + 1;

        if (relic.modifiers == null) return;
        foreach (RelicModifier modifier in relic.modifiers)
        {
            if (modifier == null) continue;
            switch (modifier.effect)
            {
                case RelicEffectType.TowerDamagePercent:
                    towerDamagePercent += modifier.value;
                    break;
                case RelicEffectType.TowerAttackSpeedPercent:
                    towerAttackSpeedPercent += modifier.value;
                    break;
                case RelicEffectType.TowerRangePercent:
                    towerRangePercent += modifier.value;
                    break;
                case RelicEffectType.GoldGainPercent:
                    goldGainPercent += modifier.value;
                    break;
                case RelicEffectType.BuildCostDiscountPercent:
                    buildCostDiscountPercent += modifier.value;
                    break;
                case RelicEffectType.UpgradeCostDiscountPercent:
                    upgradeCostDiscountPercent += modifier.value;
                    break;
            }
        }
    }

    public int GetStacks(RelicData relic)
    {
        if (relic == null) return 0;
        return stacks.TryGetValue(relic, out int count) ? count : 0;
    }

    private List<RelicData> RollChoices(int count, RelicRarity minimumRarity)
    {
        List<RelicData> candidates = BuildCandidates(minimumRarity);

        // Safety fallback: if the project has not assigned rarities yet, never lose a collected reward.
        if (candidates.Count == 0 && minimumRarity > RelicRarity.Common)
            candidates = BuildCandidates(RelicRarity.Common);

        List<RelicData> result = new List<RelicData>();
        int targetCount = Mathf.Min(Mathf.Max(1, count), candidates.Count);
        while (result.Count < targetCount && candidates.Count > 0)
        {
            float totalWeight = 0f;
            foreach (RelicData candidate in candidates)
                totalWeight += Mathf.Max(0.01f, candidate.selectionWeight);

            float roll = Random.value * totalWeight;
            int selectedIndex = candidates.Count - 1;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= Mathf.Max(0.01f, candidates[i].selectionWeight);
                if (roll <= 0f) { selectedIndex = i; break; }
            }

            result.Add(candidates[selectedIndex]);
            candidates.RemoveAt(selectedIndex);
        }

        return result;
    }

    private List<RelicData> BuildCandidates(RelicRarity minimumRarity)
    {
        List<RelicData> candidates = new List<RelicData>();
        foreach (RelicData relic in relicPool)
        {
            if (relic == null) continue;
            if (relic.rarity < minimumRarity) continue;
            if (GetStacks(relic) >= Mathf.Max(1, relic.maxStacks)) continue;
            candidates.Add(relic);
        }
        return candidates;
    }

    public int GetBuildCost(int baseCost)
    {
        float discount = Mathf.Clamp(buildCostDiscountPercent, 0f, 0.90f);
        return Mathf.Max(0, Mathf.RoundToInt(baseCost * (1f - discount)));
    }

    public int GetUpgradeCost(int baseCost)
    {
        float discount = Mathf.Clamp(upgradeCostDiscountPercent, 0f, 0.90f);
        return Mathf.Max(0, Mathf.RoundToInt(baseCost * (1f - discount)));
    }

    public int ApplyGoldGain(int baseAmount)
    {
        if (baseAmount <= 0) return baseAmount;
        return Mathf.Max(0, Mathf.RoundToInt(baseAmount * GoldGainMultiplier));
    }

    public float ApplyDamage(float baseDamage) => baseDamage * TowerDamageMultiplier;
    public float ApplyAttackSpeed(float baseAttackSpeed) => baseAttackSpeed * TowerAttackSpeedMultiplier;
    public float ApplyRange(float baseRange) => baseRange * TowerRangeMultiplier;
}
