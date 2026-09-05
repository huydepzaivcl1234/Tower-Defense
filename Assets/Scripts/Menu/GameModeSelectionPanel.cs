using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built mode picker that reuses the current main-menu button style.
/// Story wave data and Endless scaling remain owned by WaveManager.
/// </summary>
public class GameModeSelectionPanel : MonoBehaviour
{
    private MainMenuController menu;
    private Button buttonStyleSource;
    private GameObject modeChoices;
    private GameObject storyChoices;
    private RectTransform storyContent;
    private Button storyButtonTemplate;
    private TMP_Text storyHeading;
    private TMP_Text statusText;
    private readonly List<GameObject> generatedStoryButtons = new List<GameObject>();

    public static GameModeSelectionPanel CreateRuntime(MainMenuController owner, Button styleSource)
    {
        if (owner == null || styleSource == null)
            return null;

        RectTransform root = CreateRect("GameModePanel", owner.transform);
        Stretch(root);
        root.SetAsLastSibling();

        Image blocker = root.gameObject.AddComponent<Image>();
        blocker.color = new Color(0.01f, 0.03f, 0.055f, 0.96f);
        blocker.raycastTarget = true;

        GameModeSelectionPanel panel = root.gameObject.AddComponent<GameModeSelectionPanel>();
        panel.menu = owner;
        panel.buttonStyleSource = styleSource;
        panel.Build();
        root.gameObject.SetActive(false);
        return panel;
    }

    public void ShowModeChoices()
    {
        gameObject.SetActive(true);
        if (modeChoices != null) modeChoices.SetActive(true);
        if (storyChoices != null) storyChoices.SetActive(false);
        SetStatus(string.Empty);

        Button endlessButton = modeChoices != null ? modeChoices.transform.Find("EndlessButton")?.GetComponent<Button>() : null;
        if (endlessButton != null)
            endlessButton.interactable = WaveManager.Instance != null && WaveManager.Instance.HasEndlessConfiguration;
    }

    private void Build()
    {
        RectTransform card = CreateRect("ModeCard", transform);
        SetCentered(card, new Vector2(760f, 760f));
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = new Color(0.025f, 0.09f, 0.14f, 0.99f);
        cardImage.raycastTarget = true;

        TMP_Text title = CreateText("Title", card, "SELECT MODE", 48f, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(650f, 70f));

        statusText = CreateText("Status", card, string.Empty, 18f, FontStyles.Bold);
        statusText.color = new Color(0.35f, 0.85f, 1f, 1f);
        SetRect(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(650f, 52f));

        BuildModeChoices(card);
        BuildStoryChoices(card);
    }

    private void BuildModeChoices(RectTransform card)
    {
        RectTransform root = CreateRect("ModeChoices", card);
        Stretch(root);
        modeChoices = root.gameObject;

        CreateDescription(root, "StoryDescription", "STORY\nChoose a level with configured enemy waves.", new Vector2(0f, 205f));
        Button story = CloneButton("StoryButton", root, "STORY", new Vector2(0f, 120f));
        story.onClick.AddListener(ShowStoryChoices);

        CreateDescription(root, "EndlessDescription", "ENDLESS\nEnemy groups grow from small to very large.", new Vector2(0f, 10f));
        Button endless = CloneButton("EndlessButton", root, "ENDLESS", new Vector2(0f, -75f));
        endless.onClick.AddListener(StartEndless);

        Button back = CloneButton("BackButton", root, "BACK", new Vector2(0f, -245f));
        back.onClick.AddListener(() => menu?.CloseGameModeSelection());
    }

    private void BuildStoryChoices(RectTransform card)
    {
        RectTransform root = CreateRect("StoryChoices", card);
        Stretch(root);
        storyChoices = root.gameObject;

        storyHeading = CreateText("Heading", root, "SELECT STORY LEVEL", 28f, FontStyles.Bold);
        SetRect(storyHeading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(600f, 50f));

        RectTransform viewport = CreateRect("Viewport", root);
        viewport.anchorMin = new Vector2(0.5f, 0.5f);
        viewport.anchorMax = new Vector2(0.5f, 0.5f);
        viewport.pivot = new Vector2(0.5f, 0.5f);
        viewport.anchoredPosition = new Vector2(0f, -15f);
        viewport.sizeDelta = new Vector2(600f, 390f);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.gameObject.AddComponent<RectMask2D>();

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        storyContent = CreateRect("Content", viewport);
        storyContent.anchorMin = new Vector2(0f, 1f);
        storyContent.anchorMax = new Vector2(1f, 1f);
        storyContent.pivot = new Vector2(0.5f, 1f);
        storyContent.anchoredPosition = Vector2.zero;
        storyContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = storyContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = storyContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = storyContent;

        storyButtonTemplate = CloneButton("StoryLevelTemplate", storyContent, "STORY LEVEL", Vector2.zero);
        LayoutElement templateLayout = storyButtonTemplate.gameObject.GetComponent<LayoutElement>() ?? storyButtonTemplate.gameObject.AddComponent<LayoutElement>();
        templateLayout.preferredHeight = 64f;
        templateLayout.minHeight = 64f;
        storyButtonTemplate.gameObject.SetActive(false);

        Button back = CloneButton("StoryBackButton", root, "BACK", new Vector2(0f, -255f));
        back.onClick.AddListener(ShowModeChoices);
        root.gameObject.SetActive(false);
    }

