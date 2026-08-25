#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class EndGameUISetupTool
{
    private static readonly Color Overlay = new Color(0.015f, 0.035f, 0.055f, 0.86f);
    private static readonly Color Card = new Color(0.025f, 0.085f, 0.125f, 0.98f);
    private static readonly Color ButtonNormal = new Color(0.04f, 0.16f, 0.22f, 1f);
    private static readonly Color ButtonHover = new Color(0.055f, 0.28f, 0.36f, 1f);
    private static readonly Color Cyan = new Color(0.05f, 0.78f, 0.96f, 1f);
    private static readonly Color Win = new Color(0.22f, 0.92f, 0.62f, 1f);
    private static readonly Color Lose = new Color(1f, 0.28f, 0.28f, 1f);
    private static readonly Color Text = new Color(0.93f, 0.97f, 1f, 1f);
    private static readonly Color Muted = new Color(0.60f, 0.72f, 0.80f, 1f);

    [MenuItem("Tower Defense/UI/Setup Win Lose Screen")]
    public static void Setup()
    {
        EndGameUIController existing = UnityEngine.Object.FindAnyObjectByType<EndGameUIController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Win / Lose UI", "EndGameUIController already exists. No duplicate was created.", "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        EnsureEventSystem();

        GameObject canvasGO = new GameObject("EndGameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Win Lose UI");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        EndGameUIController controller = canvasGO.AddComponent<EndGameUIController>();

        RectTransform root = CreateRect("EndGameRoot", canvasGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image overlay = root.gameObject.AddComponent<Image>();
        overlay.color = Overlay;

        RectTransform card = CreateRect("ResultCard", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-390f, -285f), new Vector2(390f, 285f));
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = Card;
        Outline cardOutline = card.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.32f);
        cardOutline.effectDistance = new Vector2(3f, -3f);

        RectTransform topLine = CreateRect("TopAccent", card, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), Vector2.zero);
        topLine.gameObject.AddComponent<Image>().color = Cyan;

        RectTransform winContent = CreateRect("WinContent", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TMP_Text victory = CreateText("Title", winContent, "VICTORY", 68, FontStyles.Bold, TextAlignmentOptions.Center, Win);
        SetAnchored(victory.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(650f, 90f));
        TMP_Text winSub = CreateText("Subtitle", winContent, "ALL WAVES CLEARED", 22, FontStyles.Bold, TextAlignmentOptions.Center, Muted);
        SetAnchored(winSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(600f, 44f));

        RectTransform loseContent = CreateRect("LoseContent", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TMP_Text defeat = CreateText("Title", loseContent, "DEFEAT", 68, FontStyles.Bold, TextAlignmentOptions.Center, Lose);
        SetAnchored(defeat.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(650f, 90f));
        TMP_Text loseSub = CreateText("Subtitle", loseContent, "YOUR BASE HAS FALLEN", 22, FontStyles.Bold, TextAlignmentOptions.Center, Muted);
        SetAnchored(loseSub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(600f, 44f));

        TMP_Text prompt = CreateText("Prompt", card, "CHOOSE YOUR NEXT MOVE", 16, FontStyles.Bold, TextAlignmentOptions.Center, Muted);
        SetAnchored(prompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(520f, 34f));

        Button retry = CreateButton("RetryButton", card, "RETRY", new Vector2(0f, -90f), new Vector2(390f, 76f), true);
        Button mainMenu = CreateButton("MainMenuButton", card, "MAIN MENU", new Vector2(0f, -188f), new Vector2(390f, 70f), false);

        controller.rootPanel = root.gameObject;
        controller.winContent = winContent.gameObject;
        controller.loseContent = loseContent.gameObject;
        controller.retryButton = retry;
        controller.mainMenuButton = mainMenu;

        root.gameObject.SetActive(false);
        winContent.gameObject.SetActive(false);
        loseContent.gameObject.SetActive(false);

        HUDManager hud = UnityEngine.Object.FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include);
        if (hud != null)
        {
            if (hud.gameOverPanel != null) hud.gameOverPanel.SetActive(false);
            if (hud.winPanel != null) hud.winPanel.SetActive(false);
            EditorUtility.SetDirty(hud);
        }

        EditorUtility.SetDirty(controller);
        Selection.activeGameObject = canvasGO;

        EditorUtility.DisplayDialog(
            "Win / Lose UI Ready",
            "Created VICTORY and DEFEAT screens with RETRY + MAIN MENU. Both actions use the same fade timing/color as the Play transition by default.",
            "OK");
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, bool primary)
    {
        RectTransform rt = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position - size * 0.5f, position + size * 0.5f);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = primary ? new Color(0.025f, 0.50f, 0.67f, 1f) : ButtonNormal;

        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = primary ? Cyan : new Color(Cyan.r, Cyan.g, Cyan.b, 0.42f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = primary ? new Color(0.04f, 0.66f, 0.84f, 1f) : ButtonHover;
        colors.pressedColor = new Color(0.035f, 0.40f, 0.52f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText("Label", rt, label, 27, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        Stretch(text.rectTransform);

        AddOptionalComponent(rt.gameObject, "UIPunchButton");
        AddOptionalComponent(rt.gameObject, "UIButtonSFX");
        return button;
    }

    private static void AddOptionalComponent(GameObject target, string typeName)
    {
        Type type = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null) break;
        }

        if (type != null && target.GetComponent(type) == null)
            Undo.AddComponent(target, type);
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style, TextAlignmentOptions align, Color color)
    {
        RectTransform rt = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-100f, -20f), new Vector2(100f, 20f));
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
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
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }
}
#endif

