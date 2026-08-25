#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class PauseMenuSetupTool
{
    private static readonly Color Overlay = new Color(0.015f, 0.04f, 0.065f, 0.72f);
    private static readonly Color Card = new Color(0.035f, 0.105f, 0.15f, 0.98f);
    private static readonly Color ButtonNormal = new Color(0.045f, 0.16f, 0.22f, 1f);
    private static readonly Color ButtonHover = new Color(0.055f, 0.27f, 0.35f, 1f);
    private static readonly Color Cyan = new Color(0.05f, 0.78f, 0.96f, 1f);
    private static readonly Color Text = new Color(0.92f, 0.97f, 1f, 1f);
    private static readonly Color Muted = new Color(0.58f, 0.70f, 0.78f, 1f);

    [MenuItem("Tower Defense/UI/Setup Pause Menu + Settings Gear")]
    public static void SetupPauseMenu()
    {
        if (UnityEngine.Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog(
                "Pause Menu",
                "A PauseMenuController already exists in this scene. Nothing was duplicated.",
                "OK");
            return;
        }

        MainMenuController mainMenu = UnityEngine.Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (mainMenu == null)
        {
            EditorUtility.DisplayDialog(
                "Pause Menu",
                "MainMenuController was not found. Run Tower Defense → UI → Setup Main Menu first.",
                "OK");
            return;
        }

        EnsureEventSystem();

        GameObject canvasGO = new GameObject("PauseMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Pause Menu");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        PauseMenuController controller = canvasGO.AddComponent<PauseMenuController>();
        controller.mainMenu = mainMenu;

        // Full-screen pause overlay.
        RectTransform pausePanel = CreateRect("PausePanel", canvasGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image overlay = pausePanel.gameObject.AddComponent<Image>();
        overlay.color = Overlay;

        RectTransform card = CreateRect("PauseCard", pausePanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-285f, -245f), new Vector2(285f, 245f));
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = Card;
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text title = CreateText("PauseTitle", card, "PAUSED", 54, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(470f, 72f));

        TMP_Text subtitle = CreateText("PauseSubtitle", card, "ESC TO CONTINUE", 17, FontStyles.Bold, TextAlignmentOptions.Center, Muted);
        SetAnchored(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(430f, 32f));

        Button continueButton = CreateButton("ContinueButton", card, "CONTINUE", new Vector2(0f, 32f), new Vector2(380f, 72f), true);
        Button mainMenuButton = CreateButton("MainMenuButton", card, "MAIN MENU", new Vector2(0f, -72f), new Vector2(380f, 72f), false);

        TMP_Text hint = CreateText("SettingsHint", card, "Use the gear button for audio settings", 16, FontStyles.Normal, TextAlignmentOptions.Center, Muted);
        SetAnchored(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(460f, 30f));

        // Always-available gameplay settings gear, hidden automatically while main/settings menu is visible.
        Button gearButton = CreateGearButton(canvasGO.transform);

        controller.pausePanel = pausePanel.gameObject;
        controller.continueButton = continueButton;
        controller.mainMenuButton = mainMenuButton;
        controller.gearButton = gearButton;

        pausePanel.gameObject.SetActive(false);
        gearButton.gameObject.SetActive(false);

        EditorUtility.SetDirty(controller);
        Selection.activeGameObject = canvasGO;

        EditorUtility.DisplayDialog(
            "Pause Menu Ready",
            "ESC now opens PAUSED. Continue restores 1x/2x/3x, Main Menu returns to the main menu, and the gear opens Settings directly from gameplay.",
            "OK");
    }

    private static Button CreateGearButton(Transform parent)
    {
        RectTransform rt = CreateRect("SettingsGearButton", parent,
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-94f, -94f), new Vector2(-26f, -26f));

        Image image = rt.gameObject.AddComponent<Image>();
        image.color = new Color(0.035f, 0.12f, 0.17f, 0.96f);

        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.06f, 0.28f, 0.36f, 1f);
        colors.pressedColor = new Color(0.03f, 0.42f, 0.55f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.06f, 0.08f, 0.10f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text icon = CreateText("GearIcon", rt, "⚙", 39, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        Stretch(icon.rectTransform);

        AddPunchIfAvailable(rt.gameObject);
        return button;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, bool primary)
    {
        RectTransform rt = CreateRect(name, parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            position - size * 0.5f, position + size * 0.5f);

        Image image = rt.gameObject.AddComponent<Image>();
        image.color = primary ? new Color(0.025f, 0.52f, 0.68f, 1f) : ButtonNormal;

        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = primary ? Cyan : new Color(Cyan.r, Cyan.g, Cyan.b, 0.38f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = primary ? new Color(0.04f, 0.67f, 0.84f, 1f) : ButtonHover;
        colors.pressedColor = new Color(0.035f, 0.42f, 0.55f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.08f, 0.10f, 0.12f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText("Label", rt, label, 25, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        Stretch(text.rectTransform);
        AddPunchIfAvailable(rt.gameObject);
        return button;
    }

    private static void AddPunchIfAvailable(GameObject target)
    {
        Type punchType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            punchType = assembly.GetType("UIPunchButton");
            if (punchType != null) break;
        }
        if (punchType == null || target.GetComponent(punchType) != null) return;
        Undo.AddComponent(target, punchType);
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style, TextAlignmentOptions align, Color color)
    {
        RectTransform rt = CreateRect(name, parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-100f, -20f), new Vector2(100f, 20f));

        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.color = color;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.localScale = Vector3.one;
        return rt;
    }

    private static void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }
}
#endif
