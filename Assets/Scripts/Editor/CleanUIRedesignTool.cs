#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click scene UI builder for the compact Infinitode-inspired layout approved for this project.
/// It does not replace gameplay systems. It creates a new visual layer, re-wires the existing
/// HUDManager / BuildMenuUI / TowerUpgradeUI references, and only hides the old visual controls.
/// Re-running only replaces the previously generated CleanUIRoot.
/// </summary>
public static class CleanUIRedesignTool
{
    private const string RootName = "CleanUIRoot";

    private static readonly Color Panel = Hex("#0B1722E8");
    private static readonly Color Panel2 = Hex("#102532EE");
    private static readonly Color Border = Hex("#315A68FF");
    private static readonly Color Cyan = Hex("#27D6F5FF");
    private static readonly Color CyanDim = Hex("#0D91B6FF");
    private static readonly Color Gold = Hex("#FFD34CFF");
    private static readonly Color Green = Hex("#55E86AFF");
    private static readonly Color Orange = Hex("#E89A3AFF");
    private static readonly Color Text = Hex("#EFF7FAFF");
    private static readonly Color Muted = Hex("#AFC2CAFF");

    [MenuItem("Tower Defense/UI/Apply Clean Infinitode Layout")]
    public static void Apply()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        HUDManager hud = Object.FindFirstObjectByType<HUDManager>();
        BuildMenuUI buildMenu = Object.FindFirstObjectByType<BuildMenuUI>();
        TowerUpgradeUI upgrade = Object.FindFirstObjectByType<TowerUpgradeUI>();

        if (canvas == null || hud == null || buildMenu == null || upgrade == null)
        {
            EditorUtility.DisplayDialog("Clean UI Redesign",
                "Không tìm thấy đủ Canvas, HUDManager, BuildMenuUI và TowerUpgradeUI trong scene hiện tại.\n\n" +
                "Hãy mở SampleScene rồi chạy lại lệnh này.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Apply Clean Tower Defense UI");

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform oldRoot = canvas.transform.Find(RootName);
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot.gameObject);

        // Cache old visuals so they can be hidden after the new references are assigned.
        var oldVisuals = new List<GameObject>();
        AddOld(oldVisuals, hud.goldText);
        AddOld(oldVisuals, hud.livesText);
        AddOld(oldVisuals, hud.waveText);
        AddOld(oldVisuals, hud.startWaveButton);
        if (buildMenu.towerButtons != null)
            foreach (var b in buildMenu.towerButtons) if (b != null) AddOld(oldVisuals, b.button);
        if (upgrade.panelRoot != null) oldVisuals.Add(upgrade.panelRoot);

