using UnityEngine;

/// <summary>
/// Defines one enemy type (e.g. Grunt, Runner, Brute).
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Tower Defense/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "New Enemy";
    public GameObject enemyPrefab;

    [Header("Stats (fully customizable)")]
    public float maxHP = 100f;
    public float moveSpeed = 3f;
    public float hpRegenPerSec = 0f;
    [Min(0f)] public float hpRegenTickInterval = 1f;

    [Header("Rewards & Penalties")]
    public int goldReward = 10;
    public int damageToPlayer = 1;

    [Header("Diamond Drop (fully customizable per enemy type)")]
    [Range(0f, 1f)] public float diamondDropChance = 0f;
    [Min(0)] public int diamondDropMin = 1;
    [Min(0)] public int diamondDropMax = 1;
    [Tooltip("Optional custom world prefab. Add DiamondDropPickup + WorldDropBounceAnimator for full control. If empty, a simple fallback object is created.")]
    public GameObject diamondDropPrefab;
    [Tooltip("Final resting Y offset relative to the enemy death position.")]
    public float diamondGroundYOffset = 0.2f;
    [Tooltip("Boss can always create an extra Diamond drop independent of the normal chance.")]
    public bool bossGuaranteedDiamonds = false;
    [Min(0)] public int bossDiamondMin = 1;
    [Min(0)] public int bossDiamondMax = 3;

    [Header("Relic Drop (fully customizable per enemy type)")]
    [Range(0f, 1f)] public float relicDropChance = 0.01f;
    public RelicRarity minimumDropRarity = RelicRarity.Common;
    public bool isBoss = false;
    public bool bossGuaranteedRelic = true;
    public RelicRarity bossGuaranteedMinimumRarity = RelicRarity.Rare;

    [Header("Kháng khống chế (CC Resist)")]
    [Range(0f, 1f)] public float ccResistPercent = 0f;

    [Header("Giáp ảo (shield) - ngưỡng máu tự kích hoạt")]
    [Range(0f, 1f)] public float shieldTriggerHPPercent = 0f;
    public float shieldTriggerAmount = 0f;
    public float shieldTriggerDuration = 5f;

    [Header("Appearance")]
    public Color tintColor = Color.white;
    public Color shieldTintColor = new Color(0.3f, 0.85f, 1f, 1f);
}
