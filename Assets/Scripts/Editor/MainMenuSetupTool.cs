#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuSetupTool
{
    private static readonly Color Background = new Color(0.025f, 0.07f, 0.105f, 0.94f);
    private static readonly Color Card = new Color(0.035f, 0.105f, 0.15f, 0.97f);
    private static readonly Color ButtonNormal = new Color(0.045f, 0.16f, 0.22f, 1f);
    private static readonly Color ButtonHover = new Color(0.055f, 0.27f, 0.35f, 1f);
    private static readonly Color Cyan = new Color(0.05f, 0.78f, 0.96f, 1f);
    private static readonly Color Text = new Color(0.91f, 0.96f, 1f, 1f);
    private static readonly Color Muted = new Color(0.55f, 0.68f, 0.76f, 1f);

    [MenuItem("Tower Defense/UI/Setup Main Menu")]
    public static void SetupMainMenu()
    {
        if (UnityEngine.Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog(
                "Main Menu",
                "A MainMenuController already exists in this scene. The setup tool did not create a duplicate.",
                "OK");
            return;
        }

        EnsureEventSystem();

        GameObject audioGO = GameObject.Find("AudioSettingsManager");
        if (audioGO == null)
        {
            audioGO = new GameObject("AudioSettingsManager");
            Undo.RegisterCreatedObjectUndo(audioGO, "Create Audio Settings Manager");
        }
        AudioSettingsManager audioManager = audioGO.GetComponent<AudioSettingsManager>();
        if (audioManager == null) audioManager = Undo.AddComponent<AudioSettingsManager>(audioGO);

        GameObject canvasGO = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Main Menu");

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        MainMenuController controller = canvasGO.AddComponent<MainMenuController>();

        RectTransform mainPanel = CreateRect("MainPanel", canvasGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image mainBg = mainPanel.gameObject.AddComponent<Image>();
        mainBg.color = Background;

        CreateAccent(mainPanel, true);
        CreateAccent(mainPanel, false);

        TMP_Text title = CreateText("Title", mainPanel, "TOWER DEFENSE", 74, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 245f), new Vector2(760f, 100f));

        TMP_Text subtitle = CreateText("Subtitle", mainPanel, "DEFEND  •  UPGRADE  •  SURVIVE", 21, FontStyles.Bold, TextAlignmentOptions.Center, Muted);
        SetAnchored(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 176f), new Vector2(700f, 44f));

        RectTransform titleLine = CreateRect("TitleLine", mainPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-170f, 142f), new Vector2(170f, 146f));
        titleLine.gameObject.AddComponent<Image>().color = Cyan;

        Button play = CreateMenuButton("PlayButton", mainPanel, "PLAY", new Vector2(0f, 45f), new Vector2(380f, 76f), true);
        Button settings = CreateMenuButton("SettingsButton", mainPanel, "SETTINGS", new Vector2(0f, -55f), new Vector2(380f, 76f), false);
        Button exit = CreateMenuButton("ExitButton", mainPanel, "EXIT", new Vector2(0f, -155f), new Vector2(380f, 76f), false);

        TMP_Text footer = CreateText("Footer", mainPanel, "A TOWER DEFENSE RUN", 16, FontStyles.Normal, TextAlignmentOptions.Center, Muted);
        SetAnchored(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(600f, 30f));

        RectTransform settingsPanel = CreateRect("SettingsPanel", canvasGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        settingsPanel.gameObject.AddComponent<Image>().color = Background;

        RectTransform settingsCard = CreateRect("SettingsCard", settingsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -350f), new Vector2(330f, 350f));
        Image cardImage = settingsCard.gameObject.AddComponent<Image>();
        cardImage.color = Card;
        Outline cardOutline = settingsCard.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.28f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        TMP_Text settingsTitle = CreateText("SettingsTitle", settingsCard, "SETTINGS", 48, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        SetAnchored(settingsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(520f, 70f));

        TMP_Text audioLabel = CreateText("AudioHeader", settingsCard, "AUDIO", 18, FontStyles.Bold, TextAlignmentOptions.Left, Cyan);
        SetAnchored(audioLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(520f, 32f));

        Slider masterSlider = CreateAudioRow(settingsCard, "MASTER", -205f, out TMP_Text masterValue);
        Slider musicSlider = CreateAudioRow(settingsCard, "MUSIC", -315f, out TMP_Text musicValue);
        Slider sfxSlider = CreateAudioRow(settingsCard, "SFX", -425f, out TMP_Text sfxValue);

        Button back = CreateMenuButton("BackButton", settingsCard, "BACK", new Vector2(0f, -282f), new Vector2(260f, 62f), false);

        controller.mainPanel = mainPanel.gameObject;
        controller.settingsPanel = settingsPanel.gameObject;
        controller.playButton = play;
        controller.settingsButton = settings;
        controller.exitButton = exit;
        controller.masterSlider = masterSlider;
        controller.musicSlider = musicSlider;
        controller.sfxSlider = sfxSlider;
        controller.masterValueText = masterValue;
        controller.musicValueText = musicValue;
        controller.sfxValueText = sfxValue;
        controller.backButton = back;

        settingsPanel.gameObject.SetActive(false);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(audioManager);
        Selection.activeGameObject = canvasGO;
        EditorUtility.DisplayDialog(
            "Main Menu Ready",
            "Main menu created. Play starts the game, Settings controls Master/Music/SFX, and Exit closes the build (or stops Play Mode in Editor).",
            "OK");
    }

    private static Slider CreateAudioRow(Transform parent, string labelText, float y, out TMP_Text valueText)
    {
        TMP_Text label = CreateText(labelText + "Label", parent, labelText, 22, FontStyles.Bold, TextAlignmentOptions.Left, Text);
        SetAnchored(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(-170f, y), new Vector2(180f, 38f));

        valueText = CreateText(labelText + "Value", parent, "100%", 19, FontStyles.Bold, TextAlignmentOptions.Right, Cyan);
        SetAnchored(valueText.rectTransform, new Vector2(0.5f, 1f), new Vector2(218f, y), new Vector2(90f, 38f));

        RectTransform sliderRT = CreateRect(labelText + "Slider", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-70f, y - 10f), new Vector2(190f, y + 18f));
        Slider slider = sliderRT.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        RectTransform background = CreateRect("Background", sliderRT, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -5f), new Vector2(0f, 5f));
        background.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.16f, 0.20f, 1f);

        RectTransform fillArea = CreateRect("Fill Area", sliderRT, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(8f, 0f), new Vector2(-8f, 0f));
        RectTransform fill = CreateRect("Fill", fillArea, new Vector2(0f, 0.32f), new Vector2(1f, 0.68f), Vector2.zero, Vector2.zero);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = Cyan;

        RectTransform handleArea = CreateRect("Handle Slide Area", sliderRT, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-10f, 0f));
        RectTransform handle = CreateRect("Handle", handleArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(10f, 10f));
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Text;
        Outline handleOutline = handle.gameObject.AddComponent<Outline>();
        handleOutline.effectColor = Cyan;
        handleOutline.effectDistance = new Vector2(2f, -2f);

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;

        ColorBlock colors = slider.colors;
        colors.normalColor = Text;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Cyan;
        slider.colors = colors;
        return slider;
    }

    private static Button CreateMenuButton(string name, Transform parent, string label, Vector2 position, Vector2 size, bool primary)
    {
        RectTransform rt = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position - size * 0.5f, position + size * 0.5f);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = primary ? new Color(0.025f, 0.52f, 0.68f, 1f) : ButtonNormal;

        Outline outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = primary ? Cyan : new Color(Cyan.r, Cyan.g, Cyan.b, 0.4f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = primary ? new Color(0.04f, 0.67f, 0.84f, 1f) : ButtonHover;
        colors.pressedColor = new Color(0.035f, 0.42f, 0.55f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.08f, 0.10f, 0.12f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText("Label", rt, label, 27, FontStyles.Bold, TextAlignmentOptions.Center, Text);
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

    private static void CreateAccent(Transform parent, bool top)
    {
        RectTransform line = CreateRect(top ? "TopAccent" : "BottomAccent", parent,
            top ? new Vector2(0f, 1f) : new Vector2(0f, 0f),
            top ? new Vector2(1f, 1f) : new Vector2(1f, 0f),
            top ? new Vector2(0f, -4f) : new Vector2(0f, 0f),
            top ? new Vector2(0f, 0f) : new Vector2(0f, 4f));
        line.gameObject.AddComponent<Image>().color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f);
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
