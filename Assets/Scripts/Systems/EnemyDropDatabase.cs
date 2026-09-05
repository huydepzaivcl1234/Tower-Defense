using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyDropEntry
{
    [Header("Enemy")]
    [Tooltip("Enemy type this drop configuration belongs to.")]
    public EnemyData enemy;

    [Header("Diamond Drop")]
    [Range(0f, 1f)]
    [Tooltip("Independent chance for this enemy to drop Diamonds. 0 disables the normal Diamond drop.")]
    public float diamondDropChance = 0f;
    [Min(0)] public int diamondDropMin = 1;
    [Min(0)] public int diamondDropMax = 1;

    [Header("Diamond Boss Reward")]
    [Tooltip("Treat this entry as a boss for guaranteed drop rules.")]
    public bool isBoss = false;
    public bool bossGuaranteedDiamonds = false;
    [Min(0)] public int bossDiamondMin = 1;
    [Min(0)] public int bossDiamondMax = 3;

    [Header("Relic Drop")]
    [Range(0f, 1f)]
    [Tooltip("Base Relic chance before runtime Relic modifiers are applied. 0 disables the normal Relic drop.")]
    public float relicDropChance = 0.01f;
    public RelicRarity minimumDropRarity = RelicRarity.Common;

    [Header("Relic Boss Reward")]
    public bool bossGuaranteedRelic = true;
    public RelicRarity bossGuaranteedMinimumRarity = RelicRarity.Rare;
}

/// <summary>
/// Single data source for all enemy-specific Diamond/Relic drop tuning.
/// EnemyData remains focused on enemy stats and no runtime drop system reads legacy EnemyData drop fields.
/// </summary>
[CreateAssetMenu(fileName = "EnemyDropDatabase", menuName = "Tower Defense/Drop/Enemy Drop Database")]
public class EnemyDropDatabase : ScriptableObject
{
    [Tooltip("One entry per EnemyData. Entries with Enemy = None are ignored.")]
    public List<EnemyDropEntry> entries = new List<EnemyDropEntry>();

    public bool TryGet(EnemyData enemy, out EnemyDropEntry entry)
    {
        entry = null;
        if (enemy == null || entries == null)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            EnemyDropEntry candidate = entries[i];
            if (candidate != null && candidate.enemy == enemy)
            {
                entry = candidate;
                return true;
            }
        }

        return false;
    }
}
