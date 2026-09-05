#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Assigns the authored Blender/OBJ models to the three current World Events.
/// Gameplay tuning and other designer-authored event fields are left unchanged.
/// </summary>
public static class WorldEventModelGenerator
{
    private const string AuthoredModelFolder = "Assets/Models/WorldEvents";
    private const string RootFolder = "Assets/WorldEvents";
    private const string ModelFolder = RootFolder + "/GeneratedModels";
    private const string MaterialFolder = ModelFolder + "/Materials";
    private const string MeshFolder = ModelFolder + "/Meshes";

    [MenuItem("Tower Defense/Event/Generate 3 World Event Models")]
    public static void GenerateAll()
    {
        GameObject dogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuthoredModelFolder + "/DogCatRain_LuckyPawDrop.obj");
        GameObject meteorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuthoredModelFolder + "/MeteorShower_Meteor.obj");
        GameObject holyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuthoredModelFolder + "/HolyLight_Shrine.obj");

        if (dogPrefab == null || meteorPrefab == null || holyPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "World Event Models Missing",
                "The authored OBJ models must exist in Assets/Models/WorldEvents before assignment.",
                "OK");
            return;
        }

        AssignGeneratedPrefabs(dogPrefab, meteorPrefab, holyPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = holyPrefab;
        EditorGUIUtility.PingObject(holyPrefab);
        EditorUtility.DisplayDialog(
            "World Event Models Ready",
            "Assigned the 3 authored world-event models:\n\n" +
            "• DogCatRain_LuckyPawDrop\n" +
            "• MeteorShower_Meteor\n" +
            "• HolyLight_Shrine\n\n" +
            "They were automatically assigned to matching WorldEventData assets when found. Existing gameplay tuning was not changed.",
            "OK");
    }

    private static GameObject BuildDogCatRainDrop(Material gold, Material paleGold)
    {
        GameObject root = new GameObject("DogCatRain_LuckyPawDrop");

        GameObject coin = Primitive(PrimitiveType.Cylinder, "LuckyCoin", root.transform, Vector3.zero,
            new Vector3(0.78f, 0.11f, 0.78f), gold);
        coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject inset = Primitive(PrimitiveType.Cylinder, "CoinInset", root.transform, new Vector3(0f, 0f, -0.115f),
            new Vector3(0.60f, 0.032f, 0.60f), paleGold);
        inset.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Large paw pad.
        AddSphere(root.transform, "PawPad", new Vector3(0f, -0.04f, -0.17f), new Vector3(0.22f, 0.18f, 0.055f), gold);
        AddSphere(root.transform, "Toe_L", new Vector3(-0.25f, 0.15f, -0.17f), new Vector3(0.105f, 0.12f, 0.05f), gold);
        AddSphere(root.transform, "Toe_ML", new Vector3(-0.085f, 0.24f, -0.17f), new Vector3(0.105f, 0.12f, 0.05f), gold);
        AddSphere(root.transform, "Toe_MR", new Vector3(0.085f, 0.24f, -0.17f), new Vector3(0.105f, 0.12f, 0.05f), gold);
        AddSphere(root.transform, "Toe_R", new Vector3(0.25f, 0.15f, -0.17f), new Vector3(0.105f, 0.12f, 0.05f), gold);

        // Tiny cat/dog-ear silhouette on the upper edge makes it readable even while spinning.
        AddCone(root.transform, "Ear_Left", new Vector3(-0.30f, 0.48f, 0f), new Vector3(0.16f, 0.24f, 0.12f), paleGold, -16f);
        AddCone(root.transform, "Ear_Right", new Vector3(0.30f, 0.48f, 0f), new Vector3(0.16f, 0.24f, 0.12f), paleGold, 16f);

        // Soft cloud puffs behind the token.
        Material cloud = GetMaterial("Event_Cloud", new Color(0.63f, 0.72f, 0.98f), new Color(0.1f, 0.18f, 0.55f), 0.75f, 0f);
        AddSphere(root.transform, "Cloud_A", new Vector3(-0.34f, -0.28f, 0.18f), new Vector3(0.32f, 0.22f, 0.20f), cloud);
        AddSphere(root.transform, "Cloud_B", new Vector3(0.00f, -0.34f, 0.22f), new Vector3(0.40f, 0.26f, 0.22f), cloud);
        AddSphere(root.transform, "Cloud_C", new Vector3(0.34f, -0.28f, 0.18f), new Vector3(0.32f, 0.22f, 0.20f), cloud);

        root.transform.localScale = Vector3.one * 0.9f;
        return root;
    }

    private static GameObject BuildMeteor(Material rock, Material magma)
    {
        GameObject root = new GameObject("MeteorShower_Meteor");
        GameObject body = Primitive(PrimitiveType.Sphere, "RockCore", root.transform, Vector3.zero,
            new Vector3(0.82f, 1.05f, 0.78f), rock);
        body.transform.localRotation = Quaternion.Euler(17f, 29f, 8f);

        // Jagged outer chunks.
        Vector3[] offsets =
        {
            new Vector3(.42f,.18f,.16f), new Vector3(-.38f,.31f,-.12f), new Vector3(.18f,-.44f,-.24f),
            new Vector3(-.12f,-.34f,.38f), new Vector3(.05f,.46f,-.31f), new Vector3(-.42f,-.10f,.12f)
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject chunk = Primitive(PrimitiveType.Cube, "RockChunk_" + i, root.transform, offsets[i],
                new Vector3(.34f,.26f,.30f), rock);
            chunk.transform.localRotation = Quaternion.Euler(31f * i, 47f * i, 19f * i);
        }

        // Magma seams: small emissive beads embedded around the body.
        Vector3[] seams =
        {
            new Vector3(.28f,.28f,-.48f), new Vector3(-.23f,.04f,-.52f), new Vector3(.05f,-.28f,-.48f),
            new Vector3(-.42f,.25f,.02f), new Vector3(.43f,-.16f,.08f)
        };
        for (int i = 0; i < seams.Length; i++)
            AddSphere(root.transform, "Magma_" + i, seams[i], new Vector3(.105f,.24f,.07f), magma);

        // Hot rear core + trail. EventManager controls the actual fall movement.
        AddSphere(root.transform, "HotCore", new Vector3(0f, 0.62f, 0f), new Vector3(.32f,.30f,.32f), magma);
        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.32f;
        trail.minVertexDistance = 0.08f;
        trail.startWidth = 0.58f;
        trail.endWidth = 0f;
        trail.material = magma;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.textureMode = LineTextureMode.Stretch;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, .78f, .14f), 0f), new GradientColorKey(new Color(1f, .08f, .01f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;

        root.transform.localScale = Vector3.one * 0.95f;
        return root;
    }

    private static GameObject BuildHolyLightShrine(Material gold, Material stone, Material cyan, Material white, Material beam)
    {
        GameObject root = new GameObject("HolyLight_Shrine");
        GameObject architecture = new GameObject("Architecture");
        architecture.transform.SetParent(root.transform, false);

        // Floating base stack.
        Primitive(PrimitiveType.Cylinder, "Base_Lower", architecture.transform, new Vector3(0f, -0.55f, 0f), new Vector3(2.6f, .20f, 2.6f), stone);
        Primitive(PrimitiveType.Cylinder, "Base_Gold", architecture.transform, new Vector3(0f, -0.38f, 0f), new Vector3(2.22f, .12f, 2.22f), gold);
        Primitive(PrimitiveType.Cylinder, "Base_Inner", architecture.transform, new Vector3(0f, -0.25f, 0f), new Vector3(1.75f, .13f, 1.75f), stone);

        Mesh torus = GetOrCreateTorusMesh();
        Transform ringA = MeshObject("Halo_Base", architecture.transform, torus, gold, new Vector3(0f, -0.10f, 0f), new Vector3(1.42f, 1.42f, 1.42f));
        ringA.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Central crystal altar.
        Mesh crystalMesh = GetOrCreateCrystalMesh();
        Transform crystal = MeshObject("HolyCrystal", architecture.transform, crystalMesh, cyan, new Vector3(0f, 0.72f, 0f), new Vector3(.72f, 1.38f, .72f));
        AddSphere(crystal, "CrystalCore", Vector3.zero, new Vector3(.28f,.38f,.28f), white);

        // Four ivory/gold prongs around the core.
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a) * .72f, .47f, Mathf.Sin(a) * .72f);
            GameObject prong = Primitive(PrimitiveType.Cube, "Prong_" + i, architecture.transform, p, new Vector3(.16f, 1.0f, .16f), stone);
            prong.transform.rotation = Quaternion.Euler(0f, -i * 90f, 0f) * Quaternion.Euler(0f, 0f, i % 2 == 0 ? -18f : 18f);
            Primitive(PrimitiveType.Cube, "GoldTip_" + i, architecture.transform, p + Vector3.up * .48f, new Vector3(.22f,.18f,.22f), gold);
        }

        // Vertical halo frames.
        Transform halo1 = MeshObject("Halo_Vertical_A", architecture.transform, torus, gold, new Vector3(0f, 1.15f, 0f), Vector3.one * 1.72f);
        halo1.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Transform halo2 = MeshObject("Halo_Vertical_B", architecture.transform, torus, cyan, new Vector3(0f, 1.15f, 0f), Vector3.one * 1.50f);
        halo2.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Floating runic shards around the shrine.
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            Transform shard = MeshObject("FloatingShard_" + i, architecture.transform, crystalMesh,
                i % 2 == 0 ? cyan : gold,
                new Vector3(Mathf.Cos(a) * 2.05f, .72f + (i % 2) * .42f, Mathf.Sin(a) * 2.05f),
                new Vector3(.15f,.36f,.15f));
            shard.localRotation = Quaternion.Euler(0f, -i * 45f, 12f);
        }

        // Light column. Transparent emissive geometry makes the event readable even without particles.
        GameObject beamColumn = Primitive(PrimitiveType.Cylinder, "HolyBeam", architecture.transform, new Vector3(0f, 4.0f, 0f),
            new Vector3(.33f, 4.2f, .33f), beam);
        RemoveCollider(beamColumn);
        GameObject beamGlow = Primitive(PrimitiveType.Cylinder, "HolyBeamGlow", architecture.transform, new Vector3(0f, 3.55f, 0f),
            new Vector3(.72f, 3.8f, .72f), beam);
        RemoveCollider(beamGlow);

        // Star crown at the top.
        Transform crown = MeshObject("CrownCrystal", architecture.transform, crystalMesh, white, new Vector3(0f, 2.86f, 0f), new Vector3(.30f,.58f,.30f));
        Transform topHalo = MeshObject("Halo_Top", architecture.transform, torus, gold, new Vector3(0f, 2.72f, 0f), Vector3.one * 1.05f);
        topHalo.localRotation = Quaternion.Euler(90f, 0f, 0f);

        WorldEventVisualAnimator animator = root.AddComponent<WorldEventVisualAnimator>();
        animator.rotatingParts = new[]
        {
            new WorldEventVisualAnimator.RotatingPart { target = ringA, degreesPerSecond = new Vector3(0f, 22f, 0f) },
            new WorldEventVisualAnimator.RotatingPart { target = halo1, degreesPerSecond = new Vector3(28f, 0f, 0f) },
            new WorldEventVisualAnimator.RotatingPart { target = halo2, degreesPerSecond = new Vector3(0f, 0f, -34f) },
            new WorldEventVisualAnimator.RotatingPart { target = topHalo, degreesPerSecond = new Vector3(0f, 42f, 0f) }
        };
        animator.pulseTarget = crystal;
        animator.pulseAmount = 0.055f;
        animator.pulseSpeed = 0.82f;
        animator.floatTarget = architecture.transform;
        animator.floatHeight = 0.10f;
        animator.floatSpeed = 0.42f;

        // Event visual is decorative, so remove all colliders generated by primitives.
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = colliders.Length - 1; i >= 0; i--) Object.DestroyImmediate(colliders[i]);
        return root;
    }

    private static void AssignGeneratedPrefabs(GameObject dog, GameObject meteor, GameObject holy)
    {
        string[] guids = AssetDatabase.FindAssets("t:WorldEventData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            WorldEventData data = AssetDatabase.LoadAssetAtPath<WorldEventData>(path);
            if (data == null) continue;
            bool changed = false;
            switch (data.eventType)
            {
                case WorldEventType.DogCatRain:
                    data.goldDropPrefab = dog;
                    changed = true;
                    break;
                case WorldEventType.MeteorShower:
                    data.meteorPrefab = meteor;
                    changed = true;
                    break;
                case WorldEventType.HolyLight:
                    data.holyLightVisualPrefab = holy;
                    changed = true;
                    break;
            }
            if (changed) EditorUtility.SetDirty(data);
        }
    }

    private static GameObject SavePrefabAndDestroy(GameObject root, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "WorldEvents");
        EnsureFolder(RootFolder, "GeneratedModels");
        EnsureFolder(ModelFolder, "Materials");
        EnsureFolder(ModelFolder, "Meshes");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    private static Material GetMaterial(string name, Color color, Color emission, float smoothness = .45f, float metallic = .15f)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (emission.maxColorComponent > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetTransparentMaterial(string name, Color color, Color emission)
    {
        Material mat = GetMaterial(name, color, emission, .1f, 0f);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        RemoveCollider(go);
        return go;
    }

    private static void AddSphere(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        => Primitive(PrimitiveType.Sphere, name, parent, pos, scale, mat);

    private static void AddCone(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, float zRotation)
    {
        Transform cone = MeshObject(name, parent, GetOrCreateConeMesh(), mat, pos, scale);
        cone.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
    }

    private static Transform MeshObject(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        return go.transform;
    }

    private static Mesh GetOrCreateTorusMesh()
    {
        string path = MeshFolder + "/Event_Torus.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;

        const int majorSegments = 40;
        const int minorSegments = 10;
        const float majorRadius = 1f;
        const float minorRadius = .075f;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();
        for (int i = 0; i <= majorSegments; i++)
        {
            float u = i / (float)majorSegments * Mathf.PI * 2f;
            Vector3 center = new Vector3(Mathf.Cos(u) * majorRadius, 0f, Mathf.Sin(u) * majorRadius);
            for (int j = 0; j <= minorSegments; j++)
            {
                float v = j / (float)minorSegments * Mathf.PI * 2f;
                Vector3 radial = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));
                Vector3 n = radial * Mathf.Cos(v) + Vector3.up * Mathf.Sin(v);
                vertices.Add(center + n * minorRadius);
                normals.Add(n.normalized);
                uv.Add(new Vector2(i / (float)majorSegments, j / (float)minorSegments));
            }
        }
        int row = minorSegments + 1;
        for (int i = 0; i < majorSegments; i++)
        for (int j = 0; j < minorSegments; j++)
        {
            int a = i * row + j;
            int b = (i + 1) * row + j;
            triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
            triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
        }
        Mesh mesh = new Mesh { name = "Event_Torus" };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh GetOrCreateCrystalMesh()
    {
        string path = MeshFolder + "/Event_Crystal.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        Vector3[] v =
        {
            new Vector3(0,1,0), new Vector3(.58f,.18f,0), new Vector3(0,.18f,.58f), new Vector3(-.58f,.18f,0), new Vector3(0,.18f,-.58f),
            new Vector3(0,-1,0)
        };
        int[] t =
        {
            0,1,2, 0,2,3, 0,3,4, 0,4,1,
            5,2,1, 5,3,2, 5,4,3, 5,1,4
        };
        Mesh mesh = new Mesh { name = "Event_Crystal" };
        mesh.vertices = v; mesh.triangles = t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh GetOrCreateConeMesh()
    {
        string path = MeshFolder + "/Event_Cone.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;
        const int sides = 16;
        List<Vector3> verts = new List<Vector3> { new Vector3(0f, .5f, 0f), new Vector3(0f, -.5f, 0f) };
        for (int i = 0; i < sides; i++)
        {
            float a = i / (float)sides * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * .5f, -.5f, Mathf.Sin(a) * .5f));
        }
        List<int> tris = new List<int>();
        for (int i = 0; i < sides; i++)
        {
            int cur = 2 + i;
            int next = 2 + ((i + 1) % sides);
            tris.Add(0); tris.Add(next); tris.Add(cur);
            tris.Add(1); tris.Add(cur); tris.Add(next);
        }
        Mesh mesh = new Mesh { name = "Event_Cone" };
        mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
#endif
