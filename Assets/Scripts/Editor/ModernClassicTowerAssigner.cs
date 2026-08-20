#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Explicit opt-in helper: assigns generated modern-classic prefabs to matching TowerData assets.
/// It does not touch stats, projectile prefabs, costs, effects, or other gameplay data.
/// </summary>
public static class ModernClassicTowerAssigner
{
    private const string Root = "Assets/TowerPrefabs/GeneratedModernClassic";

    [MenuItem("Tower Defense/Models/Assign Generated Models To TowerData")]
    public static void Assign()
    {
        var prefabMap = new Dictionary<string, string>
        {
            { "archer", Root + "/Archer Tower.prefab" },
            { "xbow", Root + "/Xbow Tower.prefab" },
            { "canon", Root + "/Canon Tower.prefab" },
            { "cannon", Root + "/Canon Tower.prefab" },
            { "big cannon", Root + "/Big Cannon.prefab" },
            { "bomb", Root + "/Bomb Tower.prefab" },
            { "burning", Root + "/Burning Tower.prefab" },
            { "ultimate", Root + "/Ultimate Tower.prefab" },
            { "gold mine", Root + "/Gold Mine.prefab" },
        };

        int changed = 0;
        string[] guids = AssetDatabase.FindAssets("t:TowerData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(path);
            if (data == null) continue;

            string key = Normalize(data.towerName);
            string prefabPath = null;

            if (prefabMap.TryGetValue(key, out string direct)) prefabPath = direct;
            else
            {
                foreach (var kv in prefabMap)
                {
                    if (key.Contains(kv.Key)) { prefabPath = kv.Value; break; }
                }
            }

            if (string.IsNullOrEmpty(prefabPath)) continue;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            Undo.RecordObject(data, "Assign Modern Classic Tower Model");
            data.towerPrefab = prefab;
            // Generated base extends slightly below root because of the stylized foundation.
            // Raise it just enough so free-placement does not visually sink it into the ground.
            data.placementYOffset = 0.3f;
            EditorUtility.SetDirty(data);
            changed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Assign Tower Models",
            "Đã gắn model mới cho " + changed + " TowerData asset.\n\n" +
            "Không thay đổi Damage / Attack Speed / Range / Cost / Projectile.\n" +
            "Nếu TowerData chỉ có 3 level thì gameplay hiện chỉ dùng Phase 1-3; Phase 4 đã có sẵn trong prefab.", "OK");
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        return value.Trim().ToLowerInvariant().Replace("tower", "").Trim();
    }
}
#endif
