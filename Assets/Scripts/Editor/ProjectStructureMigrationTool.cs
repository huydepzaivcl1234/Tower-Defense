#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase-1 project cleanup only. Uses AssetDatabase APIs so Unity keeps .meta GUIDs and references.
/// Does NOT change namespaces and deliberately skips ambiguous / third-party folders.
/// </summary>
public static class ProjectStructureMigrationTool
{
    private struct MoveRule
    {
        public string source;
        public string destination;
        public string reason;

        public MoveRule(string source, string destination, string reason)
        {
            this.source = source;
            this.destination = destination;
            this.reason = reason;
        }
    }

    private static readonly string[] TargetFolders =
    {
        "Assets/Scripts/Enemy",
        "Assets/Scripts/Tower",
        "Assets/Scripts/Projectile",
        "Assets/Scripts/Core",
        "Assets/Scripts/UI",
        "Assets/Scripts/Systems",
        "Assets/Scripts/Data",
        "Assets/Art",
        "Assets/Art/Models",
        "Assets/Art/Models/Enemies",
        "Assets/Art/Models/Towers",
        "Assets/Art/Models/Core",
        "Assets/Art/Models/Environment",
        "Assets/Art/Materials",
        "Assets/Art/Textures",
        "Assets/Art/Animations",
        "Assets/Prefabs",
        "Assets/Prefabs/Enemies",
        "Assets/Prefabs/Towers",
        "Assets/Prefabs/Core",
        "Assets/Prefabs/UI",
        "Assets/Audio",
        "Assets/Scenes",
        "Assets/Settings"
    };

    // Only folders with a clear one-to-one mapping are included here.
    // Ambiguous content such as DialogueEditor, Asset game, FX, Skybox, GameData and portal gun
    // is intentionally not moved by automation.
    private static readonly MoveRule[] SafeMoves =
    {
        new MoveRule("Assets/Animation", "Assets/Art/Animations", "Project animation assets -> target Art/Animations"),
        new MoveRule("Assets/EnemiesPrefab", "Assets/Prefabs/Enemies", "Enemy prefabs -> target Prefabs/Enemies"),
        new MoveRule("Assets/TowerPrefabs", "Assets/Prefabs/Towers", "Tower prefabs -> target Prefabs/Towers"),
        new MoveRule("Assets/Damepopup Prefabs", "Assets/Prefabs/UI/DamagePopups", "Damage popup UI prefabs -> target Prefabs/UI"),
        new MoveRule("Assets/Models", "Assets/Art/Models", "Project models -> target Art/Models"),
        new MoveRule("Assets/SFX", "Assets/Audio/SFX", "Project SFX -> target Audio/SFX")
    };

    [MenuItem("Tower Defense/Project Cleanup/Preview Safe Migration")]
    public static void PreviewSafeMigration()
    {
        string report = BuildPreviewReport();
        Debug.Log(report);
        EditorUtility.DisplayDialog(
            "Project Cleanup Preview",
            "Preview written to Console. No assets were moved.\n\n" +
            "The tool only includes clear project-owned mappings and skips ambiguous/vendor folders.",
            "OK");
    }

