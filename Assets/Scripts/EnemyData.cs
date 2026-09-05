using UnityEngine;

/// <summary>
/// Defines one enemy type. This asset only owns enemy gameplay/balance data.
/// Drop models, SFX, VFX and drop animation presentation live on the corresponding drop prefabs/systems.
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

    [Header("Kháng khống chế (CC Resist)")]
    [Range(0f, 1f)] public float ccResistPercent = 0f;

    [Header("Giáp ảo (shield) - ngưỡng máu tự kích hoạt")]
    [Range(0f, 1f)] public float shieldTriggerHPPercent = 0f;
    public float shieldTriggerAmount = 0f;
    public float shieldTriggerDuration = 5f;

    [Header("Appearance")]
    public Color tintColor = Color.white;
    public Color shieldTintColor = new Color(0.3f, 0.85f, 1f, 1f);

    // Legacy serialized drop fields are kept hidden only so existing EnemyData assets can be migrated
    // safely into EnemyDropDatabase without losing their current values. Runtime systems never read them.
    [HideInInspector] public float diamondDropChance = 0f;
    [HideInInspector] public int diamondDropMin = 1;
    [HideInInspector] public int diamondDropMax = 1;
    [HideInInspector] public bool bossGuaranteedDiamonds = false;
    [HideInInspector] public int bossDiamondMin = 1;
    [HideInInspector] public int bossDiamondMax = 3;
    [HideInInspector] public float relicDropChance = 0.01f;
    [HideInInspector] public RelicRarity minimumDropRarity = RelicRarity.Common;
    [HideInInspector] public bool isBoss = false;
    [HideInInspector] public bool bossGuaranteedRelic = true;
    [HideInInspector] public RelicRarity bossGuaranteedMinimumRarity = RelicRarity.Rare;

    // Older Diamond presentation fields are also retained only for serialization compatibility.
    [HideInInspector] public GameObject diamondDropPrefab;
    [HideInInspector] public float diamondGroundYOffset = 0.2f;
}
