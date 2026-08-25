#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MainBaseModelSetupTool
{
    private const string ModelPath = "Assets/Models/MainBase/MainBase.fbx";
    private const string MaterialsFolder = "Assets/Models/MainBase/Materials";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/MainBase.prefab";

    [MenuItem("Tower Defense/Art/Setup Main Base Model")]
    public static void SetupMainBase()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            EditorUtility.DisplayDialog("Main Base FBX Not Found",
                "Run Assets/Art/Blender/MainBaseGenerator.py in Blender first.\n\nIt exports the model to:\n" + ModelPath,
                "OK");
            return;
        }

        EnsureFolder(MaterialsFolder);
        EnsureFolder(PrefabFolder);

        Material stone = GetOrCreateMaterial("MainBase_Stone", new Color(0.46f, 0.51f, 0.56f, 1f), 0.0f, 0.28f, Color.black);
        Material gold = GetOrCreateMaterial("MainBase_Gold", new Color(0.47f, 0.26f, 0.065f, 1f), 0.92f, 0.74f, Color.black);
        Material crystal = GetOrCreateMaterial("MainBase_Crystal", new Color(0.02f, 0.42f, 0.80f, 1f), 0.0f, 0.90f, new Color(0.0f, 0.68f, 1.0f) * 3.5f);
        Material banner = GetOrCreateMaterial("MainBase_Banner", new Color(0.018f, 0.055f, 0.14f, 1f), 0.0f, 0.34f, Color.black);
        Material moss = GetOrCreateMaterial("MainBase_Moss", new Color(0.07f, 0.19f, 0.075f, 1f), 0.0f, 0.12f, Color.black);
        Material portal = GetOrCreateMaterial("MainBase_Portal", new Color(0.008f, 0.08f, 0.22f, 1f), 0.0f, 0.86f, new Color(0.0f, 0.45f, 1.0f) * 5.0f);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (instance == null) instance = Object.Instantiate(model);
        instance.name = "MainBase";
        Undo.RegisterCreatedObjectUndo(instance, "Create Main Base");

        BuildExclusionZone exclusion = instance.GetComponent<BuildExclusionZone>();
        if (exclusion == null) exclusion = Undo.AddComponent<BuildExclusionZone>(instance);
        exclusion.radius = 7.5f;
        exclusion.centerOffset = Vector3.zero;

        Dictionary<string, Material> map = new Dictionary<string, Material>
        {
            { "MAT_Stone", stone }, { "MAT_Gold", gold }, { "MAT_Crystal", crystal },
            { "MAT_Banner", banner }, { "MAT_Moss", moss }, { "MAT_Portal", portal }
        };
        RemapRendererMaterials(instance, map);
        PlaceAtPathGoal(instance);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        if (prefab == null) Debug.LogWarning("MainBase prefab could not be saved, but the scene instance was created.");

        Selection.activeGameObject = instance;
        EditorUtility.SetDirty(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Main Base Ready",
            "MainBase was configured at the final waypoint.\n\nTower placement is blocked inside a 7.5m radius around the base. You can change this on BuildExclusionZone.\n\nPrefab: " + PrefabPath,
            "OK");
    }

    private static void PlaceAtPathGoal(GameObject instance)
    {
        WaypointPath path = Object.FindAnyObjectByType<WaypointPath>(FindObjectsInactive.Include);
        if (path == null) { instance.transform.position = Vector3.zero; return; }
        List<Transform> points = path.GetWaypoints();
        if (points == null || points.Count == 0 || points[points.Count - 1] == null) { instance.transform.position = Vector3.zero; return; }

        Transform goal = points[points.Count - 1];
        instance.transform.position = goal.position;
        if (points.Count >= 2 && points[points.Count - 2] != null)
        {
            Vector3 incoming = points[points.Count - 2].position - goal.position;
            incoming.y = 0f;
            if (incoming.sqrMagnitude > 0.001f) instance.transform.rotation = Quaternion.LookRotation(incoming.normalized, Vector3.up);
        }
    }

    private static void RemapRendererMaterials(GameObject root, Dictionary<string, Material> map)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] current = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < current.Length; i++)
            {
                string sourceName = current[i] != null ? current[i].name : string.Empty;
                foreach (KeyValuePair<string, Material> pair in map)
                {
                    if (!sourceName.Contains(pair.Key)) continue;
                    current[i] = pair.Value;
                    changed = true;
                    break;
                }
            }
            if (changed) renderer.sharedMaterials = current;
        }
    }

    private static Material GetOrCreateMaterial(string fileName, Color baseColor, float metallic, float smoothness, Color emission)
    {
        string path = MaterialsFolder + "/" + fileName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader) { name = fileName };
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        if (emission.maxColorComponent > 0.001f)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void EnsureFolder(string fullPath)
    {
        string[] parts = fullPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif

