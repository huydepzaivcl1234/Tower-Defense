#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CleanUIRedesignToolV2
{
    private const string RootName = "CleanUIRoot";

    private static readonly Color Panel = Hex("#0B1722E8");
    private static readonly Color Panel2 = Hex("#102532EE");
    private static readonly Color Border = Hex("#315A68FF");
    private static readonly Color Cyan = Hex("#27D6F5FF");
    private static readonly Color CyanDim = Hex("#0D91B6FF");
    private static readonly Color Gold = Hex("#FFD34CFF");
    private static readonly Color Orange = Hex("#E89A3AFF");
    private static readonly Color Text = Hex("#EFF7FAFF");
    private static readonly Color Muted = Hex("#AFC2CAFF");

    [MenuItem("Tower Defense/UI/Apply Clean Infinitode Layout")]
    public static void Apply()
    {
        Canvas canvas = FindSceneObject<Canvas>();
        HUDManager hud = FindSceneObject<HUDManager>();
        BuildMenuUI buildMenu = FindSceneObject<BuildMenuUI>();
        TowerUpgradeUI upgrade = FindSceneObject<TowerUpgradeUI>();

        if (canvas == null || hud == null || buildMenu == null || upgrade == null)
        {
            EditorUtility.DisplayDialog("Clean UI Redesign",
                "Không tìm thấy đủ Canvas, HUDManager, BuildMenuUI và TowerUpgradeUI.\nHãy mở SampleScene rồi chạy lại.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Apply Clean Tower Defense UI");

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform previous = canvas.transform.Find(RootName);
        if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);

        var oldVisuals = new List<GameObject>();
        AddOld(oldVisuals, hud.goldText);
        AddOld(oldVisuals, hud.livesText);
        AddOld(oldVisuals, hud.waveText);
        AddOld(oldVisuals, hud.startWaveButton);
        if (buildMenu.towerButtons != null)
            foreach (var b in buildMenu.towerButtons) if (b != null) AddOld(oldVisuals, b.button);
        if (upgrade.panelRoot != null) oldVisuals.Add(upgrade.panelRoot);

        RectTransform root = Rect(RootName, canvas.transform, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        BuildResources(root, hud);
        BuildWave(root, hud);
        BuildDock(root, buildMenu);
        BuildUpgrade(root, upgrade);

        foreach (GameObject go in oldVisuals)
        {
            if (go == null || go.transform.IsChildOf(root)) continue;
            go.SetActive(false);
        }

        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(buildMenu);
        EditorUtility.SetDirty(upgrade);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;

        EditorUtility.DisplayDialog("Clean UI Redesign",
            "Đã dựng CleanUIRoot và nối lại chức năng hiện tại. UI cũ chỉ bị ẩn, không bị xóa.\n\n" +
            "Bấm Play để test Gold, Lives, Wave, Build, Upgrade, Sell và Close.", "OK");
    }

    private static void BuildResources(RectTransform root, HUDManager hud)
    {
        RectTransform box = PanelRect("ResourceHUD", root, new Vector2(24, -24), new Vector2(320, 142), new Vector2(0, 1), Panel);
        CreateResourceRow(box, "GoldRow", 0, "◆", "Gold", out TMP_Text goldValue, Gold);
        CreateResourceRow(box, "LivesRow", 1, "♥", "Lives", out TMP_Text livesValue, Cyan);
        hud.goldText = goldValue;
        hud.livesText = livesValue;
    }

    private static void CreateResourceRow(RectTransform parent, string name, int index, string icon, string label,
        out TMP_Text value, Color valueColor)
    {
        RectTransform row = PanelRect(name, parent, new Vector2(8, -8 - index * 64), new Vector2(304, 58), new Vector2(0, 1), Panel2);
        row.pivot = new Vector2(0, 1);

        TMP_Text i = TextEl("Icon", row, icon, 25, valueColor, TextAlignmentOptions.Center);
        Place(i.rectTransform, new Vector2(10, -6), new Vector2(46, 46), new Vector2(0, 1));
        TMP_Text l = TextEl("Label", row, label, 22, Text, TextAlignmentOptions.MidlineLeft);
        Place(l.rectTransform, new Vector2(64, -5), new Vector2(110, 48), new Vector2(0, 1));
        value = TextEl("Value", row, "0", 29, valueColor, TextAlignmentOptions.MidlineRight);
        Place(value.rectTransform, new Vector2(178, -5), new Vector2(112, 48), new Vector2(0, 1));
        value.fontStyle = FontStyles.Bold;
    }

    private static void BuildWave(RectTransform root, HUDManager hud)
    {
        RectTransform wrap = Rect("WaveHUD", root, new Vector2(0, -24), new Vector2(500, 145), new Vector2(.5f, 1));
        wrap.pivot = new Vector2(.5f, 1);

        RectTransform header = PanelRect("WaveHeader", wrap, Vector2.zero, new Vector2(500, 60), new Vector2(.5f, 1), Panel);
        header.pivot = new Vector2(.5f, 1);
        TMP_Text wave = TextEl("WaveValue", header, "Wave 1 / 10", 28, Text, TextAlignmentOptions.Center);
        Stretch(wave.rectTransform, 12);
        wave.fontStyle = FontStyles.Bold;
        hud.waveText = wave;

        Button start = ButtonEl("StartWaveButton", wrap, "▶  START WAVE", CyanDim, Cyan);
        Place(start.GetComponent<RectTransform>(), new Vector2(0, -72), new Vector2(390, 62), new Vector2(.5f, 1));
        start.GetComponent<RectTransform>().pivot = new Vector2(.5f, 1);
        hud.startWaveButton = start;
    }

    private static void BuildDock(RectTransform root, BuildMenuUI buildMenu)
    {
        int count = buildMenu.towerButtons != null ? buildMenu.towerButtons.Length : 0;
        float cardW = 116f, spacing = 8f;
        float width = Mathf.Clamp(28 + count * cardW + Mathf.Max(0, count - 1) * spacing, 360, 1180);
        RectTransform dock = PanelRect("BuildDock", root, new Vector2(0, 20), new Vector2(width, 174), new Vector2(.5f, 0), Panel);
        dock.pivot = new Vector2(.5f, 0);

        HorizontalLayoutGroup layout = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        if (buildMenu.towerButtons == null) return;
        for (int i = 0; i < buildMenu.towerButtons.Length; i++)
        {
            BuildMenuUI.TowerButtonBinding binding = buildMenu.towerButtons[i];
            if (binding == null || binding.towerData == null) continue;

            RectTransform card = PanelRect($"TowerCard_{i + 1}", dock, Vector2.zero, new Vector2(cardW, 150), new Vector2(.5f, .5f), Panel2);
            LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = cardW; le.preferredHeight = 150;

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            ColorBlock cb = button.colors;
            cb.highlightedColor = new Color(.82f, .97f, 1f, 1f);
            cb.pressedColor = new Color(.55f, .87f, .94f, 1f);
            cb.disabledColor = new Color(.42f, .46f, .48f, .65f);
            button.colors = cb;

            TMP_Text num = TextEl("Hotkey", card, (i + 1).ToString(), 14, Muted, TextAlignmentOptions.Center);
            Place(num.rectTransform, new Vector2(7, -7), new Vector2(22, 22), new Vector2(0, 1));
            TMP_Text icon = TextEl("Icon", card, TowerGlyph(binding.towerData.towerName), 35, Cyan, TextAlignmentOptions.Center);
            Place(icon.rectTransform, new Vector2(0, -30), new Vector2(92, 54), new Vector2(.5f, 1)); icon.rectTransform.pivot = new Vector2(.5f, 1);
            TMP_Text name = TextEl("Name", card, binding.towerData.towerName, 15, Text, TextAlignmentOptions.Center);
            Place(name.rectTransform, new Vector2(0, -89), new Vector2(105, 28), new Vector2(.5f, 1)); name.rectTransform.pivot = new Vector2(.5f, 1);
            name.enableAutoSizing = true; name.fontSizeMin = 10; name.fontSizeMax = 15;
            TMP_Text cost = TextEl("Cost", card, $"◆ {binding.towerData.buildCost}", 17, Gold, TextAlignmentOptions.Center);
            Place(cost.rectTransform, new Vector2(0, -119), new Vector2(100, 24), new Vector2(.5f, 1)); cost.rectTransform.pivot = new Vector2(.5f, 1);

            RectTransform selected = Rect("SelectedFrame", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one);
            selected.offsetMin = new Vector2(-2, -2); selected.offsetMax = new Vector2(2, 2);
            Image selImage = selected.gameObject.AddComponent<Image>();
            selImage.color = new Color(Cyan.r, Cyan.g, Cyan.b, .08f); selImage.raycastTarget = false;
            Outline selOutline = selected.gameObject.AddComponent<Outline>();
            selOutline.effectColor = Cyan; selOutline.effectDistance = new Vector2(2, -2);
            selected.gameObject.SetActive(false);

            binding.button = button;
            binding.label = null;
            binding.nameText = name;
            binding.costText = cost;
            binding.selectedFrame = selected.gameObject;
        }
    }

    private static void BuildUpgrade(RectTransform root, TowerUpgradeUI upgrade)
    {
        RectTransform panel = PanelRect("UpgradePanelClean", root, new Vector2(-24, 0), new Vector2(430, 700), new Vector2(1, .5f), Panel);
        panel.pivot = new Vector2(1, .5f);
        upgrade.panelRoot = panel.gameObject;

        TMP_Text title = TextEl("TowerName", panel, "Archer Tower", 29, Text, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, new Vector2(28, -22), new Vector2(310, 44), new Vector2(0, 1)); title.fontStyle = FontStyles.Bold;
        upgrade.towerNameText = title;

        Button close = ButtonEl("CloseButton", panel, "×", Panel2, Border);
        Place(close.GetComponent<RectTransform>(), new Vector2(-18, -18), new Vector2(42, 42), new Vector2(1, 1)); close.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
        upgrade.closeButton = close;

        TMP_Text level = TextEl("Level", panel, "Level 1", 19, Muted, TextAlignmentOptions.MidlineLeft);
        Place(level.rectTransform, new Vector2(28, -70), new Vector2(250, 30), new Vector2(0, 1)); upgrade.levelText = level;

        RectTransform current = Section(panel, "CurrentStats", new Vector2(18, -116), "CURRENT STATS");
        upgrade.strengthText = StatRow(current, "Damage", 50, "⚔");
        upgrade.attackSpeedText = StatRow(current, "Attack Speed", 91, "◷");
        upgrade.rangeText = StatRow(current, "Range", 132, "◎");

        RectTransform next = Section(panel, "NextLevel", new Vector2(18, -306), "NEXT LEVEL (2)");
        upgrade.nextLevelRoot = next.gameObject;
        upgrade.nextLevelTitleText = next.Find("SectionTitle").GetComponent<TMP_Text>();
        upgrade.nextStrengthText = StatRow(next, "Damage", 50, "⚔");
        upgrade.nextAttackSpeedText = StatRow(next, "Attack Speed", 91, "◷");
        upgrade.nextRangeText = StatRow(next, "Range", 132, "◎");

        RectTransform costBox = PanelRect("UpgradeCostBox", panel, new Vector2(18, -496), new Vector2(394, 74), new Vector2(0, 1), Panel2);
        costBox.pivot = new Vector2(0, 1);
        TMP_Text costLabel = TextEl("CostLabel", costBox, "UPGRADE COST", 13, Cyan, TextAlignmentOptions.MidlineLeft);
        Place(costLabel.rectTransform, new Vector2(14, -8), new Vector2(150, 24), new Vector2(0, 1));
        TMP_Text cost = TextEl("CostValue", costBox, "120", 25, Gold, TextAlignmentOptions.MidlineRight);
        Place(cost.rectTransform, new Vector2(190, -18), new Vector2(182, 40), new Vector2(0, 1)); upgrade.upgradeCostText = cost;

        Button up = ButtonEl("UpgradeButton", panel, "UPGRADE", CyanDim, Cyan);
        Place(up.GetComponent<RectTransform>(), new Vector2(18, -590), new Vector2(170, 56), new Vector2(0, 1)); up.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        upgrade.upgradeButton = up; upgrade.upgradeButtonLabel = up.GetComponentInChildren<TMP_Text>();

        Button sell = ButtonEl("SellButton", panel, "SELL", new Color(.26f, .16f, .08f, 1f), Orange);
        Place(sell.GetComponent<RectTransform>(), new Vector2(198, -590), new Vector2(120, 56), new Vector2(0, 1)); sell.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        upgrade.sellButton = sell; upgrade.sellButtonLabel = sell.GetComponentInChildren<TMP_Text>();

        Button closeBottom = ButtonEl("CloseBottomButton", panel, "CLOSE", Panel2, Border);
        Place(closeBottom.GetComponent<RectTransform>(), new Vector2(328, -590), new Vector2(86, 56), new Vector2(0, 1)); closeBottom.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        upgrade.secondaryCloseButton = closeBottom;

        panel.gameObject.SetActive(false);
    }

    private static RectTransform Section(RectTransform parent, string name, Vector2 pos, string title)
    {
        RectTransform section = PanelRect(name, parent, pos, new Vector2(394, 180), new Vector2(0, 1), Panel2);
        section.pivot = new Vector2(0, 1);
        TMP_Text t = TextEl("SectionTitle", section, title, 13, Cyan, TextAlignmentOptions.MidlineLeft);
        Place(t.rectTransform, new Vector2(14, -7), new Vector2(250, 26), new Vector2(0, 1));
        return section;
    }

    private static TMP_Text StatRow(RectTransform parent, string label, float y, string glyph)
    {
        TMP_Text icon = TextEl(label + "Icon", parent, glyph, 18, Muted, TextAlignmentOptions.Center);
        Place(icon.rectTransform, new Vector2(14, -y), new Vector2(28, 30), new Vector2(0, 1));
        TMP_Text labelText = TextEl(label + "Label", parent, label, 18, Muted, TextAlignmentOptions.MidlineLeft);
        Place(labelText.rectTransform, new Vector2(50, -y), new Vector2(165, 30), new Vector2(0, 1));
        TMP_Text value = TextEl(label + "Value", parent, "0", 18, Text, TextAlignmentOptions.MidlineRight);
        Place(value.rectTransform, new Vector2(215, -y), new Vector2(158, 30), new Vector2(0, 1));
        return value;
    }

    private static Button ButtonEl(string name, Transform parent, string text, Color bg, Color border)
    {
        RectTransform rt = Rect(name, parent, Vector2.zero, new Vector2(120, 50), new Vector2(.5f, .5f));
        Image img = rt.gameObject.AddComponent<Image>(); img.color = bg; img.raycastTarget = true;
        Outline outline = rt.gameObject.AddComponent<Outline>(); outline.effectColor = border; outline.effectDistance = new Vector2(2, -2);
        Button button = rt.gameObject.AddComponent<Button>(); button.targetGraphic = img;
        TMP_Text label = TextEl("Label", rt, text, 19, Text, TextAlignmentOptions.Center); Stretch(label.rectTransform, 6); label.fontStyle = FontStyles.Bold;
        return button;
    }

    private static RectTransform PanelRect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchor, Color color)
    {
        RectTransform rt = Rect(name, parent, pos, size, anchor);
        Image img = rt.gameObject.AddComponent<Image>(); img.color = color; img.raycastTarget = false;
        Outline outline = rt.gameObject.AddComponent<Outline>(); outline.effectColor = Border; outline.effectDistance = new Vector2(1, -1);
        return rt;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchor)
        => Rect(name, parent, pos, size, anchor, anchor);

    private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create clean UI");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.sizeDelta = size; rt.anchoredPosition = pos;
        return rt;
    }

    private static TMP_Text TextEl(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create clean UI text");
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color; tmp.alignment = align; tmp.enableWordWrapping = false; tmp.raycastTarget = false;
        return tmp;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size, Vector2 anchor)
    {
        rt.anchorMin = rt.anchorMax = anchor; rt.sizeDelta = size; rt.anchoredPosition = pos;
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void AddOld(List<GameObject> list, Component c)
    {
        if (c != null && !list.Contains(c.gameObject)) list.Add(c.gameObject);
    }

    private static T FindSceneObject<T>() where T : Object
    {
        foreach (T obj in Resources.FindObjectsOfTypeAll<T>())
        {
            if (obj == null || EditorUtility.IsPersistent(obj)) continue;
            Component c = obj as Component;
            if (c != null && c.gameObject.scene.IsValid()) return obj;
            GameObject go = obj as GameObject;
            if (go != null && go.scene.IsValid()) return obj;
        }
        return null;
    }

    private static string TowerGlyph(string name)
    {
        string n = (name ?? string.Empty).ToLowerInvariant();
        if (n.Contains("gold") || n.Contains("mine")) return "◆";
        if (n.Contains("bomb")) return "●";
        if (n.Contains("burn") || n.Contains("fire")) return "♨";
        if (n.Contains("cannon")) return "◉";
        if (n.Contains("arch") || n.Contains("bow")) return "➶";
        return "▲";
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
#endif
