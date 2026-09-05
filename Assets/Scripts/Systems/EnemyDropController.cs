using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single owner of enemy-death drop rules. Diamond and Relic rolls stay independent.
/// Presentation/spawning remains delegated to DiamondDropSystem and RelicManager.
/// </summary>
[DisallowMultipleComponent]
public class EnemyDropController : MonoBehaviour
{
    public static EnemyDropController Instance { get; private set; }

    [Header("Drop Data")]
    [Tooltip("Single database containing all enemy-specific Diamond and Relic drop tuning.")]
    public EnemyDropDatabase dropDatabase;

    [Header("Systems")]
    [Tooltip("Presentation/spawn system for Diamond world drops.")]
    public DiamondDropSystem diamondDropSystem;
    [Tooltip("Optional explicit RelicManager reference. If empty, RelicManager.Instance is used.")]
    public RelicManager relicManager;

    [Header("Boss Spawn Offsets")]
    [Tooltip("Extra offset for guaranteed Boss Diamond so it does not overlap a normal Diamond drop.")]
    public Vector3 bossDiamondSpawnOffset = new Vector3(0.4f, 0f, 0.2f);
    [Tooltip("Extra offset for guaranteed Boss Relic so it does not overlap other drops.")]
    public Vector3 bossRelicSpawnOffset = new Vector3(0.45f, 0f, 0.15f);

    [Header("Diagnostics")]
    [Tooltip("Log a warning once when an EnemyData has no entry in the drop database.")]
    public bool warnWhenEntryMissing = true;

    private readonly HashSet<EnemyData> warnedMissing = new HashSet<EnemyData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveSystems();
    }

    private void OnEnable() => Enemy.OnAnyEnemyDied += HandleEnemyDied;
    private void OnDisable() => Enemy.OnAnyEnemyDied -= HandleEnemyDied;

    private void ResolveSystems()
    {
        if (diamondDropSystem == null)
            diamondDropSystem = Object.FindAnyObjectByType<DiamondDropSystem>(FindObjectsInactive.Include);

        if (relicManager == null)
            relicManager = RelicManager.Instance ?? Object.FindAnyObjectByType<RelicManager>(FindObjectsInactive.Include);
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null || enemy.data == null || dropDatabase == null)
            return;

        if (!dropDatabase.TryGet(enemy.data, out EnemyDropEntry entry) || entry == null)
        {
            if (warnWhenEntryMissing && warnedMissing.Add(enemy.data))
                Debug.LogWarning($"EnemyDropController: no drop entry configured for '{enemy.data.name}'. No Diamond/Relic drop was rolled.", this);
            return;
        }

        ResolveSystems();
        Vector3 deathPosition = enemy.transform.position;

        RollDiamond(entry, deathPosition);
        RollRelic(entry, deathPosition);
    }

    private void RollDiamond(EnemyDropEntry entry, Vector3 deathPosition)
    {
        if (diamondDropSystem == null)
            return;

        float chance = Mathf.Clamp01(entry.diamondDropChance);
        if (chance > 0f && UnityEngine.Random.value < chance)
            diamondDropSystem.SpawnDrop(deathPosition, RollAmount(entry.diamondDropMin, entry.diamondDropMax));

        if (entry.isBoss && entry.bossGuaranteedDiamonds)
            diamondDropSystem.SpawnDrop(
                deathPosition + bossDiamondSpawnOffset,
                RollAmount(entry.bossDiamondMin, entry.bossDiamondMax));
    }

    private void RollRelic(EnemyDropEntry entry, Vector3 deathPosition)
    {
        RelicManager manager = relicManager != null ? relicManager : RelicManager.Instance;
        if (manager == null)
            return;

        float chance = manager.GetEffectiveEnemyRelicDropChance(entry.relicDropChance);
        if (chance > 0f && UnityEngine.Random.value <= chance)
            manager.SpawnDroppedRelicReward(deathPosition, entry.minimumDropRarity, false);

        if (entry.isBoss && entry.bossGuaranteedRelic)
            manager.SpawnDroppedRelicReward(
                deathPosition + bossRelicSpawnOffset,
                entry.bossGuaranteedMinimumRarity,
                true);
    }

    private static int RollAmount(int min, int max)
    {
        int safeMin = Mathf.Max(0, Mathf.Min(min, max));
        int safeMax = Mathf.Max(safeMin, Mathf.Max(min, max));
        return safeMax <= safeMin ? safeMin : UnityEngine.Random.Range(safeMin, safeMax + 1);
    }
}
