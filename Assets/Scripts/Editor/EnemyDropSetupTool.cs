#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EnemyDropSetupTool
{
    private const string DatabasePath = "Assets/GameData/EnemyDropDatabase.asset";

    [MenuItem("Tower Defense/Drop/Setup + Migrate Enemy Drops")]
    public static void SetupAndMigrate()
    {
        EnemyDropDatabase database = AssetDatabase.LoadAssetAtPath<EnemyDropDatabase>(DatabasePath);
        if (database == null)
        {
            EnsureFolder("Assets/GameData");
            database = ScriptableObject.CreateInstance<EnemyDropDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            AssetDatabase.SaveAssets();
        }

        int added = MigrateMissingEntries(database);

        EnemyDropController controller = Object.FindAnyObjectByType<EnemyDropController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            GameObject go = new GameObject("EnemyDropController");
            Undo.RegisterCreatedObjectUndo(go, "Create EnemyDropController");
            controller = go.AddComponent<EnemyDropController>();
        }

        if (controller.dropDatabase == null)
            controller.dropDatabase = database;

        if (controller.diamondDropSystem == null)
            controller.diamondDropSystem = Object.FindAnyObjectByType<DiamondDropSystem>(FindObjectsInactive.Include);

        if (controller.relicManager == null)
            controller.relicManager = Object.FindAnyObjectByType<RelicManager>(FindObjectsInactive.Include);

        EditorUtility.SetDirty(database);
        EditorUtility.SetDirty(controller);
        if (controller.gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(controller.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);

        EditorUtility.DisplayDialog(
            "Enemy Drop System Ready",
            $"Central drop system is ready.\n\nDatabase: {DatabasePath}\nNew entries migrated: {added}\n\nExisting database entries were NOT overwritten.",
            "OK");
    }

    private static int MigrateMissingEntries(EnemyDropDatabase database)
    {
        if (database.entries == null)
            database.entries = new System.Collections.Generic.List<EnemyDropEntry>();

        string[] guids = AssetDatabase.FindAssets("t:EnemyData");
        int added = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (enemy == null || Contains(database, enemy))
                continue;

            EnemyDropEntry entry = new EnemyDropEntry
            {
                enemy = enemy,
                diamondDropChance = enemy.diamondDropChance,
                diamondDropMin = enemy.diamondDropMin,
                diamondDropMax = enemy.diamondDropMax,
                isBoss = enemy.isBoss,
                bossGuaranteedDiamonds = enemy.bossGuaranteedDiamonds,
                bossDiamondMin = enemy.bossDiamondMin,
                bossDiamondMax = enemy.bossDiamondMax,
                relicDropChance = enemy.relicDropChance,
                minimumDropRarity = enemy.minimumDropRarity,
                bossGuaranteedRelic = enemy.bossGuaranteedRelic,
                bossGuaranteedMinimumRarity = enemy.bossGuaranteedMinimumRarity
            };

            database.entries.Add(entry);
            added++;
        }

        return added;
    }

    private static bool Contains(EnemyDropDatabase database, EnemyData enemy)
    {
        if (database.entries == null)
            return false;

        for (int i = 0; i < database.entries.Count; i++)
        {
            EnemyDropEntry entry = database.entries[i];
            if (entry != null && entry.enemy == enemy)
                return true;
        }

        return false;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
