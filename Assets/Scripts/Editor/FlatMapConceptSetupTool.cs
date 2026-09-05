#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Designs the EXISTING SampleScene Plane + Path.
/// Reuses the current Plane, WaypointPath, Portal and MainBase.
/// It never creates a parallel map root and never auto-saves the scene.
/// </summary>
public static class FlatMapConceptSetupTool
{
    private const string PlaneName = "Plane";
    private const string PathName = "Path";
    private const string PortalName = "Portal";
    private const string MainBaseName = "MainBase";

    private const string MaterialFolder = "Assets/Art/Materials";
    private const string GroundMaterialPath = MaterialFolder + "/ExistingMap_GroundPrototype.mat";
    private const string RoadMaterialPath = MaterialFolder + "/ExistingMap_PathPrototype.mat";
    private const string BorderMaterialPath = MaterialFolder + "/ExistingMap_PathBorderPrototype.mat";

    // XZ layout is authored around the current scene's real Portal/MainBase placement.
    // First/last points are replaced at runtime by the actual Portal/MainBase positions.
    private static readonly Vector3[] DesignedWorldPoints =
    {
        new Vector3( 28f, 0f, -29f), // Portal
        new Vector3( 27f, 0f, -22f),
        new Vector3( 23f, 0f, -16f),
        new Vector3( 16f, 0f, -13f),
        new Vector3(  9f, 0f, -13f),
        new Vector3(  5f, 0f,  -8f),
        new Vector3(  5f, 0f,  -1f),
        new Vector3( 10f, 0f,   5f),
        new Vector3( 17f, 0f,   9f),
        new Vector3( 20f, 0f,  15f),
        new Vector3( 16f, 0f,  20f),
        new Vector3(  8f, 0f,  23f),
        new Vector3( -1f, 0f,  22f),
        new Vector3( -8f, 0f,  18f),
        new Vector3(-11f, 0f,  11f),
        new Vector3( -9f, 0f,   4f),
        new Vector3( -4f, 0f,  -1f),
        new Vector3( -4f, 0f,  -8f),
        new Vector3( -9f, 0f, -13f),
        new Vector3(-16f, 0f, -15f),
        new Vector3(-22f, 0f, -20f),
        new Vector3(-27f, 0f, -30f)  // MainBase
    };

    [MenuItem("Tower Defense/Map/Design Existing Plane + Path")]
    public static void DesignExistingMap()
    {
        GameObject plane = GameObject.Find(PlaneName);
        GameObject pathObject = GameObject.Find(PathName);
        GameObject portal = GameObject.Find(PortalName);
        GameObject mainBase = GameObject.Find(MainBaseName);

        if (plane == null || pathObject == null)
        {
            EditorUtility.DisplayDialog(
                "Existing Map Designer",
                "Could not find the existing Plane and Path in the open scene. Nothing was changed.",
                "OK");
            return;
        }

        WaypointPath path = pathObject.GetComponent<WaypointPath>();
        if (path == null)
        {
            EditorUtility.DisplayDialog(
                "Existing Map Designer",
                "The existing Path does not have WaypointPath. Nothing was changed.",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("Design existing Tower Defense map path");
        int group = Undo.GetCurrentGroup();

        Undo.RegisterFullObjectHierarchyUndo(pathObject, "Design existing path");
        Undo.RecordObject(path, "Normalize existing waypoint path");
        Undo.RecordObject(plane.transform, "Keep existing map plane");

        float roadY = plane.transform.position.y + 0.04f;

        List<Transform> waypoints = EnsureWaypointCount(pathObject.transform, DesignedWorldPoints.Length);
        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform wp = waypoints[i];
            Undo.RecordObject(wp, "Move existing waypoint");

            Vector3 target = DesignedWorldPoints[i];
            if (i == 0 && portal != null)
            {
                target.x = portal.transform.position.x;
                target.z = portal.transform.position.z;
            }
            else if (i == waypoints.Count - 1 && mainBase != null)
            {
                target.x = mainBase.transform.position.x;
                target.z = mainBase.transform.position.z;
            }

            target.y = roadY;
            wp.position = target;
            wp.name = i == 0
                ? "WP_00_SPAWN"
                : i == waypoints.Count - 1
                    ? $"WP_{i:00}_GOAL"
                    : $"WP_{i:00}";
            wp.SetSiblingIndex(i);
        }

        // Replace the old duplicated serialized list with the exact existing child order.
        path.waypoints = new List<Transform>(waypoints);
        path.pathExclusionRadius = 3.0f;

        Material groundMat = GetOrCreateMaterial(
            GroundMaterialPath,
            new Color(0.23f, 0.43f, 0.18f, 1f),
            0f,
            0.16f);

        Material borderMat = GetOrCreateMaterial(
            BorderMaterialPath,
            new Color(0.20f, 0.14f, 0.09f, 1f),
            0f,
            0.10f);

        Material roadMat = GetOrCreateMaterial(
            RoadMaterialPath,
            new Color(0.55f, 0.37f, 0.19f, 1f),
            0f,
            0.14f);

        MeshRenderer planeRenderer = plane.GetComponent<MeshRenderer>();
        if (planeRenderer != null)
        {
            Undo.RecordObject(planeRenderer, "Apply map prototype ground material");
            planeRenderer.sharedMaterial = groundMat;
        }

        FlatMapPathSurface border = EnsureSurface(pathObject.transform, path, "Path_Border", 5.8f, 0.022f, borderMat);
        FlatMapPathSurface road = EnsureSurface(pathObject.transform, path, "Path_Surface", 4.8f, 0.04f, roadMat);

        border.Rebuild();
        road.Rebuild();

        WireExistingSystems(path);

        EditorUtility.SetDirty(path);
        EditorUtility.SetDirty(border);
        EditorUtility.SetDirty(road);
        if (planeRenderer != null) EditorUtility.SetDirty(planeRenderer);

        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = pathObject;

        EditorUtility.DisplayDialog(
            "Existing Map Designed",
            "Updated the EXISTING map only.\n\n" +
            "- reused Plane\n" +
            "- reused Path + existing waypoint children\n" +
            "- normalized duplicate waypoint references\n" +
            "- Spawn anchored to existing Portal\n" +
            "- Goal anchored to existing MainBase\n" +
            "- added a visible road + border under the existing Path\n" +
            "- WaveManager/TowerPlacementManager still use the same WaypointPath\n\n" +
            "No houses, trees, rocks or outside structures were created.\n" +
            "Scene was NOT auto-saved.",
            "OK");
    }

    [MenuItem("Tower Defense/Map/Rebuild Existing Path Surface")]
    public static void RebuildExistingPathSurface()
    {
        GameObject pathObject = GameObject.Find(PathName);
        if (pathObject == null)
        {
            EditorUtility.DisplayDialog("Existing Map Designer", "Existing Path was not found.", "OK");
            return;
        }

        WaypointPath path = pathObject.GetComponent<WaypointPath>();
        if (path == null)
        {
            EditorUtility.DisplayDialog("Existing Map Designer", "WaypointPath is missing on the existing Path.", "OK");
            return;
        }

        FlatMapPathSurface[] surfaces = pathObject.GetComponentsInChildren<FlatMapPathSurface>(true);
        if (surfaces == null || surfaces.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Existing Map Designer",
                "No path surface exists yet. Run Design Existing Plane + Path once first.",
                "OK");
            return;
        }

        foreach (FlatMapPathSurface surface in surfaces)
        {
            if (surface == null) continue;
            Undo.RecordObject(surface, "Rebuild existing path surface");
            surface.path = path;
            surface.Rebuild();
            EditorUtility.SetDirty(surface);
        }

        Selection.activeGameObject = pathObject;
    }

