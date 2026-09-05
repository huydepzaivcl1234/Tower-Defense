#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates only the first playable map layer: a flat build plane and a functional winding enemy path.
/// It reuses WaypointPath, WaveManager and TowerPlacementManager instead of creating competing systems.
/// No houses, trees, rocks, cliffs or decorative structures are created.
/// </summary>
public static class FlatMapConceptSetupTool
{
    private const string RootName = "TD_FlatMapConcept";
    private const string MaterialFolder = "Assets/Art/Materials";
    private const string GroundMaterialPath = MaterialFolder + "/MapGround_Concept.mat";
    private const string PathMaterialPath = MaterialFolder + "/MapPath_Concept.mat";

    private static readonly Vector3[] DefaultPathPoints =
    {
        new Vector3(-35f, 0f,  8f),
        new Vector3(-31f, 0f, 10f),
        new Vector3(-26f, 0f, 10f),
        new Vector3(-22f, 0f,  8f),
        new Vector3(-19f, 0f,  4f),
        new Vector3(-18f, 0f, -1f),
        new Vector3(-15f, 0f, -5f),
        new Vector3(-10f, 0f, -7f),
        new Vector3( -5f, 0f, -6f),
        new Vector3( -1f, 0f, -3f),
        new Vector3(  1f, 0f,  2f),
        new Vector3(  4f, 0f,  7f),
        new Vector3(  9f, 0f,  9f),
        new Vector3( 14f, 0f,  8f),
        new Vector3( 18f, 0f,  4f),
        new Vector3( 19f, 0f, -1f),
        new Vector3( 21f, 0f, -6f),
        new Vector3( 25f, 0f, -9f),
        new Vector3( 30f, 0f, -9f),
        new Vector3( 34f, 0f, -6f),
        new Vector3( 35f, 0f, -2f)
    };

    [MenuItem("Tower Defense/Map/Create Flat Concept Map + Enemy Path")]
    public static void Create()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorUtility.DisplayDialog(
                "Flat Map Concept",
                "TD_FlatMapConcept already exists. Nothing was rebuilt or overwritten.\n\nMove the waypoint children to customize the route, then use Rebuild Flat Map Path Surface.",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("Create Flat Tower Defense Map");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create flat map root");

        GameObject ground = CreateGround(root.transform);
        WaypointPath path = CreatePath(root.transform);
        FlatMapPathSurface surface = CreatePathSurface(root.transform, path);

        Material groundMat = GetOrCreateMaterial(GroundMaterialPath, new Color(0.24f, 0.47f, 0.19f, 1f), 0f, 0.18f);
        Material pathMat = GetOrCreateMaterial(PathMaterialPath, new Color(0.49f, 0.31f, 0.15f, 1f), 0f, 0.12f);

        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null && groundRenderer.sharedMaterial == null)
            groundRenderer.sharedMaterial = groundMat;

        MeshRenderer pathRenderer = surface.GetComponent<MeshRenderer>();
        if (pathRenderer != null && pathRenderer.sharedMaterial == null)
            pathRenderer.sharedMaterial = pathMat;

        WireGameplay(path, ground);
        surface.Rebuild();

