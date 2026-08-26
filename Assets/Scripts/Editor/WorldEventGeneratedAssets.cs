#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WorldEventGeneratedAssets
{
    private const string RootFolder = "Assets/WorldEvents/Generated";
    private const string AudioFolder = RootFolder + "/Audio";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string IconFolder = RootFolder + "/Icons";

    public static void EnsureAndAssign(WorldEventData dogCat, WorldEventData meteor, WorldEventData holy)
    {
        EnsureFolders();

        Sprite dogIcon = EnsureDogCatIcon();
        Sprite meteorIcon = EnsureMeteorIcon();
        Sprite holyIcon = EnsureHolyIcon();

        AudioClip dogSfx = EnsureDogCatSfx();
        AudioClip meteorSfx = EnsureMeteorSfx();
        AudioClip holySfx = EnsureHolySfx();
        AudioClip holyCollapseSfx = EnsureHolyCollapseSfx();

        GameObject dogPrefab = EnsureDogCatDropPrefab();
        GameObject meteorPrefab = EnsureMeteorPrefab();
        GameObject holyPrefab = EnsureHolyLightPrefab();

        if (dogCat != null)
        {
            if (dogCat.icon == null) dogCat.icon = dogIcon;
            if (dogCat.announcementSfx == null) dogCat.announcementSfx = dogSfx;
            if (dogCat.goldDropPrefab == null) dogCat.goldDropPrefab = dogPrefab;
            EditorUtility.SetDirty(dogCat);
        }

        if (meteor != null)
        {
            if (meteor.icon == null) meteor.icon = meteorIcon;
            if (meteor.announcementSfx == null) meteor.announcementSfx = meteorSfx;
            if (meteor.meteorPrefab == null) meteor.meteorPrefab = meteorPrefab;
            EditorUtility.SetDirty(meteor);
        }

        if (holy != null)
        {
            if (holy.icon == null) holy.icon = holyIcon;
            if (holy.announcementSfx == null) holy.announcementSfx = holySfx;
            if (holy.holyCollapseSfx == null) holy.holyCollapseSfx = holyCollapseSfx;
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
        EnsureFolder(RootFolder, "Icons");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    // ---------------------------------------------------------------------
    // Icons
    // ---------------------------------------------------------------------

    private static Sprite EnsureDogCatIcon()
    {
        return EnsureIcon(IconFolder + "/DogCatRain_Icon.png", texture =>
        {
            DrawDisc(texture, new Vector2(128f, 128f), 112f, new Color(0.09f, 0.16f, 0.40f, 1f));
            DrawDisc(texture, new Vector2(82f, 142f), 55f, new Color(0.24f, 0.36f, 0.88f, 1f));
            DrawDisc(texture, new Vector2(132f, 158f), 66f, new Color(0.30f, 0.43f, 0.98f, 1f));
            DrawDisc(texture, new Vector2(179f, 139f), 53f, new Color(0.35f, 0.30f, 0.82f, 1f));
            DrawDisc(texture, new Vector2(128f, 105f), 62f, new Color(0.27f, 0.39f, 0.93f, 1f));

            Color gold = new Color(1f, 0.72f, 0.08f, 1f);
            Color bright = new Color(1f, 0.93f, 0.42f, 1f);
            DrawDisc(texture, new Vector2(128f, 104f), 27f, gold);
            DrawDisc(texture, new Vector2(91f, 137f), 15f, bright);
            DrawDisc(texture, new Vector2(116f, 150f), 15f, bright);
            DrawDisc(texture, new Vector2(143f, 150f), 15f, bright);
            DrawDisc(texture, new Vector2(168f, 137f), 15f, bright);

            DrawLine(texture, new Vector2(68f, 70f), new Vector2(68f, 38f), 6f, gold);
            DrawLine(texture, new Vector2(128f, 66f), new Vector2(128f, 27f), 7f, gold);
            DrawLine(texture, new Vector2(188f, 70f), new Vector2(188f, 42f), 6f, gold);
        });
    }

    private static Sprite EnsureMeteorIcon()
    {
        return EnsureIcon(IconFolder + "/MeteorShower_Icon.png", texture =>
        {
            DrawDisc(texture, new Vector2(128f, 128f), 112f, new Color(0.10f, 0.07f, 0.25f, 1f));
            DrawDisc(texture, new Vector2(128f, 128f), 102f, new Color(0.14f, 0.10f, 0.38f, 1f));

            Color orange = new Color(1f, 0.24f, 0.02f, 1f);
            Color yellow = new Color(1f, 0.82f, 0.12f, 1f);
            Color rock = new Color(0.22f, 0.13f, 0.12f, 1f);

            DrawLine(texture, new Vector2(48f, 210f), new Vector2(138f, 120f), 19f, orange);
            DrawLine(texture, new Vector2(58f, 201f), new Vector2(143f, 116f), 9f, yellow);
            DrawDisc(texture, new Vector2(157f, 99f), 42f, orange);
            DrawDisc(texture, new Vector2(157f, 99f), 31f, rock);
            DrawDisc(texture, new Vector2(145f, 112f), 10f, yellow);
            DrawDisc(texture, new Vector2(175f, 91f), 8f, orange);

            DrawLine(texture, new Vector2(120f, 207f), new Vector2(165f, 162f), 7f, orange);
            DrawDisc(texture, new Vector2(175f, 151f), 12f, yellow);
            DrawLine(texture, new Vector2(175f, 214f), new Vector2(205f, 183f), 6f, orange);
            DrawDisc(texture, new Vector2(211f, 176f), 10f, yellow);
        });
    }

    private static Sprite EnsureHolyIcon()
    {
        return EnsureIcon(IconFolder + "/HolyLight_Icon.png", texture =>
        {
            DrawDisc(texture, new Vector2(128f, 128f), 112f, new Color(0.04f, 0.18f, 0.34f, 1f));
            DrawRing(texture, new Vector2(128f, 128f), 92f, 7f, new Color(1f, 0.78f, 0.18f, 1f));
            DrawRing(texture, new Vector2(128f, 169f), 48f, 7f, new Color(1f, 0.87f, 0.38f, 1f));
            DrawLine(texture, new Vector2(128f, 204f), new Vector2(128f, 48f), 17f, new Color(1f, 0.94f, 0.64f, 1f));
            DrawLine(texture, new Vector2(128f, 199f), new Vector2(128f, 54f), 7f, Color.white);
            DrawStar(texture, new Vector2(128f, 176f), 34f, 10f, new Color(1f, 0.92f, 0.42f, 1f));
            DrawRing(texture, new Vector2(128f, 52f), 46f, 5f, new Color(0.35f, 0.88f, 1f, 1f));
        });
    }

    private static Sprite EnsureIcon(string assetPath, Action<Texture2D> draw)
    {
        string absolute = ToAbsolutePath(assetPath);
        if (!File.Exists(absolute))
        {
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color[] clear = new Color[256 * 256];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            texture.SetPixels(clear);
            draw?.Invoke(texture);
            texture.Apply(false, false);
            File.WriteAllBytes(absolute, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void DrawDisc(Texture2D texture, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + radius));
        float r2 = radius * radius;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 delta = new Vector2(x, y) - center;
                if (delta.sqrMagnitude <= r2)
                    texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DrawRing(Texture2D texture, Vector2 center, float radius, float thickness, Color color)
    {
        float outer2 = radius * radius;
        float inner = Mathf.Max(0f, radius - thickness);
        float inner2 = inner * inner;
        int bound = Mathf.CeilToInt(radius);

        for (int y = Mathf.Max(0, (int)center.y - bound); y <= Mathf.Min(texture.height - 1, (int)center.y + bound); y++)
        {
            for (int x = Mathf.Max(0, (int)center.x - bound); x <= Mathf.Min(texture.width - 1, (int)center.x + bound); x++)
            {
                float sq = (new Vector2(x, y) - center).sqrMagnitude;
                if (sq <= outer2 && sq >= inner2)
                    texture.SetPixel(x, y, color);
            }
        }
    }

    private static void DrawLine(Texture2D texture, Vector2 from, Vector2 to, float width, Color color)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            DrawDisc(texture, Vector2.Lerp(from, to, t), width * 0.5f, color);
        }
    }

    private static void DrawStar(Texture2D texture, Vector2 center, float longRadius, float shortRadius, Color color)
    {
        DrawLine(texture, center + Vector2.up * longRadius, center - Vector2.up * longRadius, shortRadius, color);
        DrawLine(texture, center + Vector2.right * longRadius, center - Vector2.right * longRadius, shortRadius, color);
        DrawDisc(texture, center, shortRadius * 0.75f, Color.white);
    }

    // ---------------------------------------------------------------------
    // SFX
    // ---------------------------------------------------------------------

    private static AudioClip EnsureDogCatSfx()
    {
        string path = AudioFolder + "/DogCatRain_Announcement.wav";
        EnsureWav(path, 1.25f, (t, random) =>
        {
            float env = Mathf.Exp(-2.2f * t);
            float chime = 0.28f * Mathf.Sin(2f * Mathf.PI * 880f * t)
                        + 0.20f * Mathf.Sin(2f * Mathf.PI * 1320f * t)
                        + 0.12f * Mathf.Sin(2f * Mathf.PI * 1760f * t);
            float sparkle = t > 0.36f
                ? 0.18f * Mathf.Sin(2f * Mathf.PI * 1174.66f * (t - 0.36f)) * Mathf.Exp(-7f * (t - 0.36f))
                : 0f;
            return Mathf.Clamp(chime * env + sparkle, -0.9f, 0.9f);
        });
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static AudioClip EnsureMeteorSfx()
    {
        string path = AudioFolder + "/MeteorShower_Announcement.wav";
        EnsureWav(path, 1.55f, (t, random) =>
        {
            float envelope = Mathf.Clamp01(1f - t / 1.55f);
            float rumble = (0.22f * Mathf.Sin(2f * Mathf.PI * (65f - 15f * t) * t)
                          + 0.13f * Mathf.Sin(2f * Mathf.PI * 42f * t)
                          + 0.08f * random) * envelope;
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
            float envelope = attack * release;
            float chord = 0.18f * Mathf.Sin(2f * Mathf.PI * 523.25f * t)
                        + 0.16f * Mathf.Sin(2f * Mathf.PI * 659.25f * t)
                        + 0.15f * Mathf.Sin(2f * Mathf.PI * 783.99f * t)
                        + 0.10f * Mathf.Sin(2f * Mathf.PI * 1046.50f * t);
            float shimmer = 0.05f * Mathf.Sin(2f * Mathf.PI * 1567.98f * t)
                          * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * t));
            return Mathf.Clamp((chord + shimmer) * envelope, -0.85f, 0.85f);
        });
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static AudioClip EnsureHolyCollapseSfx()
    {
        string path = AudioFolder + "/HolyLight_Collapse.wav";
        EnsureWav(path, 1.5f, (t, random) =>
        {
            float envelope = Mathf.Clamp01(1f - t / 1.5f);
            float fall = 0.22f * Mathf.Sin(2f * Mathf.PI * (420f - 210f * t) * t);
            float low = 0.24f * Mathf.Sin(2f * Mathf.PI * 72f * t);
            float crack = t < 0.35f ? random * 0.17f * (1f - t / 0.35f) : 0f;
            return Mathf.Clamp((fall + low + crack) * envelope, -0.9f, 0.9f);
        });
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static void EnsureWav(string assetPath, float duration, Func<float, float, float> sampleFunction)
    {
        string absolute = ToAbsolutePath(assetPath);
        if (File.Exists(absolute))
            return;

        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        short[] pcm = new short[sampleCount];
        System.Random randomGenerator = new System.Random(assetPath.GetHashCode());

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float noise = (float)(randomGenerator.NextDouble() * 2.0 - 1.0);
            float sample = Mathf.Clamp(sampleFunction(t, noise), -1f, 1f);
            pcm[i] = (short)Mathf.RoundToInt(sample * short.MaxValue);
        }

        File.WriteAllBytes(absolute, BuildWavBytes(pcm, sampleRate));
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static byte[] BuildWavBytes(short[] samples, int sampleRate)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        int dataSize = samples.Length * sizeof(short);

        using (MemoryStream stream = new MemoryStream(44 + dataSize))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            for (int i = 0; i < samples.Length; i++)
                writer.Write(samples[i]);
            writer.Flush();
            return stream.ToArray();
        }
    }

    // ---------------------------------------------------------------------
    // Fallback 3D prefabs
    // ---------------------------------------------------------------------

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
            AddPrimitive(root.transform, PrimitiveType.Cube, "RockChunk" + i, offset, new Vector3(0.25f, 0.24f, 0.28f), new Vector3(i * 21f, angle, i * 31f), rock);
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
        AddPrimitive(root.transform, PrimitiveType.Sphere, "BlessingOrb", new Vector3(0f, 4.9f, 0f), new Vector3(0.42f, 0.42f, 0.42f), Vector3.zero, whiteGlow);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(1.8f, 0f, 0f);
            AddPrimitive(root.transform, PrimitiveType.Cube, "Ray" + i, new Vector3(offset.x, 0.12f, offset.z), new Vector3(1.15f, 0.035f, 0.08f), new Vector3(0f, -angle, 0f), goldGlow);
        }

        return SavePrefabAndDestroy(root, path);
    }

    private static Transform AddPrimitive(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEuler,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.transform.localEulerAngles = localEuler;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

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
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            return null;

        Material material = new Material(shader) { name = fileName };
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

    private static string ToAbsolutePath(string assetPath)
    {
        return Path.Combine(
            Directory.GetCurrentDirectory(),
            assetPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
#endif
