#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

public static class WorldEventAssetResolver
{
    private const string PreferredUserFolder = "Assets/GameData/Event/";
    private const string GeneratedDataFolder = "Assets/WorldEvents/";

    public static WorldEventData Resolve(WorldEventManager manager, WorldEventType type)
    {
        // The scene/runtime manager is the strongest source of truth.
        if (manager != null && manager.eventPool != null)
        {
            for (int i = 0; i < manager.eventPool.Count; i++)
            {
                WorldEventData data = manager.eventPool[i];
                if (data != null && data.eventType == type)
                    return data;
            }
        }

        List<WorldEventData> all = FindAll(type);
        if (all.Count == 0)
            return null;

        // Prefer the user's authored GameData folder over generated fallback data.
        for (int i = 0; i < all.Count; i++)
        {
            string path = AssetDatabase.GetAssetPath(all[i]);
            if (!string.IsNullOrEmpty(path) && path.StartsWith(PreferredUserFolder, System.StringComparison.OrdinalIgnoreCase))
                return all[i];
        }

        for (int i = 0; i < all.Count; i++)
        {
            string path = AssetDatabase.GetAssetPath(all[i]);
            if (!string.IsNullOrEmpty(path) && path.StartsWith(GeneratedDataFolder, System.StringComparison.OrdinalIgnoreCase))
                return all[i];
        }

        return all[0];
    }

    public static List<WorldEventData> FindAll(WorldEventType type)
    {
        List<WorldEventData> result = new List<WorldEventData>();
        string[] guids = AssetDatabase.FindAssets("t:WorldEventData");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            WorldEventData data = AssetDatabase.LoadAssetAtPath<WorldEventData>(path);
            if (data != null && data.eventType == type)
                result.Add(data);
        }

        return result;
    }

    public static string Describe(WorldEventData data)
    {
        return data == null ? "<missing>" : AssetDatabase.GetAssetPath(data);
    }
}
#endif
