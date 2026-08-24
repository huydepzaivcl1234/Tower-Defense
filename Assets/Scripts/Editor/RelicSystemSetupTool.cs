#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Creates starter relic assets + a clean 3-card runtime choice UI in the open scene.</summary>
public static class RelicSystemSetupTool
{
    private const string DataRoot = "Assets/Data";
    private const string RelicRoot = "Assets/Data/Relics";

    [MenuItem("Tower Defense/Relics/Setup Relic System")]
    public static void Setup()
    {
        EnsureFolder(DataRoot);
        EnsureFolder(RelicRoot);

        List<RelicData> relics = new List<RelicData>
        {
            CreateRelic("LongSight", "Long Sight", "+10% range for all towers.", RelicEffectType.TowerRangePercent, 0.10f, 99),
            CreateRelic("RapidMechanism", "Rapid Mechanism", "+10% attack speed for all towers.", RelicEffectType.TowerAttackSpeedPercent, 0.10f, 99),
            CreateRelic("SharpenedAmmo", "Sharpened Ammo", "+10% damage for all towers.", RelicEffectType.TowerDamagePercent, 0.10f, 99),
            CreateRelic("GoldenTouch", "Golden Touch", "+5% gold earned from enemies and gold towers.", RelicEffectType.GoldGainPercent, 0.05f, 99),
            CreateRelic("CheapFoundation", "Cheap Foundation", "Tower build cost -5%.", RelicEffectType.BuildCostDiscountPercent, 0.05f, 18),
            CreateRelic("EfficientUpgrade", "Efficient Upgrade", "Tower upgrade cost -5%.", RelicEffectType.UpgradeCostDiscountPercent, 0.05f, 18)
        };

        RelicManager manager = Object.FindFirstObjectByType<RelicManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("RelicManager");
            Undo.RegisterCreatedObjectUndo(go, "Create Relic Manager");
            manager = go.AddComponent<RelicManager>();
        }

        RelicChoiceUI ui = Object.FindFirstObjectByType<RelicChoiceUI>(FindObjectsInactive.Include);
        if (ui == null)
            ui = CreateUI();
        else
            UpgradeExistingUI(ui);

        Undo.RecordObject(manager, "Configure Relic Manager");
        manager.wavesPerChoice = 3;
        manager.choicesPerRoll = 3;
        manager.skipAfterFinalWave = true;
        manager.relicPool = relics;
        manager.choiceUI = ui;
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(ui);

