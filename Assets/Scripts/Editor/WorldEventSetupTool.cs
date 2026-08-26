#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WorldEventSetupTool
{
    private const string EventFolder = "Assets/WorldEvents";

    [MenuItem("Tower Defense/Event/Setup World Event System")]
    public static void Setup()
    {
        EnsureFolder();

        bool dogCreated;
        WorldEventData dogCat = GetOrCreateEvent(
            "DogCatRain.asset",
            "Dog & Cat Rain",
            WorldEventRarity.Common,
            WorldEventType.DogCatRain,
            out dogCreated);
        if (dogCreated)
            ApplyDogCatDefaults(dogCat);

        bool meteorCreated;
        WorldEventData meteor = GetOrCreateEvent(
            "MeteorShower.asset",
            "Meteor Shower",
            WorldEventRarity.Common,
            WorldEventType.MeteorShower,
            out meteorCreated);
        if (meteorCreated)
            ApplyMeteorDefaults(meteor);

        bool holyCreated;
        WorldEventData holy = GetOrCreateEvent(
            "HolyLight.asset",
            "Holy Light",
            WorldEventRarity.Rare,
            WorldEventType.HolyLight,
            out holyCreated);
        if (holyCreated)
            ApplyHolyDefaults(holy);

        // Fill generated icon/SFX/model slots only when the user has not assigned custom assets.
        WorldEventGeneratedAssets.EnsureAndAssign(dogCat, meteor, holy);

        WorldEventManager manager = Object.FindAnyObjectByType<WorldEventManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            GameObject managerGo = new GameObject("WorldEventManager");
            Undo.RegisterCreatedObjectUndo(managerGo, "Create WorldEventManager");
            manager = managerGo.AddComponent<WorldEventManager>();
        }

        EnsureEventInPool(manager, dogCat);
        EnsureEventInPool(manager, meteor);
        EnsureEventInPool(manager, holy);

        if (Mathf.Approximately(manager.eventChancePerOpportunity, 0f))
            manager.eventChancePerOpportunity = 0.35f;
        if (Mathf.Approximately(manager.rareChanceWhenEventRolls, 0f))
            manager.rareChanceWhenEventRolls = 0.10f;
        if (manager.firstEligibleWave < 1)
            manager.firstEligibleWave = 2;

        EnsureAudioSource(manager);
        EnsureWorldCenter(manager);

        Canvas canvas = FindBestCanvas();
        if (canvas != null)
            SetupAnnouncementUI(manager, canvas.transform);

        AutoAssignHUDTargets(manager);

        Light sun = RenderSettings.sun;
        if (manager.holyDirectionalLight == null && sun != null)
            manager.holyDirectionalLight = sun;

        EditorUtility.SetDirty(dogCat);
        EditorUtility.SetDirty(meteor);
        EditorUtility.SetDirty(holy);
        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = manager.gameObject;
        EditorGUIUtility.PingObject(manager.gameObject);

        EditorUtility.DisplayDialog(
            "World Event System Ready",
            "World Event System has been created/repaired without overwriting your custom values.\n\n" +
            "• Dog & Cat Rain — Common\n" +
            "• Meteor Shower — Common\n" +
            "• Holy Light — Rare\n\n" +
            "Missing generated icons, SFX and fallback 3D prefabs were also assigned. " +
            "Move WorldEventAreaCenter to the center of the playable map if needed.",
            "OK");
    }

    private static void ApplyDogCatDefaults(WorldEventData data)
    {
        data.description = "Gold rains from the sky while enemies are alive, but enemies gain +30% maximum HP.";
        data.durationRounds = 2;
        data.selectionWeight = 1f;
        data.enemyMaxHpBonusPercent = 0.30f;
        data.goldPerDrop = 5;
        data.goldDropInterval = 0.8f;
        data.accentColor = new Color(1f, 0.76f, 0.18f, 1f);
    }

    private static void ApplyMeteorDefaults(WorldEventData data)
    {
        data.description = "Meteors fall while enemies are alive. Enemy hits lose Max-HP damage; tower hits temporarily reduce attack speed.";
        data.durationRounds = 2;
        data.selectionWeight = 1f;
        data.meteorChancePerTick = 0.22f;
        data.meteorTickInterval = 0.8f;
        data.meteorTargetEnemyChance = 0.75f;
        data.meteorTargetScatterRadius = 2.25f;
        data.meteorEnemyMaxHpDamagePercent = 0.10f;
        data.meteorTowerAttackSpeedPenaltyPercent = 0.20f;
        data.meteorTowerDebuffDuration = 4f;
        data.accentColor = new Color(1f, 0.34f, 0.08f, 1f);
    }

    private static void ApplyHolyDefaults(WorldEventData data)
    {
        data.description = "Holy Light empowers towers. At the end of an affected round, the blessing may collapse and empower enemies instead.";
        data.durationRounds = 3;
        data.selectionWeight = 1f;
        data.holyTowerAttackSpeedBonusPercent = 0.25f;
        data.holyTowerDamageBonusPercent = 0.25f;
        data.holyProjectileSpeedBonusPercent = 0.25f;
        data.holyCollapseChancePerRound = 0.20f;
        data.collapsePenaltyRounds = 2;
        data.collapseEnemyMaxHpBonusPercent = 0.35f;
        data.collapseEnemyCCResistanceBonusPercent = 0.30f;
        data.collapseEnemyShieldPercentOfMaxHp = 0.20f;
        data.accentColor = new Color(1f, 0.88f, 0.38f, 1f);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(EventFolder))
            AssetDatabase.CreateFolder("Assets", "WorldEvents");
    }

    private static WorldEventData GetOrCreateEvent(
        string fileName,
        string displayName,
        WorldEventRarity rarity,
        WorldEventType type,
        out bool created)
    {
        string path = EventFolder + "/" + fileName;
        WorldEventData data = AssetDatabase.LoadAssetAtPath<WorldEventData>(path);
        created = data == null;

        if (created)
        {
            data = ScriptableObject.CreateInstance<WorldEventData>();
            AssetDatabase.CreateAsset(data, path);
        }

        // Identity is canonical; gameplay tuning is not overwritten on re-run.
        data.eventName = displayName;
        data.rarity = rarity;
        data.eventType = type;
        if (data.selectionWeight <= 0f)
            data.selectionWeight = 1f;

        EditorUtility.SetDirty(data);
        return data;
    }

    private static void EnsureEventInPool(WorldEventManager manager, WorldEventData data)
    {
        if (manager == null || data == null)
            return;

        if (manager.eventPool == null)
            manager.eventPool = new List<WorldEventData>();

        if (!manager.eventPool.Contains(data))
            manager.eventPool.Add(data);
    }

    private static void EnsureAudioSource(WorldEventManager manager)
    {
        if (manager == null)
            return;

        AudioSource source = manager.eventAudioSource;
        if (source == null)
            source = manager.GetComponent<AudioSource>();
        if (source == null)
            source = Undo.AddComponent<AudioSource>(manager.gameObject);

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        manager.eventAudioSource = source;
    }

    private static void EnsureWorldCenter(WorldEventManager manager)
    {
        if (manager == null || manager.worldCenter != null)
            return;

        Transform existing = manager.transform.Find("WorldEventAreaCenter");
        if (existing != null)
        {
            manager.worldCenter = existing;
            return;
        }

        GameObject center = new GameObject("WorldEventAreaCenter");
        Undo.RegisterCreatedObjectUndo(center, "Create World Event Area Center");
        center.transform.SetParent(manager.transform, false);
        center.transform.localPosition = Vector3.zero;
        manager.worldCenter = center.transform;
    }

    private static Canvas FindBestCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas fallback = null;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            if (fallback == null)
                fallback = canvas;

            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            // Prefer a gameplay canvas, but any screen-space canvas is safe for the announcement.
            string lower = canvas.name.ToLowerInvariant();
            if (lower.Contains("hud") || lower.Contains("game") || lower.Contains("qol"))
                return canvas;
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].renderMode != RenderMode.WorldSpace)
                return canvases[i];
        }

        return fallback;
    }

    private static void SetupAnnouncementUI(WorldEventManager manager, Transform canvas)
    {
        Transform existing = FindDeepChild(canvas, "WorldEventAnnouncement");
        GameObject root = existing != null
            ? existing.gameObject
            : new GameObject("WorldEventAnnouncement", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));

        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(root, "Create World Event Announcement");
            root.transform.SetParent(canvas, false);
        }

        root.transform.SetAsLastSibling();

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(820f, 250f);
        rect.anchoredPosition = Vector2.zero;

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.025f, 0.055f, 0.09f, 0.97f);

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        manager.announcementRoot = group;

        manager.rarityText = EnsureText(
            root.transform,
            "Rarity",
            new Vector2(55f, 78f),
            new Vector2(610f, 30f),
            18f,
            FontStyles.Bold);

        manager.titleText = EnsureText(
            root.transform,
            "Title",
            new Vector2(55f, 28f),
            new Vector2(610f, 50f),
            34f,
            FontStyles.Bold);

        manager.descriptionText = EnsureText(
            root.transform,
            "Description",
            new Vector2(55f, -52f),
            new Vector2(610f, 82f),
            17f,
            FontStyles.Normal);

        Transform iconTransform = root.transform.Find("Icon");
        GameObject iconObject = iconTransform != null
            ? iconTransform.gameObject
            : new GameObject("Icon", typeof(RectTransform), typeof(Image));

        if (iconTransform == null)
            iconObject.transform.SetParent(root.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(105f, 105f);
        iconRect.anchoredPosition = new Vector2(75f, 0f);

        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        manager.iconImage = icon;

        group.alpha = 0f;
        root.SetActive(false);
    }

    private static TMP_Text EnsureText(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles style)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null
            ? found.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));

        if (found == null)
            go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void AutoAssignHUDTargets(WorldEventManager manager)
    {
        if (manager == null)
            return;

        if (manager.hudTargets == null)
            manager.hudTargets = new List<WorldEventManager.HUDTarget>();

        string[] preferredNames =
        {
            "HUDPanel",
            "ResourceHUD",
            "WaveHUD",
            "BuildDock",
            "UpgradePanelClean",
            "QoLCanvas",
            "QoLTopRight",
            "RelicOwnedHUD",
            "QuestLiveHUD",
            "GameSpeedController"
        };

        RectTransform[] allRects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<RectTransform> candidates = new List<RectTransform>();

        for (int n = 0; n < preferredNames.Length; n++)
        {
            string wanted = preferredNames[n];
            for (int i = 0; i < allRects.Length; i++)
            {
                RectTransform rect = allRects[i];
                if (rect == null || rect.name != wanted)
                    continue;
                if (manager.announcementRoot != null && rect.IsChildOf(manager.announcementRoot.transform))
                    continue;
                if (!candidates.Contains(rect))
                    candidates.Add(rect);
            }
        }

        // If both a parent HUD root and one of its children were found, move only the parent.
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            RectTransform candidate = candidates[i];
            for (int j = 0; j < candidates.Count; j++)
            {
                if (i == j)
                    continue;
                RectTransform possibleParent = candidates[j];
                if (candidate != null && possibleParent != null && candidate.IsChildOf(possibleParent))
                {
                    candidates.RemoveAt(i);
                    break;
                }
            }
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            RectTransform rect = candidates[i];
            if (rect == null || ContainsTarget(manager, rect))
                continue;

            manager.hudTargets.Add(new WorldEventManager.HUDTarget
            {
                target = rect,
                hiddenOffset = ChooseHiddenOffset(rect)
            });
        }
    }

    private static bool ContainsTarget(WorldEventManager manager, RectTransform target)
    {
        if (manager.hudTargets == null)
            return false;

        for (int i = 0; i < manager.hudTargets.Count; i++)
        {
            WorldEventManager.HUDTarget item = manager.hudTargets[i];
            if (item != null && item.target == target)
                return true;
        }

        return false;
    }

    private static Vector2 ChooseHiddenOffset(RectTransform rect)
    {
        Vector2 position = rect.anchoredPosition;

        if (Mathf.Abs(position.y) > Mathf.Abs(position.x) * 1.15f)
            return position.y >= 0f ? new Vector2(0f, 450f) : new Vector2(0f, -450f);

        return position.x < 0f
            ? new Vector2(-850f, 0f)
            : new Vector2(850f, 0f);
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChild(root.GetChild(i), name);
            if (result != null)
                return result;
        }

        return null;
    }
}
#endif
