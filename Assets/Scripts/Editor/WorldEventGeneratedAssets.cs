#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WorldEventGeneratedAssets
{
    private const string RootFolder = "Assets/WorldEvents/Generated";
    private const string AudioFolder = RootFolder + "/Audio";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string MaterialFolder = RootFolder + "/Materials";

    public static void EnsureAndAssign(WorldEventData dogCat, WorldEventData meteor, WorldEventData holy)
    {
        EnsureFolders();

        AudioClip dogCatSfx = EnsureDogCatSfx();
        AudioClip meteorSfx = EnsureMeteorSfx();
        AudioClip holySfx = EnsureHolySfx();

        GameObject dogCatPrefab = EnsureDogCatDropPrefab();
        GameObject meteorPrefab = EnsureMeteorPrefab();
        GameObject holyPrefab = EnsureHolyLightPrefab();

        if (dogCat != null)
        {
            if (dogCat.announcementSfx == null) dogCat.announcementSfx = dogCatSfx;
            if (dogCat.goldDropPrefab == null) dogCat.goldDropPrefab = dogCatPrefab;
            EditorUtility.SetDirty(dogCat);
        }

        if (meteor != null)
        {
            if (meteor.announcementSfx == null) meteor.announcementSfx = meteorSfx;
            if (meteor.meteorPrefab == null) meteor.meteorPrefab = meteorPrefab;
            EditorUtility.SetDirty(meteor);
        }

        if (holy != null)
        {
            if (holy.announcementSfx == null) holy.announcementSfx = holySfx;
            if (holy.holyLightVisualPrefab == null) holy.holyLightVisualPrefab = holyPrefab;
            EditorUtility.SetDirty(holy);
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/WorldEvents", "Generated");
        EnsureFolder(RootFolder, "Audio");
        EnsureFolder(RootFolder, "Prefabs");
        EnsureFolder(RootFolder, "Materials");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static AudioClip EnsureDogCatSfx()
    {
        string path = AudioFolder + "/DogCatRain_Announcement.wav";
        EnsureWav(path, 1.25f, (t, random) =>
        {
            float env = Mathf.Exp(-2.2f * t);
            float chime = 0.28f * Mathf.Sin(2f * Mathf.PI * 880f * t)
                        + 0.20f * Mathf.Sin(2f * Mathf.PI * 1320f * t)
                        + 0.12f * Mathf.Sin(2f * Mathf.PI * 1760f * t);
            float sparkle = t > 0.36f ? 0.18f * Mathf.Sin(2f * Mathf.PI * 1174.66f * (t - 0.36f)) * Mathf.Exp(-7f * (t - 0.36f)) : 0f;
            return Mathf.Clamp((chime * env) + sparkle, -0.9f, 0.9f);
        });
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static AudioClip EnsureMeteorSfx()
    {
        string path = AudioFolder + "/MeteorShower_Announcement.wav";
        EnsureWav(path, 1.55f, (t, random) =>
        {
            float rumbleEnv = Mathf.Clamp01(1f - (t / 1.55f));
            float rumble = (0.22f * Mathf.Sin(2f * Mathf.PI * (65f - 15f * t) * t)
                          + 0.13f * Mathf.Sin(2f * Mathf.PI * 42f * t)
                          + 0.08f * random) * rumbleEnv;
            float impactT = t - 0.58f;
            float impact = impactT >= 0f
                ? (0.42f * Mathf.Sin(2f * Mathf.PI * 95f * impactT) + 0.20f * random) * Mathf.Exp(-5.5f * impactT)
                : 0f;
            return Mathf.Clamp(rumble + impact, -0.95f, 0.95f);
        });
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static AudioClip EnsureHolySfx()
    {
        string path = AudioFolder + "/HolyLight_Announcement.wav";
        EnsureWav(path, 1.8f, (t, random) =>
        {
            float attack = Mathf.Clamp01(t / 0.10f);
            float release = Mathf.Clamp01((1.8f - t) / 0.55f);
            float env = attack * release;
            float chord = 0.18f * Mathf.Sin(2f * Mathf.PI * 523.25f * t)
                        + 0.16f * Mathf.Sin(2f * Mathf.PI * 659.25f * t)
                        + 0.15f * Mathf.Sin(2f * Mathf.PI * 783.99f * t)
                        + 0.10f * Mathf.Sin(2f * Mathf.PI * 1046.50f * t);
            float shimmer = 0.05f * Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * t));
            return Mathf.Clamp((chord + shimmer) * env, -0.85f, 0.85f);
        });
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static void EnsureWav(string assetPath, float duration, Func<float, float, float> sampleFunction)
    {
        if (File.Exists(ToAbsolutePath(assetPath)))
            return;

        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        short[] pcm = new short[sampleCount];
        System.Random rng = new System.Random(assetPath.GetHashCode());

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float sample = Mathf.Clamp(sampleFunction(t, noise), -1f, 1f);
            pcm[i] = (short)Mathf.RoundToInt(sample * short.MaxValue);
        }

        byte[] wav = BuildWavBytes(pcm, sampleRate);
        File.WriteAllBytes(ToAbsolutePath(assetPath), wav);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static byte[] BuildWavBytes(short[] samples, int sampleRate)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        short blockAlign = (short)(channels * (bitsPerSample / 8));
        int dataSize = samples.Length * sizeof(short);

        using (MemoryStream stream = new MemoryStream(44 + dataSize))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);
            for (int i = 0; i < samples.Length; i++) writer.Write(samples[i]);
            writer.Flush();
            return stream.ToArray();
        }
    }

    private static string ToAbsolutePath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static GameObject EnsureDogCatDropPrefab()
    {
        string path = PrefabFolder + "/DogCatRain_GoldPaw.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        Material gold = EnsureMaterial("GoldPaw_Mat", new Color(1f, 0.63f, 0.08f), new Color(1f, 0.34f, 0.02f) * 1.5f, 0.75f, 0.55f);
        Material darkGold = EnsureMaterial("GoldPawDark_Mat", new Color(0.58f, 0.24f, 0.03f), Color.black, 0.65f, 0.42f);
        Material sparkle = EnsureMaterial("GoldSparkle_Mat", new Color(1f, 0.93f, 0.55f), new Color(1f, 0.70f, 0.12f) * 2.6f, 0.2f, 0.2f);

        GameObject root = new GameObject("DogCatRain_GoldPaw");
        AddPrimitive(root.transform, PrimitiveType.Cylinder, "Coin", Vector3.zero, new Vector3(0.78f, 0.11f, 0.78f), new Vector3(90f, 0f, 0f), gold);
        AddPrimitive(root.transform, PrimitiveType.Sphere, "PawPad", new Vector3(0f, 0f, -0.03f), new Vector3(0.30f, 0.12f, 0.23f), Vector3.zero, darkGold);
        AddPrimitive(root.transform, PrimitiveType.Sphere, "ToeL", new Vector3(-0.22f, 0.02f, 0.18f), new Vector3(0.14f, 0.09f, 0.12f), Vector3.zero, darkGold);
        AddPrimitive(root.transform, PrimitiveType.Sphere, "ToeML", new Vector3(-0.07f, 0.02f, 0.25f), new Vector3(0.13f, 0.09f, 0.12f), Vector3.zero, darkGold);
        AddPrimitive(root.transform, PrimitiveType.Sphere, "ToeMR", new Vector3(0.08f, 0.02f, 0.25f), new Vector3(0.13f, 0.09f, 0.12f), Vector3.zero, darkGold);
        AddPrimitive(root.transform, PrimitiveType.Sphere, "ToeR", new Vector3(0.23f, 0.02f, 0.18f), new Vector3(0.14f, 0.09f, 0.12f), Vector3.zero, darkGold);

        AddPrimitive(root.transform, PrimitiveType.Cube, "SparkleA", new Vector3(0.48f, 0.08f, 0.05f), new Vector3(0.035f, 0.035f, 0.26f), new Vector3(0f, 25f, 0f), sparkle);
        AddPrimitive(root.transform, PrimitiveType.Cube, "SparkleB", new Vector3(0.48f, 0.08f, 0.05f), new Vector3(0.035f, 0.035f, 0.26f), new Vector3(0f, 115f, 0f), sparkle);

        return SavePrefabAndDestroy(root, path);
    }

    private static GameObject EnsureMeteorPrefab()
    {
        string path = PrefabFolder + "/MeteorShower_Meteor.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        Material rock = EnsureMaterial("MeteorRock_Mat", new Color(0.16f, 0.10f, 0.08f), Color.black, 0.05f, 0.1f);
        Material lava = EnsureMaterial("MeteorLava_Mat", new Color(1f, 0.22f, 0.015f), new Color(1f, 0.12f, 0.01f) * 4.2f, 0.12f, 0.25f);
        Material trail = EnsureMaterial("MeteorTrail_Mat", new Color(1f, 0.48f, 0.04f), new Color(1f, 0.20f, 0.01f) * 3.4f, 0.05f, 0.15f);

        GameObject root = new GameObject("MeteorShower_Meteor");
        AddPrimitive(root.transform, PrimitiveType.Sphere, "Core", Vector3.zero, new Vector3(0.90f, 0.78f, 0.88f), Vector3.zero, rock);
        AddPrimitive(root.transform, PrimitiveType.Sphere, "HotCore", new Vector3(0.08f, -0.02f, 0.10f), new Vector3(0.70f, 0.61f, 0.68f), Vector3.zero, lava);

        for (int i = 0; i < 7; i++)
        {
            float angle = i * (360f / 7f);
            Vector3 offset = Quaternion.Euler(0f, angle, i * 17f) * new Vector3(0.42f, 0.10f + 0.04f * (i % 2), 0f);
            Vector3 scale = new Vector3(0.25f + 0.05f * (i % 3), 0.22f + 0.04f * ((i + 1) % 3), 0.28f);
            AddPrimitive(root.transform, PrimitiveType.Cube, "RockChunk" + i, offset, scale, new Vector3(i * 21f, angle, i * 31f), rock);
        }

        AddPrimitive(root.transform, PrimitiveType.Cylinder, "TrailCore", new Vector3(0f, 0f, 1.15f), new Vector3(0.28f, 1.15f, 0.28f), new Vector3(90f, 0f, 0f), trail);
        AddPrimitive(root.transform, PrimitiveType.Cylinder, "TrailOuter", new Vector3(0f, 0f, 1.75f), new Vector3(0.16f, 1.55f, 0.16f), new Vector3(90f, 0f, 0f), lava);

        return SavePrefabAndDestroy(root, path);
    }

    private static GameObject EnsureHolyLightPrefab()
    {
        string path = PrefabFolder + "/HolyLight_Blessing.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        Material whiteGlow = EnsureMaterial("HolyWhite_Mat", new Color(1f, 0.98f, 0.82f), new Color(1f, 0.90f, 0.55f) * 3.8f, 0.1f, 0.25f);
        Material goldGlow = EnsureMaterial("HolyGold_Mat", new Color(1f, 0.72f, 0.12f), new Color(1f, 0.56f, 0.08f) * 3.1f, 0.15f, 0.35f);
        Material softGlow = EnsureMaterial("HolySoft_Mat", new Color(0.86f, 0.92f, 1f), new Color(0.55f, 0.74f, 1f) * 2.2f, 0.08f, 0.18f);

        GameObject root = new GameObject("HolyLight_Blessing");
        AddPrimitive(root.transform, PrimitiveType.Cylinder, "MainBeam", new Vector3(0f, 2.7f, 0f), new Vector3(0.70f, 2.7f, 0.70f), Vector3.zero, softGlow);
        AddPrimitive(root.transform, PrimitiveType.Cylinder, "InnerBeam", new Vector3(0f, 2.9f, 0f), new Vector3(0.32f, 2.9f, 0.32f), Vector3.zero, whiteGlow);
        AddPrimitive(root.transform, PrimitiveType.Cylinder, "GroundDisc", new Vector3(0f, 0.05f, 0f), new Vector3(2.2f, 0.04f, 2.2f), Vector3.zero, goldGlow);
        AddPrimitive(root.transform, PrimitiveType.Cylinder, "Halo", new Vector3(0f, 4.9f, 0f), new Vector3(1.45f, 0.06f, 1.45f), Vector3.zero, goldGlow);
        AddPrimitive(root.transform, PrimitiveType.Cylinder, "HaloCutout", new Vector3(0f, 4.91f, 0f), new Vector3(0.95f, 0.07f, 0.95f), Vector3.zero, softGlow);
        AddPrimitive(root.transform, PrimitiveType.Sphere, "BlessingOrb", new Vector3(0f, 4.9f, 0f), new Vector3(0.42f, 0.42f, 0.42f), Vector3.zero, whiteGlow);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(1.8f, 0f, 0f);
            AddPrimitive(root.transform, PrimitiveType.Cube, "Ray" + i, new Vector3(offset.x, 0.12f, offset.z), new Vector3(1.15f, 0.035f, 0.08f), new Vector3(0f, -angle, 0f), goldGlow);
        }

        return SavePrefabAndDestroy(root, path);
    }

    private static Transform AddPrimitive(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.transform.localEulerAngles = localEuler;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
        return go.transform;
    }

    private static GameObject SavePrefabAndDestroy(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Material EnsureMaterial(string fileName, Color color, Color emission, float metallic, float smoothness)
    {
        string path = MaterialFolder + "/" + fileName + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = fileName;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
#endif
