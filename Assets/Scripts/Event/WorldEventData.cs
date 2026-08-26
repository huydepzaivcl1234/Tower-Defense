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
    [Tooltip("Relative chance inside this rarity. 2 = twice as likely as weight 1.")]
    [Min(0f)] public float selectionWeight = 1f;
    [Tooltip("How many complete waves/rounds this event remains active.")]
    [Min(1)] public int durationRounds = 1;

    [Header("Announcement")]
    public Sprite icon;
    public Color accentColor = Color.white;
    public AudioClip announcementSfx;

    [Header("Dog Cat Rain")]
    [Tooltip("Gold granted when one falling reward reaches the ground.")]
    [Min(0)] public int goldPerDrop = 5;
    [Tooltip("Seconds between drop attempts while at least one enemy is alive.")]
    [Min(0.05f)] public float goldDropInterval = 0.8f;
    [Tooltip("0.30 = enemies have +30% maximum HP while this event is active.")]
    [Range(0f, 5f)] public float enemyMaxHpBonusPercent = 0.30f;
    [Tooltip("Optional 3D object that falls from the sky. Gameplay still works when empty.")]
    public GameObject goldDropPrefab;
    public Vector2 goldDropAreaSize = new Vector2(30f, 18f);
    public float goldDropHeight = 18f;
    [Min(0.05f)] public float goldDropFallDuration = 0.7f;

    [Header("Meteor Shower")]
    [Tooltip("Chance to create a meteor each tick while enemies are alive.")]
    [Range(0f, 1f)] public float meteorChancePerTick = 0.22f;
    [Min(0.05f)] public float meteorTickInterval = 0.8f;
    [Tooltip("Chance that a spawned meteor aims near a living enemy instead of a fully random map point.")]
    [Range(0f, 1f)] public float meteorTargetEnemyChance = 0.75f;
    [Tooltip("Random horizontal offset around a targeted enemy so meteors can miss or hit nearby towers.")]
    [Min(0f)] public float meteorTargetScatterRadius = 2.25f;
    [Tooltip("0.10 = meteor deals 10% of the struck enemy's maximum HP.")]
    [Range(0f, 1f)] public float meteorEnemyMaxHpDamagePercent = 0.10f;
    [Tooltip("0.20 = a hit tower temporarily loses 20% attack speed.")]
    [Range(0f, 1f)] public float meteorTowerAttackSpeedPenaltyPercent = 0.20f;
    [Min(0.05f)] public float meteorTowerDebuffDuration = 4f;
    public GameObject meteorPrefab;
    [Tooltip("Optional VFX spawned at the impact point.")]
    public GameObject meteorImpactVfxPrefab;
    public Vector2 meteorAreaSize = new Vector2(34f, 22f);
    public float meteorSpawnHeight = 20f;
    [Min(0.05f)] public float meteorFallDuration = 0.55f;
    [Min(0.1f)] public float meteorHitRadius = 2.2f;

    [Header("Holy Light - Blessing")]
    [Range(0f, 5f)] public float holyTowerAttackSpeedBonusPercent = 0.25f;
    [Range(0f, 5f)] public float holyTowerDamageBonusPercent = 0.25f;
    [Range(0f, 5f)] public float holyProjectileSpeedBonusPercent = 0.25f;
    [Tooltip("Checked when an affected round ends. If it succeeds, Holy Light ends and the enemy penalty begins next round.")]
    [Range(0f, 1f)] public float holyCollapseChancePerRound = 0.20f;
    public GameObject holyLightVisualPrefab;

    [Header("Holy Light - Collapse Penalty")]
    [Min(1)] public int collapsePenaltyRounds = 2;
    [Range(0f, 5f)] public float collapseEnemyMaxHpBonusPercent = 0.35f;
    [Range(0f, 1f)] public float collapseEnemyCCResistanceBonusPercent = 0.30f;
    [Range(0f, 5f)] public float collapseEnemyShieldPercentOfMaxHp = 0.20f;
    [Tooltip("Optional separate SFX for the moment Holy Light collapses. If empty, Announcement Sfx is reused.")]
    public AudioClip holyCollapseSfx;
    [Tooltip("Optional visual burst spawned when Holy Light collapses.")]
    public GameObject holyCollapseVfxPrefab;
}