        Selection.activeGameObject = manager.gameObject;
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Relic System",
            "Setup complete.\n\nRelic cards include Icon + Name + Description + Stack and automatically receive UIPunchButton feedback.\nExisting Relic UI was upgraded in place.", "OK");
    }

    [MenuItem("Tower Defense/Relics/Upgrade Existing Relic UI")]
    public static void UpgradeExistingRelicUIFromMenu()
    {
        RelicChoiceUI ui = Object.FindFirstObjectByType<RelicChoiceUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Relic UI", "No RelicChoiceUI found in the open scene. Run Setup Relic System first.", "OK");
            return;
        }

        UpgradeExistingUI(ui);
        EditorUtility.SetDirty(ui);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = ui.gameObject;
        EditorUtility.DisplayDialog("Relic UI", "Existing relic cards upgraded with icon slots and punch feedback.", "OK");
    }

    private static RelicData CreateRelic(string fileName, string displayName, string description,
        RelicEffectType effect, float value, int maxStacks)
    {
        string path = $"{RelicRoot}/{fileName}.asset";
        RelicData relic = AssetDatabase.LoadAssetAtPath<RelicData>(path);
        if (relic == null)
        {
            relic = ScriptableObject.CreateInstance<RelicData>();
            AssetDatabase.CreateAsset(relic, path);
        }

        relic.relicName = displayName;
        relic.description = description;
        relic.selectionWeight = 1f;
        relic.maxStacks = maxStacks;
        relic.modifiers = new[] { new RelicModifier { effect = effect, value = value } };
        EditorUtility.SetDirty(relic);
        return relic;
    }

    private static RelicChoiceUI CreateUI()
    {
        GameObject canvasGO = new GameObject("RelicCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Relic UI");
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = UIObject("RelicChoicePanel", canvasGO.transform);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.025f, 0.035f, 0.05f, 0.92f);

        TextMeshProUGUI title = MakeText("Title", panel.transform, "CHOOSE A RELIC", 42, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(900, 80), Vector2.zero);

        RelicChoiceUI ui = canvasGO.AddComponent<RelicChoiceUI>();
        ui.panelRoot = panel;
        ui.titleText = title;
        ui.cards = new RelicChoiceUI.RelicCard[3];

        float[] xs = { 0.23f, 0.50f, 0.77f };
        for (int i = 0; i < 3; i++)
            ui.cards[i] = MakeCard(panel.transform, i + 1, xs[i]);

        panel.SetActive(false);
        return ui;
    }

    private static RelicChoiceUI.RelicCard MakeCard(Transform parent, int index, float x)
    {
        GameObject cardGO = UIObject("RelicCard" + index, parent);
        RectTransform r = cardGO.GetComponent<RectTransform>();
        SetRect(r, new Vector2(x, 0.48f), new Vector2(x, 0.48f), new Vector2(420, 590), Vector2.zero);

        Image img = cardGO.AddComponent<Image>();
        img.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);
        Button button = cardGO.AddComponent<Button>();
        button.targetGraphic = img;
        ConfigureButtonColors(button);
        EnsurePunchFeedback(cardGO);

        Image icon = CreateIconArea(cardGO.transform);

        TextMeshProUGUI name = MakeText("Name", cardGO.transform, "RELIC", 30, TextAlignmentOptions.Center);
        name.fontStyle = FontStyles.Bold;
        SetRect(name.rectTransform, new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.57f), new Vector2(360, 64), Vector2.zero);

        TextMeshProUGUI desc = MakeText("Description", cardGO.transform, "Description", 21, TextAlignmentOptions.Center);
        desc.enableWordWrapping = true;
        desc.color = new Color(0.83f, 0.88f, 0.95f, 1f);
        SetRect(desc.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.34f), new Vector2(340, 150), Vector2.zero);

        TextMeshProUGUI stack = MakeText("Stack", cardGO.transform, "STACK", 17, TextAlignmentOptions.Center);
        stack.color = new Color(0.35f, 0.88f, 1f, 1f);
        SetRect(stack.rectTransform, new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.10f), new Vector2(300, 42), Vector2.zero);

        return new RelicChoiceUI.RelicCard
        {
            button = button,
            nameText = name,
            descriptionText = desc,
            stackText = stack,
            icon = icon
        };
    }

    private static Image CreateIconArea(Transform card)
    {
        GameObject frameGO = UIObject("IconFrame", card);
        Image frame = frameGO.AddComponent<Image>();
        frame.color = new Color(0.035f, 0.055f, 0.085f, 1f);
        SetRect(frame.rectTransform, new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f), new Vector2(230, 230), Vector2.zero);

        GameObject iconGO = UIObject("Icon", frameGO.transform);
        Image icon = iconGO.AddComponent<Image>();
        icon.color = Color.white;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(198, 198), Vector2.zero);
        icon.enabled = false;
        return icon;
    }

    private static void UpgradeExistingUI(RelicChoiceUI ui)
    {
        if (ui == null || ui.cards == null) return;

        Undo.RecordObject(ui, "Upgrade Relic UI");

        for (int i = 0; i < ui.cards.Length; i++)
        {
            RelicChoiceUI.RelicCard card = ui.cards[i];
            if (card == null || card.button == null) continue;

            GameObject cardGO = card.button.gameObject;
            RectTransform cardRect = cardGO.GetComponent<RectTransform>();
            if (cardRect != null)
                cardRect.sizeDelta = new Vector2(420, 590);

            EnsurePunchFeedback(cardGO);

            Transform frameTransform = cardGO.transform.Find("IconFrame");
            Image icon;
            if (frameTransform == null)
            {
                icon = CreateIconArea(cardGO.transform);
            }
            else
            {
                Transform iconTransform = frameTransform.Find("Icon");
                if (iconTransform == null)
                {
                    GameObject iconGO = UIObject("Icon", frameTransform);
                    icon = iconGO.AddComponent<Image>();
                }
                else
                {
                    icon = iconTransform.GetComponent<Image>();
                    if (icon == null) icon = iconTransform.gameObject.AddComponent<Image>();
                }

                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(198, 198), Vector2.zero);
            }

            card.icon = icon;

            if (card.nameText != null)
            {
                card.nameText.fontStyle = FontStyles.Bold;
                SetRect(card.nameText.rectTransform, new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.57f), new Vector2(360, 64), Vector2.zero);
            }

            if (card.descriptionText != null)
            {
                card.descriptionText.fontSize = 21;
                card.descriptionText.color = new Color(0.83f, 0.88f, 0.95f, 1f);
                SetRect(card.descriptionText.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.34f), new Vector2(340, 150), Vector2.zero);
            }

            if (card.stackText != null)
            {
                card.stackText.color = new Color(0.35f, 0.88f, 1f, 1f);
                SetRect(card.stackText.rectTransform, new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.10f), new Vector2(300, 42), Vector2.zero);
            }

            ConfigureButtonColors(card.button);
            EditorUtility.SetDirty(cardGO);
        }

        EditorUtility.SetDirty(ui);
    }

    private static void EnsurePunchFeedback(GameObject go)
    {
        if (go == null || go.GetComponent<UIPunchButton>() != null) return;
        Undo.AddComponent<UIPunchButton>(go);
    }

    private static void ConfigureButtonColors(Button button)
    {
        if (button == null) return;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.88f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private static GameObject UIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = UIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    private static void SetRect(RectTransform r, Vector2 min, Vector2 max, Vector2 size, Vector2 pos)
    {
        r.anchorMin = min; r.anchorMax = max; r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size; r.anchoredPosition = pos;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
