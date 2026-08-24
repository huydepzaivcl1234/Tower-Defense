#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the advanced relic set requested for Common / Uncommon / Rare.
/// Defaults are only written when an asset is created for the first time, so rerunning this tool
/// never overwrites balance changes made later in the Inspector.
/// </summary>
public static class AdvancedRelicSetupTool
{
    private const string RelicRoot = "Assets/GameData/Relics";
    private const string CannonPath = "Assets/GameData/Towers/Cannon Tower.asset";

    [MenuItem("Tower Defense/Relics/Setup Advanced Relics")]
    public static void SetupAdvancedRelics()
    {
        EnsureFolder(RelicRoot);
        TowerData cannon = AssetDatabase.LoadAssetAtPath<TowerData>(CannonPath);

        List<RelicData> createdOrLoaded = new List<RelicData>
        {
            EnsureRelic(
                "INeedMoreHealth", "I Need More Health",
                "Gain +5 Lives immediately. Stacks up to 10 times.",
                RelicRarity.Common, 10,
                new RelicModifier { effect = RelicEffectType.AddLivesFlat, value = 5f }),

            EnsureRelic(
                "IFeelingLucky", "I Feeling Lucky",
                "+1% relic-card drop chance from every enemy per stack.",
                RelicRarity.Common, 100,
                new RelicModifier { effect = RelicEffectType.RelicDropChanceFlat, value = 0.01f }),

            EnsureRelic(
                "MoreBiggerMoreStronger", "More Bigger More Stronger",
                "For every 10 current Lives, all towers gain +1 flat damage per stack.",
                RelicRarity.Common, 100,
                new RelicModifier { effect = RelicEffectType.DamagePerLives, value = 1f, value2 = 10f }),

            EnsureRelic(
                "GuessWhat", "Guess What",
                "Each stack adds 0.5% critical chance. A critical hit deals +150% extra damage.",
                RelicRarity.Uncommon, 50,
                new RelicModifier { effect = RelicEffectType.CriticalChance, value = 0.005f, value2 = 1.50f }),

            EnsureRelic(
                "DodgeThis", "Dodge This",
                "+1% projectile flight speed per stack.",
                RelicRarity.Uncommon, 100,
                new RelicModifier { effect = RelicEffectType.ProjectileSpeedPercent, value = 0.01f }),

            EnsureRelic(
                "CannonHero", "Cannon Hero",
                "Destroy existing Cannon towers (Big Cannon excluded). You may buy only one Cannon for the rest of the run. That Cannon gains +10 range, +100 damage per level, and +10% damage per distance step travelled up to +50%.",
                RelicRarity.Uncommon, 1,
                new RelicModifier
                {
                    effect = RelicEffectType.CannonHero,
                    targetTower = cannon,
                    value = 10f,       // flat range
                    value2 = 100f,     // flat damage per tower level
                    value3 = 0.10f,    // damage % per travel step
                    value4 = 0.50f,    // travel bonus cap
                    value5 = 1f        // world units per travel step (data exposed for balancing)
                }),

            EnsureRelic(
                "NewBorn", "New Born?",
                "Enemies have a scaling chance to spawn wounded. Starts at 1% chance / 75% HP and reaches 25% chance / 50% HP at max stacks.",
                RelicRarity.Rare, 25,
                new RelicModifier
                {
                    effect = RelicEffectType.EnemySpawnWeakness,
                    value = 0.01f,     // start chance
                    value2 = 0.25f,    // max chance
                    value3 = 0.75f,    // start HP fraction
                    value4 = 0.50f     // max HP fraction
                })
        };

        RelicManager manager = Object.FindFirstObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (manager != null)
        {
            Undo.RecordObject(manager, "Add Advanced Relics");
            if (manager.relicPool == null) manager.relicPool = new List<RelicData>();
            foreach (RelicData relic in createdOrLoaded)
            {
                if (relic != null && !manager.relicPool.Contains(relic))
                    manager.relicPool.Add(relic);
            }
            EditorUtility.SetDirty(manager);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string cannonStatus = cannon != null
            ? "Cannon Hero target linked to Cannon Tower."
            : "WARNING: Cannon Tower asset was not found. Assign Cannon Hero > Modifiers[0] > Target Tower manually.";

        EditorUtility.DisplayDialog(
            "Advanced Relics",
            "7 relics are ready in Assets/GameData/Relics.\n\n" + cannonStatus +
            "\n\nExisting relic assets were NOT overwritten, so all Inspector balance edits are preserved.",
            "OK");
    }

    private static RelicData EnsureRelic(
        string fileName,
        string displayName,
        string description,
        RelicRarity rarity,
        int maxStacks,
        RelicModifier modifier)
    {
        string path = $"{RelicRoot}/{fileName}.asset";
        RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);
        if (relic != null) return relic;

        relic = ScriptableObject.CreateInstance<RelicData>();
        relic.relicName = displayName;
        relic.description = description;
        relic.rarity = rarity;
        relic.selectionWeight = rarity == RelicRarity.Common ? 1f : rarity == RelicRarity.Uncommon ? 0.65f : 0.35f;
        relic.maxStacks = maxStacks;
        relic.modifiers = new[] { modifier };
        AssetDatabase.CreateAsset(relic, path);
        EditorUtility.SetDirty(relic);
        return relic;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
