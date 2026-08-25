#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click setup for TD QoL: 1x/2x/3x speed controls + queued relic-drop notification.
/// Existing gameplay UI is left untouched; this creates/updates a small top-right QoL canvas.
/// </summary>
public static class QoLAndRelicDropSetupTool
{
    [MenuItem("Tower Defense/UI/Setup Speed + Relic Drop HUD")]
    public static void Setup()
    {
        RelicManager relicManager = Object.FindAnyObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (relicManager == null)
        {
            EditorUtility.DisplayDialog("QoL Setup", "RelicManager was not found. Run Tower Defense > Relics > Setup Relic System first.", "OK");
            return;
        }

        Canvas canvas = FindOrCreateCanvas();
        Transform root = FindOrCreateRoot(canvas.transform);

        GameSpeedController speed = Object.FindAnyObjectByType<GameSpeedController>(FindObjectsInactive.Include);
        if (speed == null)
        {
            GameObject speedGO = new GameObject("GameSpeedController");
            Undo.RegisterCreatedObjectUndo(speedGO, "Create Game Speed Controller");
            speed = speedGO.AddComponent<GameSpeedController>();
        }

        BuildSpeedUI(root, speed);
        RelicRewardNotificationUI notification = BuildRelicNotification(root);

        Undo.RecordObject(relicManager, "Wire Relic Reward Notification");
        relicManager.rewardNotificationUI = notification;
        EditorUtility.SetDirty(relicManager);
        EditorUtility.SetDirty(speed);

        SeedSampleRarities();

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root.gameObject;
        EditorUtility.DisplayDialog(
            "QoL Setup",
            "Done.\n\nâ€¢ Added 1x / 2x / 3x speed controls.\nâ€¢ Added queued RELIC AVAILABLE notification.\nâ€¢ New buttons use the existing punch + hover feedback.\nâ€¢ EnemyData now controls each enemy's drop chance and boss guaranteed rare reward.\n\nRelic drops are collected by mouse hover, not click.",
            "OK");
    }

    private static Canvas FindOrCreateCanvas()
    {
        GameObject existing = GameObject.Find("QoLCanvas");
        if (existing != null && existing.TryGetComponent(out Canvas found)) return found;

        GameObject go = new GameObject("QoLCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create QoL Canvas");
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Transform FindOrCreateRoot(Transform canvas)
    {
        Transform existing = canvas.Find("QoLTopRight");
        if (existing != null) return existing;

        GameObject go = UIObject("QoLTopRight", canvas);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(420f, 180f);
        return go.transform;
    }

    private static void BuildSpeedUI(Transform root, GameSpeedController controller)
    {
        Transform old = root.Find("SpeedControls");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        GameObject panel = UIObject("SpeedControls", root);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(1f, 1f);
        pr.anchorMax = new Vector2(1f, 1f);
        pr.pivot = new Vector2(1f, 1f);
        pr.anchoredPosition = Vector2.zero;
        pr.sizeDelta = new Vector2(300f, 62f);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.035f, 0.05f, 0.075f, 0.93f);

        Button b1 = MakeButton(panel.transform, "Speed1x", "1x", new Vector2(-205f, -31f), new Vector2(82f, 46f));
        Button b2 = MakeButton(panel.transform, "Speed2x", "2x", new Vector2(-112f, -31f), new Vector2(82f, 46f));
        Button b3 = MakeButton(panel.transform, "Speed3x", "3x", new Vector2(-19f, -31f), new Vector2(82f, 46f));

        Undo.RecordObject(controller, "Wire Speed UI");
        controller.speed1Button = b1;
        controller.speed2Button = b2;
        controller.speed3Button = b3;
        controller.currentSpeedText = null;
        EditorUtility.SetDirty(controller);
    }

    private static RelicRewardNotificationUI BuildRelicNotification(Transform root)
    {
        Transform old = root.Find("RelicRewardNotification");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        GameObject container = UIObject("RelicRewardNotification", root);
        RectTransform cr = container.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(1f, 1f);
        cr.anchorMax = new Vector2(1f, 1f);
        cr.pivot = new Vector2(1f, 1f);
        cr.anchoredPosition = new Vector2(0f, -76f);
        cr.sizeDelta = new Vector2(300f, 74f);

        RelicRewardNotificationUI ui = container.AddComponent<RelicRewardNotificationUI>();
        ui.root = container;

        Button button = MakeButton(container.transform, "OpenRelicReward", "RELIC AVAILABLE", new Vector2(-150f, -37f), new Vector2(300f, 64f));
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.fontSize = 20f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.82f, 0.93f, 1f, 1f);
        }

        ui.openButton = button;
        ui.label = label;
        EditorUtility.SetDirty(ui);
        container.SetActive(false);
        return ui;
    }

    private static Button MakeButton(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = UIObject(name, parent);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = new Color(0.11f, 0.15f, 0.22f, 0.98f);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.84f, 0.90f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        if (go.GetComponent<UIPunchButton>() == null)
            go.AddComponent<UIPunchButton>();

        GameObject textGO = UIObject("Label", go.transform);
        RectTransform tr = textGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 19f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return button;
    }

    private static void SeedSampleRarities()
    {
        string[] guids = AssetDatabase.FindAssets("t:RelicData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);
            if (relic == null) continue;

            RelicRarity? seeded = null;
            switch (relic.name)
            {
                case "GoldenTouch": seeded = RelicRarity.Rare; break;
                case "SharpenedAmmo": seeded = RelicRarity.Rare; break;
                case "LongSight": seeded = RelicRarity.Rare; break;
                case "RapidMechanism": seeded = RelicRarity.Uncommon; break;
                case "EfficientUpgrade": seeded = RelicRarity.Uncommon; break;
                case "CheapFoundation": seeded = RelicRarity.Common; break;
            }

            if (seeded.HasValue && relic.rarity == RelicRarity.Common)
            {
                relic.rarity = seeded.Value;
                EditorUtility.SetDirty(relic);
            }
        }
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
}
#endif

