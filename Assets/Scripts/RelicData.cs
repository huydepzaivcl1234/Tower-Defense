using UnityEngine;

public enum RelicEffectType
{
    TowerDamagePercent,
    TowerAttackSpeedPercent,
    TowerRangePercent,
    GoldGainPercent,
    BuildCostDiscountPercent,
    UpgradeCostDiscountPercent
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
    [Tooltip("Percent written as a fraction. 0.10 = +10%, 0.05 = +5% discount.")]
    public float value = 0.10f;
}

/// <summary>
/// One permanent-for-this-run roguelite buff. Create with:
/// Assets > Create > Tower Defense > Relic Data.
/// A relic can contain one or multiple modifiers and can be stacked up to Max Stacks.
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
