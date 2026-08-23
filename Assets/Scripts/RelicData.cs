using UnityEngine;

public enum RelicEffectType
{
    TowerDamagePercent,
    TowerAttackSpeedPercent,
    TowerRangePercent,
    GoldGainPercent,
    BuildCostDiscountPercent,
    UpgradeCostDiscountPercent,

    // Advanced fully-customizable relic effects.
    AddLivesFlat,
    RelicDropChanceFlat,
    DamagePerLives,
    CriticalChance,
    ProjectileSpeedPercent,
    CannonHero,
    EnemySpawnWeakness
}

public enum RelicRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4
}

[System.Serializable]
public class RelicModifier
{
    public RelicEffectType effect;

    [Tooltip("Primary value. Percentages are fractions: 0.10 = 10%. Meaning depends on Effect.")]
    public float value = 0.10f;

    [Tooltip("Secondary customizable value used by advanced effects.")]
    public float value2 = 0f;

    [Tooltip("Third customizable value used by advanced effects.")]
    public float value3 = 0f;

    [Tooltip("Fourth customizable value used by advanced effects.")]
    public float value4 = 0f;

    [Tooltip("Optional tower target for tower-specific relics such as Cannon Hero.")]
    public TowerData targetTower;
}

/// <summary>
/// One permanent-for-this-run roguelite buff. Create with:
/// Assets > Create > Tower Defense > Relic Data.
/// A relic can contain one or multiple modifiers and can be stacked up to Max Stacks.
/// Advanced effects use value/value2/value3/value4 so their balancing stays data-driven.
/// </summary>
[CreateAssetMenu(fileName = "NewRelic", menuName = "Tower Defense/Relic Data")]
public class RelicData : ScriptableObject
{
    [Header("Display")]
    public string relicName = "New Relic";
    [TextArea(2, 5)] public string description;
    public Sprite icon;
    public RelicRarity rarity = RelicRarity.Common;

    [Header("Random Selection")]
    [Min(0.01f)] public float selectionWeight = 1f;
    [Min(1)] public int maxStacks = 99;

    [Header("Permanent modifiers for this run")]
    public RelicModifier[] modifiers = new RelicModifier[1] { new RelicModifier() };
}
