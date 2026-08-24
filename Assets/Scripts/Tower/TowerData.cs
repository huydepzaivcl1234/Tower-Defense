using UnityEngine;

/// <summary>
/// Fully customizable per-level stats for a tower.
/// Strength = damage dealt per hit. AttackSpeed = shots per second. Range = targeting radius.
/// </summary>
[System.Serializable]
public class TowerLevelStats
{
    [Tooltip("Damage dealt per hit")]
    public float strength = 10f;

    [Tooltip("Shots fired per second")]
    public float attackSpeed = 1f;

    [Tooltip("Targeting range in world units")]
    public float range = 5f;

    [Tooltip("Gold cost to upgrade INTO this level. Ignored for level 1 (index 0).")]
    public int upgradeCost = 50;

    [Tooltip("'Lan' / splash: if greater than 0, a hit also damages every OTHER enemy within " +
             "this radius of the impact point, at the same damage as Strength. Leave at 0 for a " +
             "normal single-target hit. Great for a Cannon-type tower's higher levels.")]
    public float splashRadius = 0f;

    [Header("Chảy máu (bleed / damage over time)")]
    [Tooltip("Damage dealt on each bleed tick, e.g. 10 damage per tick.")]
    public float bleedDamagePerTick = 0f;
    [Tooltip("Seconds between ticks, e.g. 1 = once a second, 0.1 = 10 times a second for smoother/finer ticking. Ignored if Bleed Damage Per Tick is 0.")]
    public float bleedTickInterval = 1f;
    [Tooltip("Total seconds the bleed lasts. Ignored if Bleed Damage Per Tick is 0. " +
             "Total bleed damage ≈ Bleed Damage Per Tick × (Bleed Duration ÷ Bleed Tick Interval).")]
    public float bleedDuration = 0f;

    [Header("Làm chậm (slow)")]
    [Tooltip("Move-speed reduction on hit, as a fraction (0.3 = 30% slower). 0 = no slow.")]
    [Range(0f, 1f)] public float slowPercent = 0f;
    [Tooltip("How many seconds the slow lasts. Ignored if Slow Percent is 0.")]
    public float slowDuration = 0f;

    [Header("Đẩy lùi (knockback)")]
    [Tooltip("Pushes the enemy backward along the path on hit, in world units. 0 = no knockback. " +
             "Reduced by the target's CC Resist Percent (see EnemyData).")]
    public float knockbackDistance = 0f;

    [Header("Sinh vàng (only used if this TowerData's Is Gold Generator is checked)")]
    [Tooltip("Gold granted once when a wave ends, at this level. Ignored for normal combat towers.")]
    public float goldPerRound = 0f;
}

/// <summary>
/// Defines one tower type (e.g. Archer, Cannon, Mage). Create instances via
/// Assets > Create > Tower Defense > Tower Data, or use the
/// "Tower Defense > Create Default Tower & Enemy Data" editor menu for ready-made examples.
/// </summary>
[CreateAssetMenu(fileName = "NewTowerData", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("Identity")]
    public string towerName = "New Tower";
    [TextArea] public string description;

    [Header("Prefabs")]
    [Tooltip("The tower GameObject that gets instantiated when built")]
    public GameObject towerPrefab;
    [Tooltip("The projectile GameObject fired by this tower")]
    public GameObject projectilePrefab;

    [Header("Economy")]
    [Tooltip("Gold cost to build this tower for the first time")]
    public int buildCost = 100;

    [Header("Gold Generator")]
    [Tooltip("If checked, this tower does NOT attack at all - instead it silently sits on the map and " +
             "grants gold once, every time a wave fully ends (see 'Gold Per Round' on each level below). " +
             "Still uses the same Levels/upgrade system as combat towers.")]
    public bool isGoldGenerator = false;

    [Header("Placement")]
    [Tooltip("Vertical offset applied when this tower is instantiated. Use this if the prefab's " +
             "pivot sits at the center of the mesh instead of its base (e.g. an unmodified " +
             "primitive like a Cylinder) - the tower will otherwise spawn half-buried in the ground. " +
             "Positive values raise it up. Try half the model's visible height as a starting point.")]
    public float placementYOffset = 0f;

    [Header("Levels")]
    [Tooltip("Index 0 = Level 1 stats, Index 1 = Level 2 stats, Index 2 = Level 3 stats. " +
             "Add/remove entries to change the max level (default is 3, as requested).")]
    public TowerLevelStats[] levels = new TowerLevelStats[3]
    {
        new TowerLevelStats(), new TowerLevelStats(), new TowerLevelStats()
    };
}