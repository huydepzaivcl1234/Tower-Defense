using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the current run's relic state. Relics are permanent until the scene/run is restarted.
/// Supports normal between-wave choices plus world-drop rewards and advanced data-driven relic effects.
/// </summary>
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    [Header("Timing")]
    [Min(1)] public int wavesPerChoice = 3;
    [Range(1, 5)] public int choicesPerRoll = 3;
    public bool skipAfterFinalWave = true;

    [Header("Pool")]
    public List<RelicData> relicPool = new List<RelicData>();

    [Header("UI")]
    public RelicChoiceUI choiceUI;
    public RelicRewardNotificationUI rewardNotificationUI;

    [Header("World Drop")]
    public GameObject relicDropPrefab;
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
    private float relicDropChanceFlat;
    private float critChance;
    private float critExtraDamageMultiplier;
    private float projectileSpeedPercent;

    private bool cannonHeroActive;
    private TowerData cannonHeroTower;
    private bool cannonHeroPurchaseUsed;
    private float cannonHeroRangeFlat;
    private float cannonHeroDamagePerLevel;
    private float cannonHeroTravelPercentPerStep;
    private float cannonHeroTravelDistancePerStep = 1f;
    private float cannonHeroTravelBonusCap;

    private float enemyWeakSpawnChance;
    private float enemyWeakHpFraction = 1f;

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
        public QueuedRelicReward(RelicRarity rarity, string rewardTitle) { minimumRarity = rarity; title = rewardTitle; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => RefreshRewardNotification();

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

    public void OpenChoice() => OpenChoiceInternal(RelicRarity.Common, "CHOOSE A RELIC", false);

    public void QueueDroppedReward(RelicRarity minimumRarity, bool bossReward)
    {
        queuedRewards.Enqueue(new QueuedRelicReward(minimumRarity, bossReward ? "CHOOSE A BOSS RELIC" : "CHOOSE A RELIC"));
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
            if (fromQueue && queuedRewards.Count > 0) queuedRewards.Dequeue();
            RefreshRewardNotification();
            Debug.LogWarning($"No available relics could be rolled for minimum rarity {minimumRarity}.");
            return;
        }
        isChoosing = true;
        activeChoiceCameFromQueue = fromQueue;
        if (choiceUI != null) choiceUI.Show(rolled, title);
        else Debug.LogWarning("RelicManager rolled relics but no RelicChoiceUI is assigned.");
    }

    public void ChooseRelic(RelicData relic)
    {
        if (!isChoosing || relic == null) return;
        ApplyRelic(relic);
        isChoosing = false;
        if (choiceUI != null) choiceUI.Hide();
        if (activeChoiceCameFromQueue && queuedRewards.Count > 0) queuedRewards.Dequeue();
        activeChoiceCameFromQueue = false;
        RefreshRewardNotification();
    }

    public void TrySpawnEnemyRelicDrops(EnemyData enemyData, Vector3 deathPosition)
    {
        if (enemyData == null) return;
        float chance = Mathf.Clamp01(enemyData.relicDropChance + relicDropChanceFlat);
        if (chance > 0f && Random.value <= chance)
            SpawnWorldReward(deathPosition, enemyData.minimumDropRarity, false);
        if (enemyData.isBoss && enemyData.bossGuaranteedRelic)
            SpawnWorldReward(deathPosition + new Vector3(0.45f, 0f, 0.15f), enemyData.bossGuaranteedMinimumRarity, true);
    }

    private void SpawnWorldReward(Vector3 position, RelicRarity minimumRarity, bool bossReward)
    {
        Vector3 spawnPos = position + Vector3.up * relicDropHeight;
        GameObject go;
        if (relicDropPrefab != null) go = Instantiate(relicDropPrefab, spawnPos, Quaternion.identity);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = bossReward ? "BossRelicDrop" : "RelicDrop";
            go.transform.position = spawnPos;
            go.transform.localScale = Vector3.one * (bossReward ? 0.85f : 0.62f);
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
        if (rewardNotificationUI != null) rewardNotificationUI.SetPendingCount(queuedRewards.Count);
    }

    private void ApplyRelic(RelicData relic)
    {
        int oldStacks = GetStacks(relic);
        int maxStacks = Mathf.Max(1, relic.maxStacks);
        if (oldStacks >= maxStacks) return;
        int newStacks = oldStacks + 1;
        stacks[relic] = newStacks;

        if (relic.modifiers == null) return;
        foreach (RelicModifier modifier in relic.modifiers)
        {
            if (modifier == null) continue;
            switch (modifier.effect)
            {
                case RelicEffectType.TowerDamagePercent: towerDamagePercent += modifier.value; break;
                case RelicEffectType.TowerAttackSpeedPercent: towerAttackSpeedPercent += modifier.value; break;
                case RelicEffectType.TowerRangePercent: towerRangePercent += modifier.value; break;
                case RelicEffectType.GoldGainPercent: goldGainPercent += modifier.value; break;
                case RelicEffectType.BuildCostDiscountPercent: buildCostDiscountPercent += modifier.value; break;
                case RelicEffectType.UpgradeCostDiscountPercent: upgradeCostDiscountPercent += modifier.value; break;
                case RelicEffectType.AddLivesFlat:
                    if (GameManager.Instance != null) GameManager.Instance.AddLives(Mathf.RoundToInt(modifier.value));
                    break;
                case RelicEffectType.RelicDropChanceFlat:
                    relicDropChanceFlat += modifier.value;
                    break;
                case RelicEffectType.CriticalChance:
                    critChance = Mathf.Clamp01(critChance + modifier.value);
                    critExtraDamageMultiplier = Mathf.Max(critExtraDamageMultiplier, modifier.value2);
                    break;
                case RelicEffectType.ProjectileSpeedPercent:
                    projectileSpeedPercent += modifier.value;
                    break;
                case RelicEffectType.CannonHero:
                    if (!cannonHeroActive)
                    {
                        cannonHeroActive = true;
                        cannonHeroTower = modifier.targetTower;
                        cannonHeroRangeFlat = modifier.value;
                        cannonHeroDamagePerLevel = modifier.value2;
                        cannonHeroTravelPercentPerStep = modifier.value3;
                        cannonHeroTravelBonusCap = modifier.value4;
                        cannonHeroTravelDistancePerStep = 1f;
                        RemoveExistingTargetTowers(cannonHeroTower);
                    }
                    break;
                case RelicEffectType.EnemySpawnWeakness:
                    float t = maxStacks <= 1 ? 1f : (newStacks - 1f) / (maxStacks - 1f);
                    enemyWeakSpawnChance = Mathf.Lerp(modifier.value, modifier.value2, t);
                    enemyWeakHpFraction = Mathf.Lerp(modifier.value3, modifier.value4, t);
                    break;
            }
        }
    }

    private void RemoveExistingTargetTowers(TowerData target)
    {
        if (target == null) return;
        for (int i = Tower.ActiveTowers.Count - 1; i >= 0; i--)
        {
            Tower tower = Tower.ActiveTowers[i];
            if (tower == null || tower.data != target) continue;
            if (tower.occupiedSpot != null) tower.occupiedSpot.ClearSpot();
            Destroy(tower.gameObject);
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
        if (candidates.Count == 0 && minimumRarity > RelicRarity.Common) candidates = BuildCandidates(RelicRarity.Common);
        List<RelicData> result = new List<RelicData>();
        int targetCount = Mathf.Min(Mathf.Max(1, count), candidates.Count);
        while (result.Count < targetCount && candidates.Count > 0)
        {
            float totalWeight = 0f;
            foreach (RelicData candidate in candidates) totalWeight += Mathf.Max(0.01f, candidate.selectionWeight);
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
            if (relic == null || relic.rarity < minimumRarity) continue;
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

    public float ApplyDamage(Tower tower, float baseDamage)
    {
        float damage = ApplyDamage(baseDamage) + GetDamageFromLives();
        if (tower != null && cannonHeroActive && cannonHeroTower != null && tower.data == cannonHeroTower)
            damage += cannonHeroDamagePerLevel * tower.CurrentLevelNumber;
        return damage;
    }

    private float GetDamageFromLives()
    {
        if (GameManager.Instance == null) return 0f;
        float total = 0f;
        foreach (var pair in stacks)
        {
            RelicData relic = pair.Key;
            int count = pair.Value;
            if (relic == null || relic.modifiers == null) continue;
            foreach (RelicModifier mod in relic.modifiers)
            {
                if (mod == null || mod.effect != RelicEffectType.DamagePerLives) continue;
                float livesPerStep = Mathf.Max(1f, mod.value2);
                total += Mathf.Floor(GameManager.Instance.CurrentLives / livesPerStep) * mod.value * count;
            }
        }
        return total;
    }

    public float ApplyAttackSpeed(float baseAttackSpeed) => baseAttackSpeed * TowerAttackSpeedMultiplier;
    public float ApplyRange(float baseRange) => baseRange * TowerRangeMultiplier;

    public float ApplyRange(Tower tower, float baseRange)
    {
        float result = ApplyRange(baseRange);
        if (tower != null && cannonHeroActive && cannonHeroTower != null && tower.data == cannonHeroTower)
            result += cannonHeroRangeFlat;
        return result;
    }

    public float ApplyProjectileSpeed(float baseSpeed) => baseSpeed * Mathf.Max(0.01f, 1f + projectileSpeedPercent);

    public float RollCriticalDamage(float damage)
    {
        if (critChance <= 0f || Random.value > Mathf.Clamp01(critChance)) return damage;
        return damage * (1f + Mathf.Max(0f, critExtraDamageMultiplier));
    }

    public float ApplyProjectileTravelDamage(TowerData sourceTower, float damage, float distanceTravelled)
    {
        if (!cannonHeroActive || sourceTower == null || sourceTower != cannonHeroTower) return damage;
        float stepDistance = Mathf.Max(0.01f, cannonHeroTravelDistancePerStep);
        float steps = Mathf.Floor(Mathf.Max(0f, distanceTravelled) / stepDistance);
        float bonus = Mathf.Min(cannonHeroTravelBonusCap, steps * cannonHeroTravelPercentPerStep);
        return damage * (1f + Mathf.Max(0f, bonus));
    }

    public bool CanBuildTower(TowerData towerData)
    {
        if (!cannonHeroActive || cannonHeroTower == null || towerData != cannonHeroTower) return true;
        return !cannonHeroPurchaseUsed;
    }

    public void NotifyTowerBuilt(TowerData towerData)
    {
        if (cannonHeroActive && cannonHeroTower != null && towerData == cannonHeroTower)
            cannonHeroPurchaseUsed = true;
    }

    public float GetSpawnHpMultiplier()
    {
        if (enemyWeakSpawnChance <= 0f || Random.value > Mathf.Clamp01(enemyWeakSpawnChance)) return 1f;
        return Mathf.Clamp(enemyWeakHpFraction, 0.01f, 1f);
    }
}
