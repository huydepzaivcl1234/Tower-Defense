#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class AnimeSkyboxInstaller
{
    private const string TexturePath = "Assets/Skybox/AnimeBrightSky.png";
    private const string MaterialPath = "Assets/Skybox/AnimeBrightSky.mat";

    [MenuItem("Tower Defense/Environment/Setup Anime Skybox")]
    public static void SetupAnimeSkybox()
    {
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        Texture2D skyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (skyTexture == null)
        {
            Debug.LogError("Anime skybox texture not found at: " + TexturePath);
            return;
        }

        Shader shader = Shader.Find("Skybox/Panoramic");
        if (shader == null)
        {
            Debug.LogError("Shader 'Skybox/Panoramic' was not found.");
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "AnimeBrightSky" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_MainTex", skyTexture);
        if (material.HasProperty("_Tex")) material.SetTexture("_Tex", skyTexture);
        if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 1.08f);
        if (material.HasProperty("_Rotation")) material.SetFloat("_Rotation", 0f);
        if (material.HasProperty("_ImageType")) material.SetFloat("_ImageType", 0f); // 360
        if (material.HasProperty("_Mapping")) material.SetFloat("_Mapping", 1f);     // Latitude/Longitude
        EditorUtility.SetDirty(material);

        RenderSettings.skybox = material;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.05f;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            mainCamera.clearFlags = CameraClearFlags.Skybox;

        DynamicGI.UpdateEnvironment();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Anime Skybox",
            "AnimeBrightSky is now assigned as the scene skybox.\n\n" +
            "Material: " + MaterialPath + "\n" +
            "Texture: " + TexturePath + "\n\n" +
            "You can tune Exposure / Rotation directly on the material.",
            "OK");
    }
}
#endif
