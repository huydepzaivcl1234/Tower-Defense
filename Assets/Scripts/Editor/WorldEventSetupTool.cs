#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WorldEventSetupTool
{
    private const string FallbackFolder = "Assets/WorldEvents";

    [MenuItem("Tower Defense/Event/Setup World Event System")]
    public static void Setup()
    {
        WorldEventManager manager = Object.FindAnyObjectByType<WorldEventManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            GameObject managerGo = new GameObject("WorldEventManager");
            Undo.RegisterCreatedObjectUndo(managerGo, "Create WorldEventManager");
            manager = managerGo.AddComponent<WorldEventManager>();
        }

        WorldEventData dog = ResolveOrCreate(manager, WorldEventType.DogCatRain, "DogCatRain.asset", "Dog & Cat Rain", WorldEventRarity.Common);
        WorldEventData meteor = ResolveOrCreate(manager, WorldEventType.MeteorShower, "MeteorShower.asset", "Meteor Shower", WorldEventRarity.Common);
        WorldEventData holy = ResolveOrCreate(manager, WorldEventType.HolyLight, "HolyLight.asset", "Holy Light", WorldEventRarity.Rare);

        NormalizeIdentity(dog, "Dog & Cat Rain", WorldEventRarity.Common, WorldEventType.DogCatRain);
        NormalizeIdentity(meteor, "Meteor Shower", WorldEventRarity.Common, WorldEventType.MeteorShower);
        NormalizeIdentity(holy, "Holy Light", WorldEventRarity.Rare, WorldEventType.HolyLight);

        EnsureEventInPool(manager, dog);
        EnsureEventInPool(manager, meteor);
        EnsureEventInPool(manager, holy);

        WorldEventGeneratedAssets.EnsureAndAssign(dog, meteor, holy);

        if (manager.eventChancePerOpportunity <= 0f)
            manager.eventChancePerOpportunity = 0.35f;
        if (manager.rareChanceWhenEventRolls <= 0f)
            manager.rareChanceWhenEventRolls = 0.10f;
        if (manager.firstEligibleWave < 1)
            manager.firstEligibleWave = 2;

        EnsureAudioSource(manager);
        EnsureWorldCenter(manager);
        EnsureAnnouncementUI(manager);
        SyncHUDTargetsFromNpcDialogue(manager);

        if (manager.holyDirectionalLight == null && RenderSettings.sun != null)
            manager.holyDirectionalLight = RenderSettings.sun;

        EditorUtility.SetDirty(manager);
        if (dog != null) EditorUtility.SetDirty(dog);
        if (meteor != null) EditorUtility.SetDirty(meteor);
        if (holy != null) EditorUtility.SetDirty(holy);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = manager.gameObject;
        EditorGUIUtility.PingObject(manager.gameObject);

        string message =
            "World Event System repaired without replacing your custom data.\n\n" +
            "Runtime sources:\n" +
            "Dog: " + WorldEventAssetResolver.Describe(dog) + "\n" +
            "Meteor: " + WorldEventAssetResolver.Describe(meteor) + "\n" +
            "Holy: " + WorldEventAssetResolver.Describe(holy) + "\n\n" +
            "Event HUD slide now mirrors the existing NPC DialogueHUDPresentationController targets and hidden offsets.";

        EditorUtility.DisplayDialog("World Event System Ready", message, "OK");
    }

    private static WorldEventData ResolveOrCreate(
        WorldEventManager manager,
        WorldEventType type,
        string fallbackFile,
        string displayName,
        WorldEventRarity rarity)
    {
        WorldEventData data = WorldEventAssetResolver.Resolve(manager, type);
        if (data != null)
            return data;

        EnsureFallbackFolder();
        string path = FallbackFolder + "/" + fallbackFile;
        data = AssetDatabase.LoadAssetAtPath<WorldEventData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WorldEventData>();
            data.eventName = displayName;
            data.eventType = type;
            data.rarity = rarity;
            data.selectionWeight = 1f;
            data.durationRounds = type == WorldEventType.HolyLight ? 3 : 2;

            if (type == WorldEventType.DogCatRain)
            {
                data.description = "Gold rains while enemies are alive, but enemies gain increased maximum HP.";
                data.enemyMaxHpBonusPercent = 0.30f;
            }
            else if (type == WorldEventType.MeteorShower)
            {
                data.description = "Meteors damage enemies by Max HP and temporarily slow struck towers' attack speed.";
            }
            else
            {
                data.description = "Holy Light empowers towers, but the blessing may collapse and empower enemies instead.";
            }

            AssetDatabase.CreateAsset(data, path);
        }
        return data;
    }

    private static void NormalizeIdentity(
        WorldEventData data,
        string displayName,
        WorldEventRarity rarity,
        WorldEventType type)
    {
        if (data == null)
            return;

        data.eventName = displayName;
        data.rarity = rarity;
        data.eventType = type;
        if (data.selectionWeight <= 0f)
            data.selectionWeight = 1f;
        if (data.durationRounds < 1)
            data.durationRounds = 1;
    }

    private static void EnsureFallbackFolder()
    {
        if (!AssetDatabase.IsValidFolder(FallbackFolder))
            AssetDatabase.CreateFolder("Assets", "WorldEvents");
    }

    private static void EnsureEventInPool(WorldEventManager manager, WorldEventData data)
    {
        if (manager == null || data == null)
            return;
        if (manager.eventPool == null)
            manager.eventPool = new List<WorldEventData>();

        for (int i = 0; i < manager.eventPool.Count; i++)
        {
            WorldEventData existing = manager.eventPool[i];
            if (existing != null && existing.eventType == data.eventType)
                return;
        }
        manager.eventPool.Add(data);
    }

    private static void EnsureAudioSource(WorldEventManager manager)
    {
        if (manager.eventAudioSource == null)
            manager.eventAudioSource = manager.GetComponent<AudioSource>();
        if (manager.eventAudioSource == null)
            manager.eventAudioSource = Undo.AddComponent<AudioSource>(manager.gameObject);

        manager.eventAudioSource.playOnAwake = false;
        manager.eventAudioSource.loop = false;
        manager.eventAudioSource.spatialBlend = 0f;
    }

    private static void EnsureWorldCenter(WorldEventManager manager)
    {
        if (manager.worldCenter != null)
            return;

        Transform child = manager.transform.Find("WorldEventAreaCenter");
        if (child == null)
        {
            GameObject go = new GameObject("WorldEventAreaCenter");
            Undo.RegisterCreatedObjectUndo(go, "Create World Event Area Center");
            go.transform.SetParent(manager.transform, false);
            child = go.transform;
        }
        manager.worldCenter = child;
    }

    private static void EnsureAnnouncementUI(WorldEventManager manager)
    {
        if (manager.announcementRoot != null &&
            manager.rarityText != null && manager.titleText != null && manager.descriptionText != null && manager.iconImage != null)
            return;

        Canvas canvas = FindScreenCanvas();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("WorldEventCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create World Event Canvas");
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        Transform existing = FindDeepChild(canvas.transform, "WorldEventAnnouncement");
        GameObject root = existing != null
            ? existing.gameObject
            : new GameObject("WorldEventAnnouncement", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));

        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(root, "Create World Event Announcement");
            root.transform.SetParent(canvas.transform, false);
        }
        root.transform.SetAsLastSibling();

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(820f, 250f);
        rootRect.anchoredPosition = Vector2.zero;

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.025f, 0.055f, 0.09f, 0.97f);
        background.raycastTarget = false;

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        manager.announcementRoot = group;

        manager.rarityText = EnsureText(root.transform, "Rarity", new Vector2(65f, 78f), new Vector2(610f, 30f), 18f, FontStyles.Bold);
        manager.titleText = EnsureText(root.transform, "Title", new Vector2(65f, 28f), new Vector2(610f, 50f), 34f, FontStyles.Bold);
        manager.descriptionText = EnsureText(root.transform, "Description", new Vector2(65f, -52f), new Vector2(610f, 82f), 17f, FontStyles.Normal);

        Transform iconTr = root.transform.Find("Icon");
        GameObject iconGo = iconTr != null ? iconTr.gameObject : new GameObject("Icon", typeof(RectTransform), typeof(Image));
        if (iconTr == null)
            iconGo.transform.SetParent(root.transform, false);

        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(110f, 110f);
        iconRect.anchoredPosition = new Vector2(78f, 0f);

        manager.iconImage = iconGo.GetComponent<Image>();
        manager.iconImage.preserveAspect = true;
        manager.iconImage.raycastTarget = false;

        group.alpha = 0f;
        root.SetActive(false);
    }

    private static TMP_Text EnsureText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, FontStyles style)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (found == null)
            go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static Canvas FindScreenCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas fallback = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                continue;
            if (fallback == null)
                fallback = canvas;
            string n = canvas.name.ToLowerInvariant();
            if (n.Contains("hud") || n.Contains("game") || n.Contains("qol"))
                return canvas;
        }
        return fallback;
    }

    /// <summary>
    /// Reuse the exact HUD targets and hidden offsets already authored for NPC dialogue.
    /// This intentionally removes the separate automatic edge/Canvas offset calculation.
    /// </summary>
    private static void SyncHUDTargetsFromNpcDialogue(WorldEventManager manager)
    {
        DialogueHUDPresentationController dialogueHud = Object.FindAnyObjectByType<DialogueHUDPresentationController>(FindObjectsInactive.Include);
        if (dialogueHud == null || dialogueHud.hudTargets == null || dialogueHud.hudTargets.Count == 0)
        {
            Debug.LogWarning(
                "WorldEventSetupTool: no configured DialogueHUDPresentationController was found. " +
                "WorldEventManager HUD targets were left unchanged.",
                manager);
            return;
        }

        Undo.RecordObject(manager, "Sync World Event HUD From NPC Dialogue");

        if (manager.hudTargets == null)
            manager.hudTargets = new List<WorldEventManager.HUDTarget>();
        else
            manager.hudTargets.Clear();

        for (int i = 0; i < dialogueHud.hudTargets.Count; i++)
        {
            DialogueHUDPresentationController.HUDSlideTarget source = dialogueHud.hudTargets[i];
            if (source == null || source.target == null)
                continue;

            manager.hudTargets.Add(new WorldEventManager.HUDTarget
            {
                target = source.target,
                hiddenOffset = source.hiddenOffset,
                captured = false
            });
        }

        manager.hudSlideDuration = Mathf.Max(0f, dialogueHud.slideOutDuration);
        EditorUtility.SetDirty(manager);

        Debug.Log(
            $"World Event HUD now mirrors NPC dialogue: {manager.hudTargets.Count} target(s), " +
            $"duration {manager.hudSlideDuration:0.###}s.",
            manager);
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindDeepChild(root.GetChild(i), name);
            if (child != null)
                return child;
        }
        return null;
    }
}
#endif
