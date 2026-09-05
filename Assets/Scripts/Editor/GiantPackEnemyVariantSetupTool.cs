#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds custom enemy variants from the EXISTING Kevin Iglesias Humanoid Giant pack.
/// Vendor assets are referenced only and are never modified.
/// Existing generated variants are preserved on rerun.
/// </summary>
public static class GiantPackEnemyVariantSetupTool
{
    private const string BaseEnemyPrefab = "Assets/Prefabs/Enemies/Brute.prefab";
    private const string VariantPrefabFolder = "Assets/Prefabs/Enemies/GiantVariants";
    private const string VariantDataFolder = "Assets/GameData/Enemies/GiantVariants";
    private const string VariantMaterialFolder = "Assets/Art/Materials/Enemies/GiantVariants";
    private const string VariantAnimationFolder = "Assets/Art/Animations/GiantVariants";
    private const string ControllerPath = VariantAnimationFolder + "/GiantPackEnemy.controller";

    private const string IdlePath = "Assets/Prefabs/Enemies/Kevin Iglesias/Characters/Humanoid Giant/Animations/Giant@Idle01.fbx";
    private const string WalkPath = "Assets/Prefabs/Enemies/Kevin Iglesias/Characters/Humanoid Giant/Animations/Movement/Walk/Giant@Walk01 - Forward.fbx";
    private const string RunPath = "Assets/Prefabs/Enemies/Kevin Iglesias/Characters/Humanoid Giant/Animations/Movement/Run/Giant@Run01 - Forward.fbx";
    private const string DamagePath = "Assets/Prefabs/Enemies/Kevin Iglesias/Characters/Humanoid Giant/Animations/Combat/Giant@Damage01.fbx";

    private enum VariantStyle
    {
        Armored,
        Crystal,
        Berserker,
        Ancient
    }

    private sealed class VariantSpec
    {
        public string name;
        public VariantStyle style;
        public Color tint;
        public Color accent;
        public float scale;
        public float hp;
        public float speed;
        public float regen;
        public float regenTick;
        public int gold;
        public int playerDamage;
        public float ccResist;
        public float shieldAt;
        public float shieldAmount;
        public float shieldDuration;
        public Color shieldTint;
        public float deathDuration;
    }