    private void ShowStoryChoices()
    {
        if (WaveManager.Instance == null)
        {
            SetStatus("WAVE MANAGER NOT FOUND");
            return;
        }

        RebuildStoryButtons();
        modeChoices.SetActive(false);
        storyChoices.SetActive(true);
        SetStatus("COMPLETE A LEVEL TO UNLOCK THE NEXT ONE");
    }

    private void RebuildStoryButtons()
    {
        for (int i = 0; i < generatedStoryButtons.Count; i++)
        {
            if (generatedStoryButtons[i] == null) continue;
            generatedStoryButtons[i].SetActive(false);
            Destroy(generatedStoryButtons[i]);
        }
        generatedStoryButtons.Clear();

        int count = WaveManager.Instance.StoryLevelCount;
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        int unlockedCount = profile != null ? Mathf.Min(count, profile.HighestUnlockedStoryLevel) : 1;
        if (storyHeading != null)
            storyHeading.text = $"STORY  •  UNLOCKED {unlockedCount}/{count}";

        for (int i = 0; i < count; i++)
        {
            int levelIndex = i;
            bool unlocked = profile != null ? profile.IsStoryLevelUnlocked(i) : i == 0;
            bool completed = profile != null && i + 1 < profile.HighestUnlockedStoryLevel;
            Button button = Instantiate(storyButtonTemplate, storyContent);
            button.name = $"StoryLevel{i + 1}Button";
            button.onClick = new Button.ButtonClickedEvent();
            string levelName = WaveManager.Instance.GetStoryLevelName(i);
            SetButtonLabel(button, completed ? $"{levelName}  -  COMPLETED" : unlocked ? levelName : $"{levelName}  -  LOCKED");
            button.interactable = unlocked;
            button.onClick.AddListener(() => StartStory(levelIndex));
            button.gameObject.SetActive(true);
            generatedStoryButtons.Add(button.gameObject);
        }
    }

    private void StartStory(int levelIndex)
    {
        PlayerProfileManager profile = PlayerProfileManager.Instance;
        if (levelIndex > 0 && (profile == null || !profile.IsStoryLevelUnlocked(levelIndex)))
        {
            SetStatus($"COMPLETE STORY LEVEL {levelIndex} FIRST");
            return;
        }

        if (WaveManager.Instance == null || !WaveManager.Instance.ConfigureStoryMode(levelIndex))
        {
            SetStatus("THIS STORY LEVEL HAS NO WAVES");
            return;
        }

        menu?.PlayGame();
    }

    private void StartEndless()
    {
        if (WaveManager.Instance == null || !WaveManager.Instance.ConfigureEndlessMode())
        {
            SetStatus("ADD AT LEAST ONE ENDLESS ENEMY RULE");
            return;
        }

        menu?.PlayGame();
    }

    private Button CloneButton(string objectName, Transform parent, string label, Vector2 position)
    {
        Button button = Instantiate(buttonStyleSource, parent);
        button.name = objectName;
        button.onClick = new Button.ButtonClickedEvent();
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(440f, 68f);
        rect.localScale = Vector3.one;
        SetButtonLabel(button, label);
        return button;
    }

    private void CreateDescription(Transform parent, string objectName, string value, Vector2 position)
    {
        TMP_Text text = CreateText(objectName, parent, value, 19f, FontStyles.Normal);
        text.color = new Color(0.72f, 0.82f, 0.88f, 1f);
        SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(650f, 80f));
    }

    private TMP_Text CreateText(string objectName, Transform parent, string value, float size, FontStyles style)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        TMP_Text source = buttonStyleSource.GetComponentInChildren<TMP_Text>(true);
        if (source != null) text.font = source.font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetCentered(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetButtonLabel(Button button, string value)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = value;
            label.raycastTarget = false;
        }
    }

    private void SetStatus(string value)
    {
        if (statusText != null) statusText.text = value;
    }
}
