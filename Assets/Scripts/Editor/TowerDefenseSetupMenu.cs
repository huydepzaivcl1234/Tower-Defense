#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor-only helper. Adds "Tower Defense > Create Default Tower & Enemy Data" to the
/// Unity menu bar, which generates 3 ready-to-use TowerData assets and 3 EnemyData assets
/// with the balanced starting stats described in the README - so you don't have to
/// hand-create and fill in 6 ScriptableObjects yourself. Safe to run more than once;
/// it will not overwrite assets that already exist.
/// This file must stay inside a folder literally named "Editor" (Unity convention).
/// </summary>
public static class TowerDefenseSetupMenu
{
    private const string DataFolder = "Assets/GameData";

    [MenuItem("Tower Defense/Create Default Tower & Enemy Data")]
    public static void CreateDefaultData()
    {
        EnsureFolder(DataFolder);
        EnsureFolder(DataFolder + "/Towers");
        EnsureFolder(DataFolder + "/Enemies");

        CreateTower("Archer Tower", 100, new[]
        {
            new TowerLevelStats { strength = 8,  attackSpeed = 2.0f, range = 6f,   upgradeCost = 0 },
            new TowerLevelStats { strength = 14, attackSpeed = 2.3f, range = 6.5f, upgradeCost = 75 },
            new TowerLevelStats { strength = 22, attackSpeed = 2.6f, range = 7f,   upgradeCost = 125 },
        });

        CreateTower("Cannon Tower", 150, new[]
        {
            new TowerLevelStats { strength = 35, attackSpeed = 0.6f, range = 5f,   upgradeCost = 0 },
            new TowerLevelStats { strength = 55, attackSpeed = 0.7f, range = 5.5f, upgradeCost = 100 },
            new TowerLevelStats { strength = 85, attackSpeed = 0.8f, range = 6f,   upgradeCost = 175 },
        });

        CreateTower("Mage Tower", 175, new[]
        {
            new TowerLevelStats { strength = 15, attackSpeed = 1.2f, range = 8f,   upgradeCost = 0 },
            new TowerLevelStats { strength = 24, attackSpeed = 1.4f, range = 8.5f, upgradeCost = 110 },
            new TowerLevelStats { strength = 38, attackSpeed = 1.6f, range = 9.5f, upgradeCost = 190 },
        });

        CreateEnemy("Grunt", hp: 50, speed: 3.5f, regen: 0f, reward: 10, dmg: 1);
        CreateEnemy("Runner", hp: 30, speed: 6f, regen: 0f, reward: 8, dmg: 1);
        CreateEnemy("Brute", hp: 150, speed: 2f, regen: 3f, reward: 20, dmg: 2);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Tower Defense Setup",
            $"Created default Tower & Enemy data assets in {DataFolder}.\n\n" +
            "Open them in the Inspector any time to fully customize strength, attack speed, " +
            "range, HP, speed and HP regen.", "OK");
    }

    private static void CreateTower(string name, int cost, TowerLevelStats[] levels)
    {
        string path = $"{DataFolder}/Towers/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<TowerData>(path) != null) return;

        TowerData data = ScriptableObject.CreateInstance<TowerData>();
        data.towerName = name;
        data.buildCost = cost;
        data.levels = levels;
        data.placementYOffset = 1f; // matches half the height of an unscaled default Cylinder primitive
        AssetDatabase.CreateAsset(data, path);
    }

    private static void CreateEnemy(string name, float hp, float speed, float regen, int reward, int dmg)
    {
        string path = $"{DataFolder}/Enemies/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyData>(path) != null) return;

        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = name;
        data.maxHP = hp;
        data.moveSpeed = speed;
        data.hpRegenPerSec = regen;
        data.goldReward = reward;
        data.damageToPlayer = dmg;
        AssetDatabase.CreateAsset(data, path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif