#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CleanUIRedesignToolV3
{
    private const string RootName = "CleanUIRoot";

    private static readonly Color Deep = Hex("#06131FEF");
    private static readonly Color Panel = Hex("#0A2030F2");
    private static readonly Color Panel2 = Hex("#0D2B3BF5");
    private static readonly Color Card = Hex("#102F42F5");
    private static readonly Color Border = Hex("#1E7594FF");
    private static readonly Color Cyan = Hex("#39E5FFFF");
    private static readonly Color Cyan2 = Hex("#13BFE6FF");
    private static readonly Color Blue = Hex("#247CFFFF");
    private static readonly Color Gold = Hex("#FFD45CFF");
    private static readonly Color Orange = Hex("#F0A341FF");
    private static readonly Color Green = Hex("#56E68AFF");
    private static readonly Color Red = Hex("#FF5A6BFF");
    private static readonly Color Text = Hex("#F4FBFFFF");
    private static readonly Color Muted = Hex("#9FC2D0FF");

    [MenuItem("Tower Defense/UI/Apply Aurora Gameplay UI")]
    public static void Apply()
    {
        ApplyGameplay(true);
    }

    public static bool ApplyGameplay(bool showDialog)
    {
        HUDManager hud = FindSceneObject<HUDManager>();
        if (hud == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Aurora Gameplay UI", "Could not find HUDManager in the open scene.", "OK");
            return false;
        }

        Canvas canvas = hud.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Aurora Gameplay UI", "HUDManager is not under a Canvas.", "OK");
            return false;
        }

        BuildMenuUI buildMenu = canvas.GetComponentInChildren<BuildMenuUI>(true);
        TowerUpgradeUI upgrade = canvas.GetComponentInChildren<TowerUpgradeUI>(true);
        GameSpeedController speed = FindSceneObject<GameSpeedController>();

        if (buildMenu == null || upgrade == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog(
                    "Aurora Gameplay UI",
                    "Could not find BuildMenuUI and TowerUpgradeUI inside the same gameplay Canvas as HUDManager. Nothing was changed.",
                    "OK");
            return false;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Apply Aurora Gameplay UI");

        DestroyGeneratedRoots(canvas.gameObject.scene);

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform previous = canvas.transform.Find(RootName);
        if (previous != null)
            Undo.DestroyObjectImmediate(previous.gameObject);

        List<GameObject> oldVisuals = new List<GameObject>();
        AddOld(oldVisuals, hud.goldText);
        AddOld(oldVisuals, hud.livesText);
        AddOld(oldVisuals, hud.waveText);
        AddOld(oldVisuals, hud.startWaveButton);

        if (buildMenu.towerButtons != null)
        {
            foreach (BuildMenuUI.TowerButtonBinding binding in buildMenu.towerButtons)
                if (binding != null)
                    AddOld(oldVisuals, binding.button);
        }

        if (upgrade.panelRoot != null)
            oldVisuals.Add(upgrade.panelRoot);

        RectTransform root = Rect(RootName, canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        BuildResources(root, hud);
        BuildWave(root, hud);
        BuildDock(root, buildMenu);
        BuildUpgrade(root, upgrade);

        if (speed != null)
            StyleSpeedSelector(speed);

        foreach (GameObject go in oldVisuals)
        {
            if (go == null || go.transform.IsChildOf(root))
                continue;
            go.SetActive(false);
        }

        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(buildMenu);
        EditorUtility.SetDirty(upgrade);
        if (speed != null) EditorUtility.SetDirty(speed);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Aurora Gameplay UI",
                "Aurora gameplay UI applied to the Canvas that owns HUDManager. Wrong/generated CleanUIRoot objects were removed first.\n\nTest Gold, Lives, Wave, Build, Upgrade, Sell, Close and speed buttons in Play Mode.",
                "OK");
        }

        return true;
    }

    private static void DestroyGeneratedRoots(UnityEngine.SceneManagement.Scene scene)
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = all.Length - 1; i >= 0; i--)
        {
            Transform t = all[i];
            if (t == null || t.name != RootName || !t.gameObject.scene.IsValid() || t.gameObject.scene != scene)
                continue;

            Undo.DestroyObjectImmediate(t.gameObject);
        }
    }

    private static void BuildResources(RectTransform root, HUDManager hud)
    {
        RectTransform box = AuroraPanel("ResourceHUD", root, new Vector2(24, -24), new Vector2(312, 146), new Vector2(0, 1), Deep);
        box.pivot = new Vector2(0, 1);

        AddTopAccent(box, Cyan, 3f);
        CreateResourceRow(box, "GoldRow", 0, "G", "GOLD", out TMP_Text goldValue, Gold);
        CreateResourceRow(box, "LivesRow", 1, "HP", "LIVES", out TMP_Text livesValue, Red);

        hud.goldText = goldValue;
        hud.livesText = livesValue;
    }

    private static void CreateResourceRow(RectTransform parent, string name, int index, string icon, string label, out TMP_Text value, Color valueColor)
    {
        RectTransform row = AuroraPanel(name, parent, new Vector2(9, -12 - index * 63), new Vector2(294, 56), new Vector2(0, 1), Panel2);
        row.pivot = new Vector2(0, 1);

        RectTransform iconBox = PanelRect("IconBox", row, new Vector2(8, -7), new Vector2(42, 42), new Vector2(0, 1), new Color(valueColor.r, valueColor.g, valueColor.b, 0.12f));
        iconBox.pivot = new Vector2(0, 1);
        TMP_Text iconText = TextEl("Icon", iconBox, icon, icon.Length > 1 ? 14 : 22, valueColor, TextAlignmentOptions.Center);
        Stretch(iconText.rectTransform, 2);
        iconText.fontStyle = FontStyles.Bold;

        TMP_Text labelText = TextEl("Label", row, label, 13, Muted, TextAlignmentOptions.MidlineLeft);
        Place(labelText.rectTransform, new Vector2(60, -8), new Vector2(90, 18), new Vector2(0, 1));
        labelText.fontStyle = FontStyles.Bold;

        value = TextEl("Value", row, "0", 26, valueColor, TextAlignmentOptions.MidlineRight);
        Place(value.rectTransform, new Vector2(144, -7), new Vector2(136, 40), new Vector2(0, 1));
        value.fontStyle = FontStyles.Bold;
    }

    private static void BuildWave(RectTransform root, HUDManager hud)
    {
        RectTransform wrap = Rect("WaveHUD", root, Vector2.zero, new Vector2(520, 150), new Vector2(.5f, 1));
        wrap.anchoredPosition = new Vector2(0, -18);
        wrap.pivot = new Vector2(.5f, 1);

        RectTransform header = AuroraPanel("WaveHeader", wrap, Vector2.zero, new Vector2(520, 64), new Vector2(.5f, 1), Deep);
        header.pivot = new Vector2(.5f, 1);
        AddTopAccent(header, Cyan, 3f);
        AddSideDiamond(header, -1);
        AddSideDiamond(header, 1);

        TMP_Text tiny = TextEl("ModeLabel", header, "DEFENSE PROTOCOL", 10, Muted, TextAlignmentOptions.Center);
        Place(tiny.rectTransform, new Vector2(0, -7), new Vector2(280, 14), new Vector2(.5f, 1));
        tiny.rectTransform.pivot = new Vector2(.5f, 1);

        TMP_Text wave = TextEl("WaveValue", header, "WAVE 1 / 15", 27, Text, TextAlignmentOptions.Center);
        Place(wave.rectTransform, new Vector2(0, -22), new Vector2(430, 36), new Vector2(.5f, 1));
        wave.rectTransform.pivot = new Vector2(.5f, 1);
        wave.fontStyle = FontStyles.Bold;
        hud.waveText = wave;

        Button start = ButtonEl("StartWaveButton", wrap, "START WAVE", Cyan2, Cyan, 20);
        Place(start.GetComponent<RectTransform>(), new Vector2(0, -76), new Vector2(356, 58), new Vector2(.5f, 1));
        start.GetComponent<RectTransform>().pivot = new Vector2(.5f, 1);
        AddButtonAccent(start.transform, Green);
        hud.startWaveButton = start;
    }

    private static void BuildDock(RectTransform root, BuildMenuUI buildMenu)
    {
        int count = buildMenu.towerButtons != null ? buildMenu.towerButtons.Length : 0;
        const float cardW = 126f;
        const float spacing = 7f;
        float width = Mathf.Clamp(32 + count * cardW + Mathf.Max(0, count - 1) * spacing, 420f, 1220f);

        RectTransform dock = AuroraPanel("BuildDock", root, new Vector2(0, 18), new Vector2(width, 184), new Vector2(.5f, 0), Deep);
        dock.pivot = new Vector2(.5f, 0);
        AddTopAccent(dock, Cyan, 3f);

        TMP_Text dockLabel = TextEl("DockLabel", dock, "TOWER ARSENAL", 10, Muted, TextAlignmentOptions.Center);
        Place(dockLabel.rectTransform, new Vector2(0, -6), new Vector2(180, 16), new Vector2(.5f, 1));
        dockLabel.rectTransform.pivot = new Vector2(.5f, 1);

        RectTransform cards = Rect("Cards", dock, new Vector2(0, -26), new Vector2(width - 24, 146), new Vector2(.5f, 1));
        cards.pivot = new Vector2(.5f, 1);

        HorizontalLayoutGroup layout = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 0, 0);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (buildMenu.towerButtons == null)
            return;

        for (int i = 0; i < buildMenu.towerButtons.Length; i++)
        {
            BuildMenuUI.TowerButtonBinding binding = buildMenu.towerButtons[i];
            if (binding == null || binding.towerData == null)
                continue;

            RectTransform card = AuroraPanel($"TowerCard_{i + 1}", cards, Vector2.zero, new Vector2(cardW, 146), new Vector2(.5f, .5f), Card);
            LayoutElement le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = cardW;
            le.preferredHeight = 146;

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.transition = Selectable.Transition.None;
            AddPunch(button);

            RectTransform hotkeyBadge = PanelRect("HotkeyBadge", card, new Vector2(7, -7), new Vector2(27, 23), new Vector2(0, 1), Deep);
            hotkeyBadge.pivot = new Vector2(0, 1);
            TMP_Text hotkey = TextEl("Hotkey", hotkeyBadge, (i + 1).ToString(), 12, Cyan, TextAlignmentOptions.Center);
            Stretch(hotkey.rectTransform, 1);
            hotkey.fontStyle = FontStyles.Bold;

            RectTransform glyphBox = PanelRect("TowerGlyphBox", card, new Vector2(0, -34), new Vector2(64, 50), new Vector2(.5f, 1), new Color(Cyan.r, Cyan.g, Cyan.b, .08f));
            glyphBox.pivot = new Vector2(.5f, 1);
            TMP_Text icon = TextEl("Icon", glyphBox, TowerGlyph(binding.towerData.towerName), 28, Cyan, TextAlignmentOptions.Center);
            Stretch(icon.rectTransform, 2);
            icon.fontStyle = FontStyles.Bold;

            TMP_Text name = TextEl("Name", card, binding.towerData.towerName, 14, Text, TextAlignmentOptions.Center);
            Place(name.rectTransform, new Vector2(0, -88), new Vector2(114, 28), new Vector2(.5f, 1));
            name.rectTransform.pivot = new Vector2(.5f, 1);
            name.enableAutoSizing = true;
            name.fontSizeMin = 10;
            name.fontSizeMax = 14;

            RectTransform costBox = PanelRect("CostBox", card, new Vector2(0, -118), new Vector2(104, 22), new Vector2(.5f, 1), Deep);
            costBox.pivot = new Vector2(.5f, 1);
            TMP_Text cost = TextEl("Cost", costBox, CompactNumber.Format(binding.towerData.buildCost), 14, Gold, TextAlignmentOptions.Center);
            Stretch(cost.rectTransform, 2);
            cost.fontStyle = FontStyles.Bold;

            RectTransform selected = Rect("SelectedFrame", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.one);
            selected.offsetMin = new Vector2(-3, -3);
            selected.offsetMax = new Vector2(3, 3);
            Image selectedImage = selected.gameObject.AddComponent<Image>();
            selectedImage.color = new Color(Cyan.r, Cyan.g, Cyan.b, .09f);
            selectedImage.raycastTarget = false;
            Outline selectedOutline = selected.gameObject.AddComponent<Outline>();
            selectedOutline.effectColor = Cyan;
            selectedOutline.effectDistance = new Vector2(3, -3);
            AuroraUIEffects pulse = selected.gameObject.AddComponent<AuroraUIEffects>();
            pulse.targetGraphic = selectedImage;
            pulse.minAlpha = .07f;
            pulse.maxAlpha = .18f;
            pulse.pulseSpeed = 1.05f;
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
        RectTransform panel = AuroraPanel("UpgradePanelClean", root, new Vector2(-24, 0), new Vector2(448, 720), new Vector2(1, .5f), Deep);
        panel.pivot = new Vector2(1, .5f);
        AddTopAccent(panel, Cyan, 4f);
        upgrade.panelRoot = panel.gameObject;

        TMP_Text category = TextEl("Category", panel, "TOWER CONTROL", 10, Cyan, TextAlignmentOptions.MidlineLeft);
        Place(category.rectTransform, new Vector2(24, -16), new Vector2(200, 18), new Vector2(0, 1));
        category.fontStyle = FontStyles.Bold;

        TMP_Text title = TextEl("TowerName", panel, "Tower", 29, Text, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, new Vector2(24, -38), new Vector2(320, 42), new Vector2(0, 1));
        title.fontStyle = FontStyles.Bold;
        upgrade.towerNameText = title;

        Button close = ButtonEl("CloseButton", panel, "X", Panel2, Border, 17);
        Place(close.GetComponent<RectTransform>(), new Vector2(-17, -17), new Vector2(42, 42), new Vector2(1, 1));
        close.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
        upgrade.closeButton = close;

        RectTransform levelBadge = AuroraPanel("LevelBadge", panel, new Vector2(-24, -72), new Vector2(94, 52), new Vector2(1, 1), Panel2);
        levelBadge.pivot = new Vector2(1, 1);
        TMP_Text levelCaption = TextEl("Caption", levelBadge, "LEVEL", 9, Muted, TextAlignmentOptions.Center);
        Place(levelCaption.rectTransform, new Vector2(0, -5), new Vector2(80, 13), new Vector2(.5f, 1));
        levelCaption.rectTransform.pivot = new Vector2(.5f, 1);
        TMP_Text level = TextEl("Level", levelBadge, "Level 1", 17, Cyan, TextAlignmentOptions.Center);
        Place(level.rectTransform, new Vector2(0, -19), new Vector2(84, 27), new Vector2(.5f, 1));
        level.rectTransform.pivot = new Vector2(.5f, 1);
        level.fontStyle = FontStyles.Bold;
        upgrade.levelText = level;

        RectTransform current = Section(panel, "CurrentStats", new Vector2(18, -140), "CURRENT STATS", Cyan);
        upgrade.strengthText = StatRow(current, "Damage", 48, "DMG", Cyan);
        upgrade.attackSpeedText = StatRow(current, "Attack Speed", 91, "SPD", Blue);
        upgrade.rangeText = StatRow(current, "Range", 134, "RNG", Green);

        RectTransform next = Section(panel, "NextLevel", new Vector2(18, -338), "NEXT LEVEL", Green);
        upgrade.nextLevelRoot = next.gameObject;
        upgrade.nextLevelTitleText = next.Find("SectionTitle").GetComponent<TMP_Text>();
        upgrade.nextStrengthText = StatRow(next, "Damage", 48, "DMG", Cyan);
        upgrade.nextAttackSpeedText = StatRow(next, "Attack Speed", 91, "SPD", Blue);
        upgrade.nextRangeText = StatRow(next, "Range", 134, "RNG", Green);

        RectTransform costBox = AuroraPanel("UpgradeCostBox", panel, new Vector2(18, -536), new Vector2(412, 66), new Vector2(0, 1), Panel2);
        costBox.pivot = new Vector2(0, 1);
        TMP_Text costLabel = TextEl("CostLabel", costBox, "UPGRADE COST", 11, Muted, TextAlignmentOptions.MidlineLeft);
        Place(costLabel.rectTransform, new Vector2(16, -9), new Vector2(160, 18), new Vector2(0, 1));
        TMP_Text cost = TextEl("CostValue", costBox, "0", 26, Gold, TextAlignmentOptions.MidlineRight);
        Place(cost.rectTransform, new Vector2(200, -9), new Vector2(190, 44), new Vector2(0, 1));
        cost.fontStyle = FontStyles.Bold;
        upgrade.upgradeCostText = cost;

        Button up = ButtonEl("UpgradeButton", panel, "UPGRADE", Hex("#078CA5FF"), Cyan, 18);
        Place(up.GetComponent<RectTransform>(), new Vector2(18, -620), new Vector2(188, 60), new Vector2(0, 1));
        up.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        AddButtonAccent(up.transform, Green);
        upgrade.upgradeButton = up;
        upgrade.upgradeButtonLabel = up.GetComponentInChildren<TMP_Text>();

        Button sell = ButtonEl("SellButton", panel, "SELL", Hex("#493018FF"), Orange, 16);
        Place(sell.GetComponent<RectTransform>(), new Vector2(214, -620), new Vector2(118, 60), new Vector2(0, 1));
        sell.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        upgrade.sellButton = sell;
        upgrade.sellButtonLabel = sell.GetComponentInChildren<TMP_Text>();

        Button closeBottom = ButtonEl("CloseBottomButton", panel, "CLOSE", Panel2, Border, 14);
        Place(closeBottom.GetComponent<RectTransform>(), new Vector2(340, -620), new Vector2(90, 60), new Vector2(0, 1));
        closeBottom.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        upgrade.secondaryCloseButton = closeBottom;

        panel.gameObject.SetActive(false);
    }

    private static RectTransform Section(RectTransform parent, string name, Vector2 pos, string title, Color accent)
    {
        RectTransform section = AuroraPanel(name, parent, pos, new Vector2(412, 184), new Vector2(0, 1), Panel);
        section.pivot = new Vector2(0, 1);

        RectTransform accentBar = PanelRect("Accent", section, new Vector2(0, 0), new Vector2(4, 184), new Vector2(0, 1), accent);
        accentBar.pivot = new Vector2(0, 1);

        TMP_Text t = TextEl("SectionTitle", section, title, 12, accent, TextAlignmentOptions.MidlineLeft);
        Place(t.rectTransform, new Vector2(16, -8), new Vector2(250, 23), new Vector2(0, 1));
        t.fontStyle = FontStyles.Bold;
        return section;
    }

    private static TMP_Text StatRow(RectTransform parent, string label, float y, string glyph, Color accent)
    {
        RectTransform iconBox = PanelRect(label + "IconBox", parent, new Vector2(15, -y), new Vector2(40, 32), new Vector2(0, 1), new Color(accent.r, accent.g, accent.b, .10f));
        iconBox.pivot = new Vector2(0, 1);

        TMP_Text icon = TextEl(label + "Icon", iconBox, glyph, 9, accent, TextAlignmentOptions.Center);
        Stretch(icon.rectTransform, 1);
        icon.fontStyle = FontStyles.Bold;

        TMP_Text labelText = TextEl(label + "Label", parent, label, 16, Muted, TextAlignmentOptions.MidlineLeft);
        Place(labelText.rectTransform, new Vector2(66, -y), new Vector2(150, 32), new Vector2(0, 1));

        TMP_Text value = TextEl(label + "Value", parent, "0", 17, Text, TextAlignmentOptions.MidlineRight);
        Place(value.rectTransform, new Vector2(218, -y), new Vector2(174, 32), new Vector2(0, 1));
        value.fontStyle = FontStyles.Bold;
        return value;
    }

    private static void StyleSpeedSelector(GameSpeedController speed)
    {
        StyleSpeedButton(speed.speed1Button);
        StyleSpeedButton(speed.speed2Button);
        StyleSpeedButton(speed.speed3Button);

        if (speed.currentSpeedText != null)
        {
            speed.currentSpeedText.color = Cyan;
            speed.currentSpeedText.fontStyle = FontStyles.Bold;
        }
    }

    private static void StyleSpeedButton(Button button)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();

        if (image != null)
        {
            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(button.gameObject);
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(1, -1);
        }

        AddPunch(button);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.color = Text;
            label.fontStyle = FontStyles.Bold;
        }
    }

    private static void AddTopAccent(RectTransform parent, Color color, float height)
    {
        RectTransform bar = PanelRect("AuroraAccent", parent, new Vector2(0, 0), new Vector2(0, height), new Vector2(0, 1), color);
        bar.anchorMin = new Vector2(0, 1);
        bar.anchorMax = new Vector2(1, 1);
        bar.offsetMin = new Vector2(10, -height);
        bar.offsetMax = new Vector2(-10, 0);

        AuroraUIEffects fx = bar.gameObject.AddComponent<AuroraUIEffects>();
        fx.targetGraphic = bar.GetComponent<Image>();
        fx.minAlpha = .35f;
        fx.maxAlpha = .9f;
        fx.pulseSpeed = .65f;
    }

    private static void AddButtonAccent(Transform parent, Color color)
    {
        RectTransform bar = PanelRect("ActionAccent", parent, new Vector2(0, 0), Vector2.zero, new Vector2(0, 0), color);
        bar.anchorMin = new Vector2(0, 0);
        bar.anchorMax = new Vector2(1, 0);
        bar.offsetMin = new Vector2(8, 3);
        bar.offsetMax = new Vector2(-8, 6);
        AuroraUIEffects fx = bar.gameObject.AddComponent<AuroraUIEffects>();
        fx.targetGraphic = bar.GetComponent<Image>();
        fx.minAlpha = .25f;
        fx.maxAlpha = .85f;
        fx.pulseSpeed = .85f;
    }

    private static void AddSideDiamond(RectTransform parent, int side)
    {
        RectTransform d = PanelRect(side < 0 ? "LeftAccent" : "RightAccent", parent, new Vector2(side * 242, -27), new Vector2(16, 16), new Vector2(.5f, 1), Cyan);
        d.pivot = new Vector2(.5f, .5f);
        d.localRotation = Quaternion.Euler(0, 0, 45);
        d.GetComponent<Image>().raycastTarget = false;
    }

    private static Button ButtonEl(string name, Transform parent, string text, Color bg, Color border, float fontSize)
    {
        RectTransform rt = Rect(name, parent, Vector2.zero, new Vector2(120, 50), new Vector2(.5f, .5f));
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = bg;
        img.raycastTarget = true;

        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = border;
        outline.effectDistance = new Vector2(2, -2);

        Shadow shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .45f);
        shadow.effectDistance = new Vector2(0, -4);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        button.transition = Selectable.Transition.None;
        AddPunch(button);

        TMP_Text label = TextEl("Label", rt, text, fontSize, Text, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 7);
        label.fontStyle = FontStyles.Bold;
        return button;
    }

    private static void AddPunch(Button button)
    {
        if (button == null)
            return;

        UIPunchButton punch = button.GetComponent<UIPunchButton>();
        if (punch == null)
            punch = Undo.AddComponent<UIPunchButton>(button.gameObject);

        punch.hoverScale = 1.035f;
        punch.pressedScale = .95f;
        punch.hoverDuration = .10f;
        punch.hoverBrightness = 1.10f;
    }

    private static RectTransform AuroraPanel(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchor, Color color)
    {
        RectTransform rt = PanelRect(name, parent, pos, size, anchor, color);

        Shadow shadow = rt.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .52f);
        shadow.effectDistance = new Vector2(0, -5);
        return rt;
    }

    private static RectTransform PanelRect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchor, Color color)
    {
        RectTransform rt = Rect(name, parent, pos, size, anchor);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(1, -1);
        return rt;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchor)
    {
        return Rect(name, parent, pos, size, anchor, anchor);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create Aurora UI");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    private static TMP_Text TextEl(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create Aurora UI Text");
        go.transform.SetParent(parent, false);

        TMP_Text tmp = go.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size, Vector2 anchor)
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
        if (component != null && !list.Contains(component.gameObject))
            list.Add(component.gameObject);
    }

    private static T FindSceneObject<T>() where T : Object
    {
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in all)
        {
            if (obj == null || EditorUtility.IsPersistent(obj))
                continue;

            Component c = obj as Component;
            if (c != null && c.gameObject.scene.IsValid())
                return obj;
        }

        return null;
    }

    private static string TowerGlyph(string name)
    {
        string n = (name ?? string.Empty).ToLowerInvariant();
        if (n.Contains("gold") || n.Contains("mine")) return "$";
        if (n.Contains("bomb")) return "B";
        if (n.Contains("burn") || n.Contains("fire")) return "F";
        if (n.Contains("cannon")) return "C";
        if (n.Contains("arch") || n.Contains("bow")) return "A";
        return "T";
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
#endif