    [MenuItem("Tower Defense/Enemies/Create Giant Pack Variants")]
    public static void CreateVariants()
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseEnemyPrefab);
        if (basePrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Giant Variants",
                "Base enemy prefab was not found at:\n" + BaseEnemyPrefab + "\n\nNothing was changed.",
                "OK");
            return;
        }

        EnsureFolder(VariantPrefabFolder);
        EnsureFolder(VariantDataFolder);
        EnsureFolder(VariantMaterialFolder);
        EnsureFolder(VariantAnimationFolder);

        RuntimeAnimatorController controller = GetOrCreateController();

        VariantSpec[] specs =
        {
            new VariantSpec
            {
                name = "Armored Colossus",
                style = VariantStyle.Armored,
                tint = new Color(0.62f, 0.67f, 0.72f, 1f),
                accent = new Color(0.20f, 0.26f, 0.31f, 1f),
                scale = 1.10f,
                hp = 1800f,
                speed = 1.15f,
                regen = 0f,
                regenTick = 1f,
                gold = 45,
                playerDamage = 4,
                ccResist = 0.45f,
                shieldAt = 0f,
                shieldAmount = 0f,
                shieldDuration = 0f,
                shieldTint = new Color(0.50f, 0.72f, 0.90f, 1f),
                deathDuration = 0.85f
            },
            new VariantSpec
            {
                name = "Crystal Giant",
                style = VariantStyle.Crystal,
                tint = new Color(0.58f, 0.78f, 0.88f, 1f),
                accent = new Color(0.12f, 0.92f, 1f, 1f),
                scale = 1.05f,
                hp = 1450f,
                speed = 1.35f,
                regen = 0f,
                regenTick = 1f,
                gold = 50,
                playerDamage = 3,
                ccResist = 0.30f,
                shieldAt = 0.55f,
                shieldAmount = 500f,
                shieldDuration = 7f,
                shieldTint = new Color(0.10f, 0.95f, 1f, 1f),
                deathDuration = 0.85f
            },
            new VariantSpec
            {
                name = "Berserker Giant",
                style = VariantStyle.Berserker,
                tint = new Color(0.80f, 0.42f, 0.30f, 1f),
                accent = new Color(0.95f, 0.15f, 0.05f, 1f),
                scale = 0.98f,
                hp = 950f,
                speed = 2.35f,
                regen = 0f,
                regenTick = 1f,
                gold = 38,
                playerDamage = 5,
                ccResist = 0.22f,
                shieldAt = 0f,
                shieldAmount = 0f,
                shieldDuration = 0f,
                shieldTint = new Color(1f, 0.30f, 0.15f, 1f),
                deathDuration = 0.75f
            },
            new VariantSpec
            {
                name = "Ancient Titan",
                style = VariantStyle.Ancient,
                tint = new Color(0.42f, 0.52f, 0.33f, 1f),
                accent = new Color(0.26f, 0.34f, 0.18f, 1f),
                scale = 1.18f,
                hp = 2600f,
                speed = 0.85f,
                regen = 14f,
                regenTick = 1f,
                gold = 70,
                playerDamage = 6,
                ccResist = 0.55f,
                shieldAt = 0f,
                shieldAmount = 0f,
                shieldDuration = 0f,
                shieldTint = new Color(0.46f, 0.76f, 0.35f, 1f),
                deathDuration = 0.95f
            }
        };

        int createdPrefabs = 0;
        int createdData = 0;
        int skippedExisting = 0;

        foreach (VariantSpec spec in specs)
        {
            string prefabPath = VariantPrefabFolder + "/" + spec.name + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                prefab = CreateVariantPrefab(spec, controller, prefabPath);
                if (prefab != null)
                    createdPrefabs++;
            }
            else
            {
                skippedExisting++;
            }

            if (prefab == null)
                continue;

            string dataPath = VariantDataFolder + "/" + spec.name + ".asset";
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                data.enemyName = spec.name;
                data.enemyPrefab = prefab;
                data.maxHP = spec.hp;
                data.moveSpeed = spec.speed;
                data.hpRegenPerSec = spec.regen;
                data.hpRegenTickInterval = spec.regenTick;
                data.goldReward = spec.gold;
                data.damageToPlayer = spec.playerDamage;
                data.ccResistPercent = spec.ccResist;
                data.shieldTriggerHPPercent = spec.shieldAt;
                data.shieldTriggerAmount = spec.shieldAmount;
                data.shieldTriggerDuration = spec.shieldDuration;
                data.tintColor = Color.white;
                data.shieldTintColor = spec.shieldTint;

                AssetDatabase.CreateAsset(data, dataPath);
                createdData++;
            }

            EnsureDropEntry(data);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Giant Pack Variants",
            "Finished.\n\n" +
            $"New prefabs: {createdPrefabs}\n" +
            $"New EnemyData assets: {createdData}\n" +
            $"Existing variants preserved: {skippedExisting}\n\n" +
            "Created variants:\n" +
            "- Armored Colossus\n" +
            "- Crystal Giant\n" +
            "- Berserker Giant\n" +
            "- Ancient Titan\n\n" +
            "Vendor Giant assets were not modified.\n" +
            "No waves were changed automatically.",
            "OK");
    }

    private static GameObject CreateVariantPrefab(VariantSpec spec, RuntimeAnimatorController controller, string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BaseEnemyPrefab);
        if (root == null)
            return null;

        try
        {
            root.name = spec.name;
            root.transform.localScale *= spec.scale;

            Enemy enemy = root.GetComponent<Enemy>();
            Animator animator = root.GetComponentInChildren<Animator>(true);

            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
            }

            if (enemy != null)
            {
                enemy.animator = animator;
                enemy.speedParam = "Speed";
                enemy.dieTrigger = "Die";
                enemy.deathAnimDuration = spec.deathDuration;
            }

            ApplyVariantMaterials(root, spec);
            AddVariantAccessories(root, spec);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return saved;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static RuntimeAnimatorController GetOrCreateController()
    {
        RuntimeAnimatorController existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (existing != null)
            return existing;

        AnimationClip idle = LoadFirstClip(IdlePath);
        AnimationClip walk = LoadFirstClip(WalkPath);
        AnimationClip run = LoadFirstClip(RunPath);
        AnimationClip damage = LoadFirstClip(DamagePath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states)
            machine.RemoveState(child.state);

        BlendTree blendTree = new BlendTree
        {
            name = "GiantMoveBlend",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(blendTree, controller);

        if (idle != null) blendTree.AddChild(idle, 0f);
        if (walk != null) blendTree.AddChild(walk, 1.25f);
        if (run != null) blendTree.AddChild(run, 2.5f);

        AnimatorState moveState = machine.AddState("Move");
        moveState.motion = blendTree;
        machine.defaultState = moveState;

        AnimatorState dieState = machine.AddState("Die");
        dieState.motion = damage;
        dieState.speed = 0.72f;

        AnimatorStateTransition transition = moveState.AddTransition(dieState);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Die");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip LoadFirstClip(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip == null)
                continue;
            if (clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
                continue;
            return clip;
        }
        return null;
    }

    private static void ApplyVariantMaterials(GameObject root, VariantSpec spec)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Dictionary<Material, Material> replacements = new Dictionary<Material, Material>();
        int index = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            Material[] source = renderer.sharedMaterials;
            if (source == null || source.Length == 0)
                continue;

            Material[] target = new Material[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                Material original = source[i];
                if (original == null)
                {
                    target[i] = null;
                    continue;
                }

                if (!replacements.TryGetValue(original, out Material variant))
                {
                    string safeName = Sanitize(spec.name) + "_Surface_" + index++;
                    string path = VariantMaterialFolder + "/" + safeName + ".mat";
                    variant = AssetDatabase.LoadAssetAtPath<Material>(path);

                    if (variant == null)
                    {
                        variant = new Material(original)
                        {
                            name = safeName
                        };

                        Color baseColor = Color.white;
                        if (variant.HasProperty("_BaseColor"))
                            baseColor = variant.GetColor("_BaseColor");
                        else if (variant.HasProperty("_Color"))
                            baseColor = variant.GetColor("_Color");

                        Color tinted = Color.Lerp(baseColor, spec.tint, 0.42f);
                        tinted.a = baseColor.a;

                        if (variant.HasProperty("_BaseColor")) variant.SetColor("_BaseColor", tinted);
                        if (variant.HasProperty("_Color")) variant.SetColor("_Color", tinted);

                        if (spec.style == VariantStyle.Crystal)
                        {
                            variant.EnableKeyword("_EMISSION");
                            Color emission = spec.accent * 1.8f;
                            if (variant.HasProperty("_EmissionColor"))
                                variant.SetColor("_EmissionColor", emission);
                        }

                        AssetDatabase.CreateAsset(variant, path);
                    }

                    replacements.Add(original, variant);
                }

                target[i] = variant;
            }

            renderer.sharedMaterials = target;
        }
    }

    private static void AddVariantAccessories(GameObject root, VariantSpec spec)
    {
        Transform chest = FindDeep(root.transform, "B-chest");
        Transform head = FindDeep(root.transform, "B-head");
        Transform leftShoulder = FindDeep(root.transform, "B-shoulder.L");
        Transform rightShoulder = FindDeep(root.transform, "B-shoulder.R");
        Transform leftForearm = FindDeep(root.transform, "B-forearm.L");
        Transform rightForearm = FindDeep(root.transform, "B-forearm.R");

        Material accent = GetOrCreateAccentMaterial(spec);

        switch (spec.style)
        {
            case VariantStyle.Armored:
                AddBox(chest, "Armor_Chest", new Vector3(0f, 0.08f, 0.23f), new Vector3(0.85f, 0.18f, 0.58f), Vector3.zero, accent);
                AddBox(leftShoulder, "Armor_Shoulder_L", new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.20f, 0.42f), new Vector3(0f, 0f, 18f), accent);
                AddBox(rightShoulder, "Armor_Shoulder_R", new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.20f, 0.42f), new Vector3(0f, 0f, -18f), accent);
                AddCylinder(leftForearm, "Armor_Bracer_L", new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.23f, 0.22f), new Vector3(90f, 0f, 0f), accent);
                AddCylinder(rightForearm, "Armor_Bracer_R", new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.23f, 0.22f), new Vector3(90f, 0f, 0f), accent);
                break;

            case VariantStyle.Crystal:
                AddBox(chest, "Crystal_Back_Center", new Vector3(0f, 0.12f, -0.28f), new Vector3(0.16f, 0.55f, 0.16f), new Vector3(28f, 0f, 45f), accent);
                AddBox(leftShoulder, "Crystal_Shoulder_L", new Vector3(0f, 0.10f, 0f), new Vector3(0.14f, 0.42f, 0.14f), new Vector3(18f, 0f, 32f), accent);
                AddBox(rightShoulder, "Crystal_Shoulder_R", new Vector3(0f, 0.10f, 0f), new Vector3(0.14f, 0.42f, 0.14f), new Vector3(-18f, 0f, -32f), accent);
                AddBox(head, "Crystal_Crown", new Vector3(0f, 0.18f, -0.02f), new Vector3(0.12f, 0.34f, 0.12f), new Vector3(0f, 0f, 12f), accent);
                break;

            case VariantStyle.Berserker:
                AddCylinder(head, "Horn_L", new Vector3(-0.18f, 0.08f, 0f), new Vector3(0.08f, 0.28f, 0.08f), new Vector3(0f, 0f, 35f), accent);
                AddCylinder(head, "Horn_R", new Vector3(0.18f, 0.08f, 0f), new Vector3(0.08f, 0.28f, 0.08f), new Vector3(0f, 0f, -35f), accent);
                AddCylinder(leftForearm, "Berserker_Bracer_L", Vector3.zero, new Vector3(0.20f, 0.22f, 0.20f), new Vector3(90f, 0f, 0f), accent);
                AddCylinder(rightForearm, "Berserker_Bracer_R", Vector3.zero, new Vector3(0.20f, 0.22f, 0.20f), new Vector3(90f, 0f, 0f), accent);
                break;

            case VariantStyle.Ancient:
                AddSphere(leftShoulder, "Ancient_Boulder_L", Vector3.zero, new Vector3(0.42f, 0.30f, 0.40f), accent);
                AddSphere(rightShoulder, "Ancient_Boulder_R", Vector3.zero, new Vector3(0.42f, 0.30f, 0.40f), accent);
                AddBox(chest, "Ancient_ChestStone", new Vector3(0f, 0.04f, 0.20f), new Vector3(0.66f, 0.15f, 0.48f), new Vector3(0f, 0f, 4f), accent);
                AddSphere(head, "Ancient_Crest", new Vector3(0f, 0.12f, -0.05f), new Vector3(0.18f, 0.14f, 0.18f), accent);
                break;
        }
    }

    private static Material GetOrCreateAccentMaterial(VariantSpec spec)
    {
        string path = VariantMaterialFolder + "/" + Sanitize(spec.name) + "_Accent.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        material = new Material(shader)
        {
            name = Sanitize(spec.name) + "_Accent"
        };

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", spec.accent);
        if (material.HasProperty("_Color")) material.SetColor("_Color", spec.accent);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", spec.style == VariantStyle.Armored ? 0.7f : 0.15f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", spec.style == VariantStyle.Crystal ? 0.85f : 0.38f);

        if (spec.style == VariantStyle.Crystal)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", spec.accent * 2.2f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void AddBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
    {
        AddPrimitive(parent, PrimitiveType.Cube, name, localPosition, localScale, localEuler, material);
    }

    private static void AddSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        AddPrimitive(parent, PrimitiveType.Sphere, name, localPosition, localScale, Vector3.zero, material);
    }

    private static void AddCylinder(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Vector3 localEuler, Material material)
    {
        AddPrimitive(parent, PrimitiveType.Cylinder, name, localPosition, localScale, localEuler, material);
    }

    private static void AddPrimitive(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEuler,
        Material material)
    {
        if (parent == null)
            return;

        Transform existing = parent.Find(name);
        if (existing != null)
            return;

        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.transform.localEulerAngles = localEuler;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static Transform FindDeep(Transform root, string exactName)
    {
        if (root == null)
            return null;

        if (root.name == exactName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), exactName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void EnsureDropEntry(EnemyData data)
    {
        if (data == null)
            return;

        string[] guids = AssetDatabase.FindAssets("t:EnemyDropDatabase");
        if (guids == null || guids.Length == 0)
            return;

        EnemyDropDatabase database = AssetDatabase.LoadAssetAtPath<EnemyDropDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (database == null)
            return;

        if (database.entries == null)
            database.entries = new List<EnemyDropEntry>();

        foreach (EnemyDropEntry entry in database.entries)
            if (entry != null && entry.enemy == data)
                return;

        database.entries.Add(new EnemyDropEntry
        {
            enemy = data,
            diamondDropChance = 0f,
            diamondDropMin = 1,
            diamondDropMax = 1,
            relicDropChance = 0f,
            isBoss = false,
            bossGuaranteedDiamonds = false,
            bossGuaranteedRelic = false
        });

        EditorUtility.SetDirty(database);
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value.Replace(" ", "_");
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
        string name = Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