    private static List<Transform> EnsureWaypointCount(Transform pathRoot, int requiredCount)
    {
        List<Transform> result = new List<Transform>();

        // Reuse every existing non-surface child first.
        for (int i = 0; i < pathRoot.childCount; i++)
        {
            Transform child = pathRoot.GetChild(i);
            if (child == null) continue;
            if (child.GetComponent<FlatMapPathSurface>() != null) continue;
            if (child.name == "Path_Surface" || child.name == "Path_Border") continue;
            result.Add(child);
        }

        while (result.Count < requiredCount)
        {
            GameObject point = new GameObject($"WP_{result.Count:00}");
            Undo.RegisterCreatedObjectUndo(point, "Add waypoint to existing Path");
            point.transform.SetParent(pathRoot, true);
            result.Add(point.transform);
        }

        // If the existing path has more points than this design needs, keep them in the hierarchy
        // but do not delete them. Disable only from the serialized route by not adding them to the list.
        if (result.Count > requiredCount)
            result.RemoveRange(requiredCount, result.Count - requiredCount);

        return result;
    }

    private static FlatMapPathSurface EnsureSurface(
        Transform pathRoot,
        WaypointPath path,
        string objectName,
        float width,
        float yOffset,
        Material material)
    {
        Transform existing = pathRoot.Find(objectName);
        GameObject go;

        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(go, "Add surface to existing Path");
            go.transform.SetParent(pathRoot, false);
        }

        FlatMapPathSurface surface = go.GetComponent<FlatMapPathSurface>();
        if (surface == null)
            surface = Undo.AddComponent<FlatMapPathSurface>(go);

        Undo.RecordObject(surface, "Configure existing path surface");
        surface.path = path;
        surface.pathWidth = width;
        surface.surfaceYOffset = yOffset;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Assign existing path material");
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        return surface;
    }

    private static void WireExistingSystems(WaypointPath path)
    {
        WaveManager waveManager = Object.FindAnyObjectByType<WaveManager>(FindObjectsInactive.Include);
        if (waveManager != null && waveManager.path != path)
        {
            Undo.RecordObject(waveManager, "Keep WaveManager on existing path");
            waveManager.path = path;
            EditorUtility.SetDirty(waveManager);
        }

        TowerPlacementManager placement = Object.FindAnyObjectByType<TowerPlacementManager>(FindObjectsInactive.Include);
        if (placement != null && placement.path != path)
        {
            Undo.RecordObject(placement, "Keep TowerPlacementManager on existing path");
            placement.path = path;
            EditorUtility.SetDirty(placement);
        }
    }

    private static Material GetOrCreateMaterial(string path, Color color, float metallic, float smoothness)
    {
        EnsureFolder(MaterialFolder);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        material = new Material(shader)
        {
            name = System.IO.Path.GetFileNameWithoutExtension(path)
        };

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);

        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = System.IO.Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
