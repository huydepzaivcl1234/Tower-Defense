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

        WorldEventData dogCat = GetOrCreateEvent("DogCatRain.asset", "Dog & Cat Rain", WorldEventRarity.Common, WorldEventType.DogCatRain);
        dogCat.description = "Gold rains from the sky while enemies are alive, but enemies gain +30% maximum HP.";
        dogCat.durationRounds = Mathf.Max(1, dogCat.durationRounds);
        dogCat.enemyMaxHpBonusPercent = 0.30f;
        dogCat.goldPerDrop = Mathf.Max(1, dogCat.goldPerDrop);
        EditorUtility.SetDirty(dogCat);

        WorldEventData meteor = GetOrCreateEvent("MeteorShower.asset", "Meteor Shower", WorldEventRarity.Common, WorldEventType.MeteorShower);
        meteor.description = "Meteors can strike the battlefield. Enemies lose a percentage of max HP; hit towers temporarily lose attack speed.";
        meteor.durationRounds = Mathf.Max(1, meteor.durationRounds);
        meteor.meteorEnemyMaxHpDamagePercent = meteor.meteorEnemyMaxHpDamagePercent <= 0f ? 0.10f : meteor.meteorEnemyMaxHpDamagePercent;
        meteor.meteorTowerAttackSpeedPenaltyPercent = meteor.meteorTowerAttackSpeedPenaltyPercent <= 0f ? 0.20f : meteor.meteorTowerAttackSpeedPenaltyPercent;
        EditorUtility.SetDirty(meteor);

        WorldEventData holy = GetOrCreateEvent("HolyLight.asset", "Holy Light", WorldEventRarity.Rare, WorldEventType.HolyLight);
        holy.description = "Holy Light empowers towers, but each round carries a chance for the blessing to collapse and empower enemies instead.";
        holy.durationRounds = Mathf.Max(2, holy.durationRounds);
        holy.selectionWeight = 1f;
        holy.holyTowerAttackSpeedBonusPercent = holy.holyTowerAttackSpeedBonusPercent <= 0f ? 0.25f : holy.holyTowerAttackSpeedBonusPercent;
        holy.holyTowerDamageBonusPercent = holy.holyTowerDamageBonusPercent <= 0f ? 0.25f : holy.holyTowerDamageBonusPercent;
        holy.holyProjectileSpeedBonusPercent = holy.holyProjectileSpeedBonusPercent <= 0f ? 0.25f : holy.holyProjectileSpeedBonusPercent;
        holy.holyCollapseChancePerRound = holy.holyCollapseChancePerRound <= 0f ? 0.20f : holy.holyCollapseChancePerRound;
        holy.collapseEnemyMaxHpBonusPercent = holy.collapseEnemyMaxHpBonusPercent <= 0f ? 0.35f : holy.collapseEnemyMaxHpBonusPercent;
        holy.collapseEnemyCCResistanceBonusPercent = holy.collapseEnemyCCResistanceBonusPercent <= 0f ? 0.30f : holy.collapseEnemyCCResistanceBonusPercent;
        holy.collapseEnemyShieldPercentOfMaxHp = holy.collapseEnemyShieldPercentOfMaxHp <= 0f ? 0.20f : holy.collapseEnemyShieldPercentOfMaxHp;
        EditorUtility.SetDirty(holy);

        WorldEventManager manager = Object.FindAnyObjectByType<WorldEventManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            GameObject managerGo = new GameObject("WorldEventManager");
            Undo.RegisterCreatedObjectUndo(managerGo, "Create WorldEventManager");
            manager = managerGo.AddComponent<WorldEventManager>();
        }

        manager.eventPool = new List<WorldEventData> { dogCat, meteor, holy };
        manager.eventChancePerOpportunity = Mathf.Approximately(manager.eventChancePerOpportunity, 0f) ? 0.35f : manager.eventChancePerOpportunity;
        manager.rareChanceWhenEventRolls = Mathf.Approximately(manager.rareChanceWhenEventRolls, 0f) ? 0.10f : manager.rareChanceWhenEventRolls;
        if (manager.firstEligibleWave < 1) manager.firstEligibleWave = 2;

        Canvas canvas = FindBestCanvas();
        if (canvas != null)
        {
            SetupAnnouncementUI(manager, canvas.transform);
            AutoAssignHUDTargets(manager, canvas.transform);
        }

        Light sun = RenderSettings.sun;
        if (manager.holyDirectionalLight == null && sun != null)
            manager.holyDirectionalLight = sun;

        EditorUtility.SetDirty(manager);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = manager.gameObject;
        EditorGUIUtility.PingObject(manager.gameObject);

        EditorUtility.DisplayDialog(
            "World Event System Ready",
            "Created/updated WorldEventManager + Dog & Cat Rain + Meteor Shower + Holy Light.\n\nNext: assign your 3D prefabs in Assets/WorldEvents (Gold Drop Prefab, Meteor Prefab, Holy Light Visual Prefab). Tune all chances, rounds, percentages and animation values in Inspector.",
            "OK");
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(EventFolder))
            AssetDatabase.CreateFolder("Assets", "WorldEvents");
    }

    private static WorldEventData GetOrCreateEvent(string fileName, string displayName, WorldEventRarity rarity, WorldEventType type)
    {
        string path = EventFolder + "/" + fileName;
        WorldEventData data = AssetDatabase.LoadAssetAtPath<WorldEventData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WorldEventData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.eventName = displayName;
        data.rarity = rarity;
        data.eventType = type;
        if (data.selectionWeight <= 0f) data.selectionWeight = 1f;
        return data;
    }

    private static Canvas FindBestCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas fallback = null;
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue;
            if (fallback == null) fallback = canvas;
            if (canvas.renderMode != RenderMode.WorldSpace) return canvas;
        }
        return fallback;
    }

    private static void SetupAnnouncementUI(WorldEventManager manager, Transform canvas)
    {
        Transform existing = canvas.Find("WorldEventAnnouncement");
        GameObject root = existing != null ? existing.gameObject : new GameObject("WorldEventAnnouncement", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(root, "Create World Event Announcement");
            root.transform.SetParent(canvas, false);
        }

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(760f, 230f);
        rect.anchoredPosition = Vector2.zero;

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.035f, 0.075f, 0.11f, 0.96f);
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        manager.announcementRoot = group;

        manager.rarityText = EnsureText(root.transform, "Rarity", new Vector2(0f, 75f), new Vector2(650f, 28f), 18f, FontStyles.Bold);
        manager.titleText = EnsureText(root.transform, "Title", new Vector2(0f, 25f), new Vector2(650f, 50f), 34f, FontStyles.Bold);
        manager.descriptionText = EnsureText(root.transform, "Description", new Vector2(0f, -48f), new Vector2(650f, 70f), 18f, FontStyles.Normal);

        Transform iconT = root.transform.Find("Icon");
        GameObject iconGo = iconT != null ? iconT.gameObject : new GameObject("Icon", typeof(RectTransform), typeof(Image));
        if (iconT == null) iconGo.transform.SetParent(root.transform, false);
        RectTransform ir = iconGo.GetComponent<RectTransform>();
        ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
        ir.pivot = new Vector2(0.5f, 0.5f);
        ir.sizeDelta = new Vector2(82f, 82f);
        ir.anchoredPosition = new Vector2(62f, 0f);
        manager.iconImage = iconGo.GetComponent<Image>();

        group.alpha = 0f;
        root.SetActive(false);
    }

    private static TMP_Text EnsureText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, FontStyles style)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (found == null) go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos + new Vector2(35f, 0f);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableWordWrapping = true;
        return text;
    }

    private static void AutoAssignHUDTargets(WorldEventManager manager, Transform canvas)
    {
        string[] names = { "ResourceHUD", "WaveHUD", "BuildDock", "UpgradePanelClean", "QoLTopRight", "RelicOwnedHUD", "QuestLiveHUD" };
        if (manager.hudTargets == null) manager.hudTargets = new List<WorldEventManager.HUDTarget>();
        if (manager.hudTargets.Count > 0) return;

        foreach (string name in names)
        {
            Transform t = FindDeepChild(canvas, name);
            RectTransform rect = t as RectTransform;
            if (rect == null) continue;
            Vector2 offset = rect.anchoredPosition.x < 0f ? new Vector2(-650f, 0f) : new Vector2(650f, 0f);
            if (Mathf.Abs(rect.anchoredPosition.y) > Mathf.Abs(rect.anchoredPosition.x))
                offset = rect.anchoredPosition.y > 0f ? new Vector2(0f, 350f) : new Vector2(0f, -350f);
            manager.hudTargets.Add(new WorldEventManager.HUDTarget { target = rect, hiddenOffset = offset });
        }
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChild(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
#endif