        RectTransform root = Rect(RootName, canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        BuildResourceHUD(root, hud);
        BuildWaveHUD(root, hud);
        BuildDock(root, buildMenu);
        BuildUpgradePanel(root, upgrade);

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
            "Đã dựng UI mới và nối lại chức năng hiện tại.\n\n" +
            "Đã giữ nguyên gameplay logic; UI cũ chỉ bị ẩn, không bị xóa.\n" +
            "Bây giờ hãy bấm Play để kiểm tra Gold / Lives / Wave / Build / Upgrade / Sell / Close.", "OK");
    }

    private static void BuildResourceHUD(RectTransform root, HUDManager hud)
    {
        RectTransform box = PanelRect("ResourceHUD", root, new Vector2(24, -24), new Vector2(320, 142),
            new Vector2(0, 1), new Vector2(0, 1), Panel);

        CreateResourceRow(box, "GoldRow", 0, "◆", "Gold", out TMP_Text goldValue, Gold);
        CreateResourceRow(box, "LivesRow", 1, "♥", "Lives", out TMP_Text livesValue, Cyan);
        hud.goldText = goldValue;
        hud.livesText = livesValue;
    }

    private static void CreateResourceRow(RectTransform parent, string name, int index, string icon,
        string label, out TMP_Text value, Color valueColor)
    {
        float y = -8f - index * 64f;
        RectTransform row = PanelRect(name, parent, new Vector2(8, y), new Vector2(304, 58),
            new Vector2(0, 1), new Vector2(0, 1), Panel2);
        row.pivot = new Vector2(0, 1);

        TMP_Text iconText = TextEl("Icon", row, icon, 25, valueColor, TextAlignmentOptions.Center);
        SetRect(iconText.rectTransform, new Vector2(10, -6), new Vector2(46, 46), new Vector2(0, 1));
        iconText.raycastTarget = false;

        TMP_Text labelText = TextEl("Label", row, label, 22, Text, TextAlignmentOptions.MidlineLeft);
        SetRect(labelText.rectTransform, new Vector2(64, -5), new Vector2(110, 48), new Vector2(0, 1));
        labelText.raycastTarget = false;

        value = TextEl("Value", row, "0", 29, valueColor, TextAlignmentOptions.MidlineRight);
        SetRect(value.rectTransform, new Vector2(178, -5), new Vector2(112, 48), new Vector2(0, 1));
        value.fontStyle = FontStyles.Bold;
        value.raycastTarget = false;
    }

    private static void BuildWaveHUD(RectTransform root, HUDManager hud)
    {
        RectTransform wrap = Rect("WaveHUD", root, new Vector2(0, -24), new Vector2(500, 145),
            new Vector2(.5f, 1), new Vector2(.5f, 1));
        wrap.pivot = new Vector2(.5f, 1);

        RectTransform header = PanelRect("WaveHeader", wrap, Vector2.zero, new Vector2(500, 60),
            new Vector2(.5f, 1), new Vector2(.5f, 1), Panel);
        header.pivot = new Vector2(.5f, 1);

        TMP_Text wave = TextEl("WaveValue", header, "Wave 1 / 10", 28, Text, TextAlignmentOptions.Center);
        Stretch(wave.rectTransform, 12);
        wave.fontStyle = FontStyles.Bold;
        wave.raycastTarget = false;
        hud.waveText = wave;

        Button start = ButtonEl("StartWaveButton", wrap, "▶  START WAVE", CyanDim, Cyan,
            new Vector2(390, 62));
        SetRect(start.GetComponent<RectTransform>(), new Vector2(0, -72), new Vector2(390, 62), new Vector2(.5f, 1));
        start.GetComponent<RectTransform>().pivot = new Vector2(.5f, 1);
        hud.startWaveButton = start;
    }

    private static void BuildDock(RectTransform root, BuildMenuUI buildMenu)
    {
        int count = buildMenu.towerButtons != null ? buildMenu.towerButtons.Length : 0;
        float cardW = 116f;
        float spacing = 8f;
        float width = Mathf.Clamp(28 + count * cardW + Mathf.Max(0, count - 1) * spacing, 360, 1180);

        RectTransform dock = PanelRect("BuildDock", root, new Vector2(0, 20), new Vector2(width, 174),
            new Vector2(.5f, 0), new Vector2(.5f, 0), Panel);
        dock.pivot = new Vector2(.5f, 0);

        HorizontalLayoutGroup layout = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (buildMenu.towerButtons == null) return;

        for (int i = 0; i < buildMenu.towerButtons.Length; i++)
        {
            BuildMenuUI.TowerButtonBinding binding = buildMenu.towerButtons[i];
            if (binding == null || binding.towerData == null) continue;

            RectTransform card = PanelRect($"TowerCard_{i + 1}", dock, Vector2.zero, new Vector2(cardW, 150),
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), Panel2);
            LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = cardW;
            le.preferredHeight = 150;

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.82f, 0.97f, 1f, 1f);
            cb.pressedColor = new Color(0.55f, 0.87f, 0.94f, 1f);
            cb.disabledColor = new Color(0.45f, 0.5f, 0.52f, 0.65f);
            button.colors = cb;

            TMP_Text number = TextEl("Hotkey", card, (i + 1).ToString(), 14, Muted, TextAlignmentOptions.Center);
            SetRect(number.rectTransform, new Vector2(7, -7), new Vector2(22, 22), new Vector2(0, 1));
            number.raycastTarget = false;

            TMP_Text icon = TextEl("Icon", card, TowerGlyph(binding.towerData.towerName), 35, Cyan, TextAlignmentOptions.Center);
            SetRect(icon.rectTransform, new Vector2(0, -30), new Vector2(92, 54), new Vector2(.5f, 1));
            icon.rectTransform.pivot = new Vector2(.5f, 1);
            icon.raycastTarget = false;

            TMP_Text name = TextEl("Name", card, binding.towerData.towerName, 15, Text, TextAlignmentOptions.Center);
            SetRect(name.rectTransform, new Vector2(0, -89), new Vector2(105, 28), new Vector2(.5f, 1));
            name.rectTransform.pivot = new Vector2(.5f, 1);
            name.enableAutoSizing = true;
            name.fontSizeMin = 10;
            name.fontSizeMax = 15;
            name.raycastTarget = false;

            TMP_Text cost = TextEl("Cost", card, binding.towerData.buildCost.ToString(), 17, Gold, TextAlignmentOptions.Center);
            SetRect(cost.rectTransform, new Vector2(0, -119), new Vector2(100, 24), new Vector2(.5f, 1));
            cost.rectTransform.pivot = new Vector2(.5f, 1);
            cost.text = $"◆ {binding.towerData.buildCost}";
            cost.raycastTarget = false;

            RectTransform selected = Rect("SelectedFrame", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            selected.offsetMin = new Vector2(-2, -2);
            selected.offsetMax = new Vector2(2, 2);
            Image selectedImage = selected.gameObject.AddComponent<Image>();
            selectedImage.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.09f);
            selectedImage.raycastTarget = false;
            Outline selectedOutline = selected.gameObject.AddComponent<Outline>();
            selectedOutline.effectColor = Cyan;
            selectedOutline.effectDistance = new Vector2(2, -2);
            selected.gameObject.SetActive(false);

            binding.button = button;
            binding.label = null;
            binding.nameText = name;
            binding.costText = cost;
            binding.selectedFrame = selected.gameObject;
        }
    }

    private static void BuildUpgradePanel(RectTransform root, TowerUpgradeUI upgrade)
    {
        RectTransform panel = PanelRect("UpgradePanelClean", root, new Vector2(-24, 0), new Vector2(430, 700),
            new Vector2(1, .5f), new Vector2(1, .5f), Panel);
        panel.pivot = new Vector2(1, .5f);
        upgrade.panelRoot = panel.gameObject;

        TMP_Text title = TextEl("TowerName", panel, "Archer Tower", 29, Text, TextAlignmentOptions.MidlineLeft);
        SetRect(title.rectTransform, new Vector2(28, -22), new Vector2(310, 44), new Vector2(0, 1));
        title.fontStyle = FontStyles.Bold;
        title.raycastTarget = false;
        upgrade.towerNameText = title;

        Button close = ButtonEl("CloseButton", panel, "×", Panel2, Border, new Vector2(42, 42));
        SetRect(close.GetComponent<RectTransform>(), new Vector2(-18, -18), new Vector2(42, 42), new Vector2(1, 1));
        close.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
        upgrade.closeButton = close;

        TMP_Text level = TextEl("Level", panel, "Level 1", 19, Muted, TextAlignmentOptions.MidlineLeft);
        SetRect(level.rectTransform, new Vector2(28, -70), new Vector2(250, 30), new Vector2(0, 1));
        level.raycastTarget = false;
        upgrade.levelText = level;

        RectTransform current = Section(panel, "CurrentStats", new Vector2(18, -116), new Vector2(394, 180), "CURRENT STATS");
        upgrade.strengthText = StatRow(current, "Damage", 50, "⚔", out _);
        upgrade.attackSpeedText = StatRow(current, "Attack Speed", 91, "◷", out _);
        upgrade.rangeText = StatRow(current, "Range", 132, "◎", out _);

        RectTransform next = Section(panel, "NextLevel", new Vector2(18, -306), new Vector2(394, 180), "NEXT LEVEL (2)");
        upgrade.nextLevelRoot = next.gameObject;
        upgrade.nextLevelTitleText = next.Find("SectionTitle").GetComponent<TMP_Text>();
        upgrade.nextStrengthText = StatRow(next, "Damage", 50, "⚔", out _);
        upgrade.nextAttackSpeedText = StatRow(next, "Attack Speed", 91, "◷", out _);
        upgrade.nextRangeText = StatRow(next, "Range", 132, "◎", out _);

        RectTransform costBox = PanelRect("UpgradeCostBox", panel, new Vector2(18, -496), new Vector2(394, 74),
            new Vector2(0, 1), new Vector2(0, 1), Panel2);
        costBox.pivot = new Vector2(0, 1);
        TMP_Text costLabel = TextEl("CostLabel", costBox, "UPGRADE COST", 13, Cyan, TextAlignmentOptions.MidlineLeft);
        SetRect(costLabel.rectTransform, new Vector2(14, -8), new Vector2(150, 24), new Vector2(0, 1));
        costLabel.raycastTarget = false;
        TMP_Text cost = TextEl("CostValue", costBox, "◆ 120", 25, Gold, TextAlignmentOptions.MidlineRight);
        SetRect(cost.rectTransform, new Vector2(190, -18), new Vector2(182, 40), new Vector2(0, 1));
        cost.raycastTarget = false;
        upgrade.upgradeCostText = cost;

        Button upgradeButton = ButtonEl("UpgradeButton", panel, "UPGRADE", CyanDim, Cyan, new Vector2(170, 56));
        SetRect(upgradeButton.GetComponent<RectTransform>(), new Vector2(18, -590), new Vector2(170, 56), new Vector2(0, 1));
        upgradeButton.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        upgrade.upgradeButton = upgradeButton;
        upgrade.upgradeButtonLabel = upgradeButton.GetComponentInChildren<TMP_Text>();

        Button sell = ButtonEl("SellButton", panel, "SELL", new Color(0.26f, 0.16f, 0.08f, 1f), Orange, new Vector2(120, 56));
        SetRect(sell.GetComponent<RectTransform>(), new Vector2(198, -590), new Vector2(120, 56), new Vector2(0, 1));
        sell.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        upgrade.sellButton = sell;
        upgrade.sellButtonLabel = sell.GetComponentInChildren<TMP_Text>();

        Button close2 = ButtonEl("CloseBottomButton", panel, "CLOSE", Panel2, Border, new Vector2(86, 56));
        SetRect(close2.GetComponent<RectTransform>(), new Vector2(328, -590), new Vector2(86, 56), new Vector2(0, 1));
        close2.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        // Keep the top X as the wired close button; mirror the same action through a tiny relay.
        CleanUICloseRelay relay = close2.gameObject.AddComponent<CleanUICloseRelay>();
        relay.target = close;

        panel.gameObject.SetActive(false);
    }

    private static RectTransform Section(RectTransform parent, string name, Vector2 pos, Vector2 size, string title)
    {
        RectTransform section = PanelRect(name, parent, pos, size, new Vector2(0, 1), new Vector2(0, 1), Panel2);
        section.pivot = new Vector2(0, 1);
        TMP_Text t = TextEl("SectionTitle", section, title, 13, Cyan, TextAlignmentOptions.MidlineLeft);
        SetRect(t.rectTransform, new Vector2(14, -7), new Vector2(250, 26), new Vector2(0, 1));
        t.raycastTarget = false;
        return section;
    }

    private static TMP_Text StatRow(RectTransform parent, string label, float y, string glyph, out TMP_Text labelText)
    {
        TMP_Text icon = TextEl(label + "Icon", parent, glyph, 18, Muted, TextAlignmentOptions.Center);
        SetRect(icon.rectTransform, new Vector2(14, -y), new Vector2(28, 30), new Vector2(0, 1));
        icon.raycastTarget = false;

        labelText = TextEl(label + "Label", parent, label, 18, Muted, TextAlignmentOptions.MidlineLeft);
        SetRect(labelText.rectTransform, new Vector2(50, -y), new Vector2(165, 30), new Vector2(0, 1));
        labelText.raycastTarget = false;

        TMP_Text value = TextEl(label + "Value", parent, "0", 18, Text, TextAlignmentOptions.MidlineRight);
        SetRect(value.rectTransform, new Vector2(215, -y), new Vector2(158, 30), new Vector2(0, 1));
        value.raycastTarget = false;
        return value;
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

    private static Button ButtonEl(string name, Transform parent, string text, Color bg, Color border, Vector2 size)
    {
        RectTransform rt = Rect(name, parent, Vector2.zero, size, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = bg;
        image.raycastTarget = true;
        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(2, -2);

        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = image;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, .9f);
        cb.pressedColor = new Color(.78f, .9f, .94f, 1f);
        cb.disabledColor = new Color(.42f, .46f, .48f, .65f);
        b.colors = cb;

        TMP_Text label = TextEl("Label", rt, text, 19, Text, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 6);
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
        return b;
    }

    private static RectTransform PanelRect(string name, Transform parent, Vector2 pos, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        RectTransform rt = Rect(name, parent, pos, size, anchorMin, anchorMax);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(1, -1);
        return rt;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create clean UI");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    private static TMP_Text TextEl(string name, Transform parent, string text, float fontSize,
        Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create clean UI text");
        go.transform.SetParent(parent, false);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static void SetRect(RectTransform rt, Vector2 pos, Vector2 size, Vector2 anchor)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void AddOld(List<GameObject> list, Component component)
    {
        if (component != null && !list.Contains(component.gameObject)) list.Add(component.gameObject);
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}

/// <summary>Editor-generated helper: bottom Close button invokes the same wired top X button.</summary>
public class CleanUICloseRelay : MonoBehaviour
{
    public Button target;
    private Button self;

    private void Awake()
    {
        self = GetComponent<Button>();
        if (self != null) self.onClick.AddListener(() => target?.onClick.Invoke());
    }
}
#endif
