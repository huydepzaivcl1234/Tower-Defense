using UnityEngine;

public enum WorldEventRarity
{
    Common,
    Rare
}

public enum WorldEventType
{
    DogCatRain,
    MeteorShower,
    HolyLight
}

[CreateAssetMenu(fileName = "WorldEvent_", menuName = "Tower Defense/World Event/World Event Data")]
public class WorldEventData : ScriptableObject
{
    [Header("Identity")]
    public string eventName = "World Event";
    [TextArea(2, 5)] public string description;
    public WorldEventRarity rarity = WorldEventRarity.Common;
    public WorldEventType eventType = WorldEventType.DogCatRain;
    [Min(0f)] public float selectionWeight = 1f;
    [Min(1)] public int durationRounds = 1;

    [Header("Announcement")]
    public Sprite icon;
    public Color accentColor = Color.white;
    public AudioClip announcementSfx;

    [Header("Dog Cat Rain")]
    [Min(0)] public int goldPerDrop = 5;
    [Min(0.05f)] public float goldDropInterval = 0.8f;
    [Range(0f, 5f)] public float enemyMaxHpBonusPercent = 0.30f;
    public GameObject goldDropPrefab;
    public Vector2 goldDropAreaSize = new Vector2(30f, 18f);
    public float goldDropHeight = 18f;
    [Min(0.05f)] public float goldDropFallDuration = 0.7f;

    [Header("Meteor Shower")]
    [Range(0f, 1f)] public float meteorChancePerTick = 0.22f;
    [Min(0.05f)] public float meteorTickInterval = 0.8f;
    [Range(0f, 1f)] public float meteorEnemyMaxHpDamagePercent = 0.10f;
    [Range(0f, 1f)] public float meteorTowerAttackSpeedPenaltyPercent = 0.20f;
    [Min(0.05f)] public float meteorTowerDebuffDuration = 4f;
    public GameObject meteorPrefab;
    public Vector2 meteorAreaSize = new Vector2(34f, 22f);
    public float meteorSpawnHeight = 20f;
    [Min(0.05f)] public float meteorFallDuration = 0.55f;
    [Min(0.1f)] public float meteorHitRadius = 2.2f;

    [Header("Holy Light - Blessing")]
    [Range(0f, 5f)] public float holyTowerAttackSpeedBonusPercent = 0.25f;
    [Range(0f, 5f)] public float holyTowerDamageBonusPercent = 0.25f;
    [Range(0f, 5f)] public float holyProjectileSpeedBonusPercent = 0.25f;
    [Range(0f, 1f)] public float holyCollapseChancePerRound = 0.20f;
    public GameObject holyLightVisualPrefab;

    [Header("Holy Light - Collapse Penalty")]
    [Min(1)] public int collapsePenaltyRounds = 2;
    [Range(0f, 5f)] public float collapseEnemyMaxHpBonusPercent = 0.35f;
    [Range(0f, 1f)] public float collapseEnemyCCResistanceBonusPercent = 0.30f;
    [Range(0f, 5f)] public float collapseEnemyShieldPercentOfMaxHp = 0.20f;
}