        EditorUtility.SetDirty(path);
        EditorUtility.SetDirty(surface);
        EditorUtility.SetDirty(root);

        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Flat Map Concept Ready",
            "Created:\n" +
            "- 80 x 45 flat build plane\n" +
            "- functional winding WaypointPath\n" +
            "- visible 5m road surface\n" +
            "- Spawn at the first waypoint and Goal at the last waypoint\n" +
            "- WaveManager/TowerPlacementManager path references wired when found\n\n" +
            "No houses, trees, rocks or outside structures were created.\n" +
            "The scene was marked dirty only; it was NOT auto-saved.",
            "OK");
    }

    [MenuItem("Tower Defense/Map/Rebuild Flat Map Path Surface")]
    public static void RebuildSurface()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Flat Map Concept", "TD_FlatMapConcept was not found.", "OK");
            return;
        }

        FlatMapPathSurface surface = root.GetComponentInChildren<FlatMapPathSurface>(true);
        WaypointPath path = root.GetComponentInChildren<WaypointPath>(true);
        if (surface == null || path == null)
        {
            EditorUtility.DisplayDialog("Flat Map Concept", "Generated path components are missing. Nothing was changed.", "OK");
            return;
        }

        Undo.RecordObject(surface, "Rebuild map path surface");
        surface.path = path;
        surface.Rebuild();
        EditorUtility.SetDirty(surface);
        Selection.activeGameObject = surface.gameObject;
    }

    private static GameObject CreateGround(Transform parent)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(ground, "Create map ground");
        ground.name = "Ground_FlatPlayableArea";
        ground.transform.SetParent(parent, false);
        ground.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(80f, 1f, 45f);

        TowerPlacementManager placement = Object.FindAnyObjectByType<TowerPlacementManager>(FindObjectsInactive.Include);
        if (placement != null && placement.groundLayerMask.value != 0)
        {
            int layer = FirstLayerInMask(placement.groundLayerMask.value);
            if (layer >= 0)
                ground.layer = layer;
        }

        return ground;
    }

    private static WaypointPath CreatePath(Transform parent)
    {
        GameObject pathObject = new GameObject("EnemyPath");
        Undo.RegisterCreatedObjectUndo(pathObject, "Create enemy path");
        pathObject.transform.SetParent(parent, false);

        WaypointPath path = Undo.AddComponent<WaypointPath>(pathObject);
        path.pathExclusionRadius = 3.25f;
        path.waypoints = new List<Transform>();

        for (int i = 0; i < DefaultPathPoints.Length; i++)
        {
            GameObject point = new GameObject(i == 0 ? "WP_00_SPAWN" :
                                              i == DefaultPathPoints.Length - 1 ? $"WP_{i:00}_GOAL" :
                                              $"WP_{i:00}");
            Undo.RegisterCreatedObjectUndo(point, "Create path waypoint");
            point.transform.SetParent(pathObject.transform, false);
            point.transform.localPosition = DefaultPathPoints[i];
            path.waypoints.Add(point.transform);
        }

        return path;
    }

    private static FlatMapPathSurface CreatePathSurface(Transform parent, WaypointPath path)
    {
        GameObject road = new GameObject("EnemyPath_Surface");
        Undo.RegisterCreatedObjectUndo(road, "Create enemy path surface");
        road.transform.SetParent(parent, false);

        FlatMapPathSurface surface = Undo.AddComponent<FlatMapPathSurface>(road);
        surface.path = path;
        surface.pathWidth = 5f;
        surface.surfaceYOffset = 0.035f;
        return surface;
    }

    private static void WireGameplay(WaypointPath path, GameObject ground)
    {
        WaveManager waveManager = Object.FindAnyObjectByType<WaveManager>(FindObjectsInactive.Include);
        if (waveManager != null)
        {
            Undo.RecordObject(waveManager, "Assign concept map path");
            waveManager.path = path;
            EditorUtility.SetDirty(waveManager);
        }

        TowerPlacementManager placement = Object.FindAnyObjectByType<TowerPlacementManager>(FindObjectsInactive.Include);
        if (placement != null)
        {
            Undo.RecordObject(placement, "Assign concept map path");
            placement.path = path;

            if (placement.groundLayerMask.value == 0)
                placement.groundLayerMask = 1 << ground.layer;

            EditorUtility.SetDirty(placement);
        }
    }

    private static Material GetOrCreateMaterial(string path, Color color, float metallic, float smoothness)
    {
        EnsureFolder("Assets/Art");
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

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);

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

    private static int FirstLayerInMask(int mask)
    {
        for (int i = 0; i < 32; i++)
            if ((mask & (1 << i)) != 0)
                return i;
        return -1;
    }
}
#endif
