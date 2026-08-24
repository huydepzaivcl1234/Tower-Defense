#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Rebuilds clean URP/Built-in compatible materials for the Blender generated tower FBXs.
/// Uses Blender material names, so FBX meshes retain the intended art palette.
/// </summary>
public static class GeneratedTowerMaterialSetup
{
    private const string Root = "Assets/TowerModels/Generated";
    private const string MaterialRoot = Root + "/Materials";

    [MenuItem("Tower Defense/Models/Apply Generated Tower Materials")]
    public static void ApplyMaterials()
    {
        EnsureFolder(Root);
        EnsureFolder(MaterialRoot);

        Material gunmetal = Make("M_Gunmetal",
            new Color(0.035f, 0.055f, 0.075f), 0.92f, 0.24f);

        Material ivory = Make("M_IvoryArmor",
            new Color(0.82f, 0.78f, 0.67f), 0.50f, 0.30f);

        Material bronze = Make("M_Bronze",
            new Color(0.50f, 0.23f, 0.05f), 0.90f, 0.23f);

        Material darkBronze = Make("M_DarkBronze",
            new Color(0.18f, 0.065f, 0.018f), 0.92f, 0.28f);

        Material wood = Make("M_DarkWood",
            new Color(0.12f, 0.035f, 0.012f), 0.0f, 0.50f);

        Material cyan = MakeEmission("M_CyanEnergy",
            new Color(0.0f, 0.55f, 0.90f),
            new Color(0.0f, 0.8f, 1.0f) * 4.0f);

        Material orange = MakeEmission("M_OrangeEnergy",
            new Color(0.85f, 0.22f, 0.02f),
            new Color(1.0f, 0.35f, 0.02f) * 4.0f);

        Material gold = Make("M_Gold",
            new Color(0.95f, 0.70f, 0.08f), 0.95f, 0.18f);

        Material glass = MakeEmission("M_GlassCyan",
            new Color(0.15f, 0.65f, 0.9f),
            new Color(0.05f, 0.55f, 1.0f) * 2.0f);

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { Root });
        int renderersChanged = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx")) continue;

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) continue;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] src = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < src.Length; i++)
                {
                    if (src[i] == null) continue;
                    string n = src[i].name;

                    Material replacement = null;
                    if (n.Contains("M_Gunmetal")) replacement = gunmetal;
                    else if (n.Contains("M_IvoryArmor")) replacement = ivory;
                    else if (n.Contains("M_DarkBronze")) replacement = darkBronze;
                    else if (n.Contains("M_Bronze")) replacement = bronze;
                    else if (n.Contains("M_DarkWood")) replacement = wood;
                    else if (n.Contains("M_CyanEnergy")) replacement = cyan;
                    else if (n.Contains("M_OrangeEnergy")) replacement = orange;
                    else if (n.Contains("M_Gold")) replacement = gold;
                    else if (n.Contains("M_GlassCyan")) replacement = glass;

                    if (replacement != null && src[i] != replacement)
                    {
                        src[i] = replacement;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = src;
                    renderersChanged++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Tower Materials",
            "Đã tạo material màu chuẩn trong:\n" + MaterialRoot +
            "\n\nRenderer cập nhật: " + renderersChanged +
            "\n\nNếu dùng URP, cyan/orange sẽ dùng emission để tower có sức sống hơn.",
            "OK"
        );
    }

    private static Material Make(string name, Color color, float metallic, float smoothness)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }

        SetBaseColor(mat, color);
        SetFloat(mat, "_Metallic", metallic);
        SetFloat(mat, "_Smoothness", 1f - smoothness);
        SetFloat(mat, "_Glossiness", 1f - smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material MakeEmission(string name, Color baseColor, Color emission)
    {
        Material mat = Make(name, baseColor, 0.2f, 0.18f);
        mat.EnableKeyword("_EMISSION");

        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", emission);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SetBaseColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }

    private static void SetFloat(Material mat, string property, float value)
    {
        if (mat.HasProperty(property)) mat.SetFloat(property, value);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