    [MenuItem("Tower Defense/Project Cleanup/Scan Resources.Load Paths")]
    public static void ScanResourcesLoadPaths()
    {
        List<string> hits = FindResourcesLoadScripts();
        if (hits.Count == 0)
        {
            Debug.Log("Project Cleanup: no Resources.Load(...) usage found in project MonoScripts.");
            EditorUtility.DisplayDialog("Resources.Load Scan", "No Resources.Load(...) usage found.", "OK");
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Resources.Load usage found. Review these BEFORE moving anything under a Resources folder:");
        for (int i = 0; i < hits.Count; i++)
            builder.AppendLine("• " + hits[i]);

        Debug.LogWarning(builder.ToString());
        EditorUtility.DisplayDialog(
            "Resources.Load Scan",
            $"Found {hits.Count} script(s). See Console before moving Resources content.",
            "OK");
    }

    [MenuItem("Tower Defense/Project Cleanup/Run Safe Migration")]
    public static void RunSafeMigration()
    {
        List<string> resourceHits = FindResourcesLoadScripts();
        if (resourceHits.Count > 0)
        {
            StringBuilder warning = new StringBuilder();
            warning.AppendLine("Resources.Load usage exists in this project.");
            warning.AppendLine("This safe phase does not intentionally move Resources folders, but review the Console scan first.");
            warning.AppendLine();
            warning.AppendLine("Continue with only the listed non-Resources moves?");

            if (!EditorUtility.DisplayDialog("Resources.Load Warning", warning.ToString(), "Continue Safe Moves", "Cancel"))
                return;
        }

        if (!EditorUtility.DisplayDialog(
                "Run Safe Project Migration",
                "This will create the target folders and move only the clear mappings shown by Preview.\n\n" +
                "Moves use AssetDatabase.MoveAsset so Unity preserves .meta GUID references.\n" +
                "Namespaces are NOT changed in this phase.\n" +
                "Ambiguous and third-party folders are NOT moved.\n\nContinue?",
                "Run Migration",
                "Cancel"))
            return;

        EnsureTargetFolders();

        int moved = 0;
        int skipped = 0;
        int failed = 0;

        for (int i = 0; i < SafeMoves.Length; i++)
        {
            MoveRule rule = SafeMoves[i];

            if (!AssetDatabase.IsValidFolder(rule.source))
            {
                skipped++;
                continue;
            }

            if (AssetDatabase.IsValidFolder(rule.destination) || AssetDatabase.LoadMainAssetAtPath(rule.destination) != null)
            {
                Debug.LogWarning($"Project Cleanup skipped '{rule.source}' because destination already exists: '{rule.destination}'. Move/merge this one manually in Unity Project window.");
                skipped++;
                continue;
            }

            EnsureParentFolder(rule.destination);

            string error = AssetDatabase.MoveAsset(rule.source, rule.destination);
            if (string.IsNullOrEmpty(error))
            {
                moved++;
                Debug.Log($"Project Cleanup moved: {rule.source} -> {rule.destination}");
            }
            else
            {
                failed++;
                Debug.LogError($"Project Cleanup FAILED: {rule.source} -> {rule.destination}\n{error}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Safe Migration Finished",
            $"Moved: {moved}\nSkipped: {skipped}\nFailed: {failed}\n\n" +
            "Next: let Unity finish importing, verify Console = 0 errors, open SampleScene and check prefab/material references. " +
            "Do not start namespace migration until this asset phase is verified.",
            "OK");
    }

    private static string BuildPreviewReport()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("===== TOWER DEFENSE PROJECT CLEANUP PREVIEW =====");
        report.AppendLine("No files are changed by Preview.");
        report.AppendLine();

        for (int i = 0; i < SafeMoves.Length; i++)
        {
            MoveRule rule = SafeMoves[i];
            bool sourceExists = AssetDatabase.IsValidFolder(rule.source);
            bool destinationExists = AssetDatabase.IsValidFolder(rule.destination) || AssetDatabase.LoadMainAssetAtPath(rule.destination) != null;

            report.Append(sourceExists ? "[READY] " : "[MISSING] ");
            report.Append(rule.source);
            report.Append(" -> ");
            report.Append(rule.destination);
            if (destinationExists)
                report.Append("  [DESTINATION EXISTS: manual merge required]");
            report.AppendLine();
            report.AppendLine("         " + rule.reason);
        }

        report.AppendLine();
        report.AppendLine("Intentionally skipped from automatic migration:");
        report.AppendLine("• Assets/DialogueEditor (third-party/plugin-style asset)");
        report.AppendLine("• Assets/Asset game (vendor content / ambiguous ownership)");
        report.AppendLine("• Assets/FX (classification needed: Art vs Prefabs vs VFX package)");
        report.AppendLine("• Assets/Skybox (classification needed)");
        report.AppendLine("• Assets/GameData (ScriptableObject content; not the same thing as Scripts/Data)");
        report.AppendLine("• Assets/portal gun and other imported/vendor packs");
        report.AppendLine("• C# namespaces (separate phase after asset verification)");

        List<string> resourceHits = FindResourcesLoadScripts();
        report.AppendLine();
        report.AppendLine(resourceHits.Count == 0
            ? "Resources.Load scan: no usage found."
            : $"Resources.Load scan: {resourceHits.Count} script(s) found; review before any Resources move.");

        return report.ToString();
    }

    private static List<string> FindResourcesLoadScripts()
    {
        List<string> hits = new List<string>();
        string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });

        for (int i = 0; i < scriptGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script == null || string.IsNullOrEmpty(script.text))
                continue;

            if (script.text.Contains("Resources.Load(" ) || script.text.Contains("Resources.Load<"))
                hits.Add(path);
        }

        return hits;
    }

    private static void EnsureTargetFolders()
    {
        for (int i = 0; i < TargetFolders.Length; i++)
            EnsureFolder(TargetFolders[i]);
    }

    private static void EnsureParentFolder(string assetPath)
    {
        int slash = assetPath.LastIndexOf('/');
        if (slash <= 0)
            return;
        EnsureFolder(assetPath.Substring(0, slash));
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || folderPath == "Assets" || AssetDatabase.IsValidFolder(folderPath))
            return;

        int slash = folderPath.LastIndexOf('/');
        if (slash <= 0)
            return;

        string parent = folderPath.Substring(0, slash);
        string name = folderPath.Substring(slash + 1);
        EnsureFolder(parent);

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
