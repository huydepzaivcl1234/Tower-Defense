#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerProfilePageSetupTool
{
    private static readonly Color Backdrop = new Color(0.01f, 0.025f, 0.045f, 0.72f);
    private static readonly Color Card = new Color(0.025f, 0.075f, 0.11f, 0.97f);
    private static readonly Color Accent = new Color(0.20f, 0.82f, 1f, 1f);

    [MenuItem("Tower Defense/Data/Setup Player Profile Page")]
    public static void Setup()
    {
        MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.mainPanel == null)
        {
            EditorUtility.DisplayDialog("Player Profile Setup", "MainMenuController with Main Panel is required.", "OK");
            return;
        }

        PlayerProfileManager profile = Object.FindAnyObjectByType<PlayerProfileManager>(FindObjectsInactive.Include);
        if (profile == null)
        {
            GameObject profileGo = new GameObject("PlayerProfileManager");
            Undo.RegisterCreatedObjectUndo(profileGo, "Create PlayerProfileManager");
            profile = profileGo.AddComponent<PlayerProfileManager>();
        }

        Transform menuParent = menu.mainPanel.transform.parent;
        if (menuParent == null)
        {
            EditorUtility.DisplayDialog("Player Profile Setup", "Main Panel must have a parent UI container.", "OK");
            return;
        }

        bool createdPanel = menu.profilePanel == null;
        PlayerProfilePanel panelController = null;
        GameObject panelRoot;

        if (menu.profilePanel != null)
        {
            panelRoot = menu.profilePanel;
            panelController = panelRoot.GetComponent<PlayerProfilePanel>();
            if (panelController == null)
                panelController = Undo.AddComponent<PlayerProfilePanel>(panelRoot);
        }
        else
        {
            Transform existing = menuParent.Find("ProfilePanel");
            panelRoot = existing != null
                ? existing.gameObject
                : new GameObject("ProfilePanel", typeof(RectTransform), typeof(Image), typeof(PlayerProfilePanel));

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(panelRoot, "Create Profile Panel");
                panelRoot.transform.SetParent(menuParent, false);
            }

            panelController = panelRoot.GetComponent<PlayerProfilePanel>();
            if (panelController == null)
                panelController = Undo.AddComponent<PlayerProfilePanel>(panelRoot);
            menu.profilePanel = panelRoot;
        }

        RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
        if (createdPanel || rootRect.anchorMin == rootRect.anchorMax)
            Stretch(rootRect);

        Image rootImage = panelRoot.GetComponent<Image>();
        if (rootImage == null) rootImage = Undo.AddComponent<Image>(panelRoot);
        if (createdPanel) rootImage.color = Backdrop;
        rootImage.raycastTarget = true;

        Transform cardTransform = panelRoot.transform.Find("ProfileCard");
        GameObject cardGo = cardTransform != null
            ? cardTransform.gameObject
            : new GameObject("ProfileCard", typeof(RectTransform), typeof(Image));
        if (cardTransform == null)
        {
            Undo.RegisterCreatedObjectUndo(cardGo, "Create Profile Card");
            cardGo.transform.SetParent(panelRoot.transform, false);
        }

        RectTransform cardRect = cardGo.GetComponent<RectTransform>();
        if (createdPanel || cardTransform == null)
        {
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(720f, 470f);
            cardRect.anchoredPosition = Vector2.zero;
        }

        Image cardImage = cardGo.GetComponent<Image>();
        if (cardImage == null) cardImage = Undo.AddComponent<Image>(cardGo);
        if (createdPanel || cardTransform == null) cardImage.color = Card;
        cardImage.raycastTarget = true;

        TMP_Text title = EnsureText(cardGo.transform, "Title", "PLAYER PROFILE");
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(480f, 48f));
        title.alignment = TextAlignmentOptions.Center;
        title.fontSize = 30f;
        title.fontStyle = FontStyles.Bold;
        title.color = Accent;

        Image avatar = EnsureImage(cardGo.transform, "Avatar");
        SetRect(avatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(145f, 55f), new Vector2(180f, 180f));
        avatar.preserveAspect = true;
        avatar.raycastTarget = false;

        Button prevAvatar = EnsureButton(cardGo.transform, "PreviousAvatarButton", "<");
        SetRect(prevAvatar.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(45f, 55f), new Vector2(48f, 48f));

        Button nextAvatar = EnsureButton(cardGo.transform, "NextAvatarButton", ">");
        SetRect(nextAvatar.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(245f, 55f), new Vector2(48f, 48f));

        TMP_InputField nameInput = EnsureInputField(cardGo.transform, "PlayerNameInput", "Player name");
        SetRect(nameInput.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(125f, -112f), new Vector2(330f, 52f));

        TMP_Text playTime = EnsureText(cardGo.transform, "PlayTime", "PLAY TIME  00:00:00");
        TMP_Text diamonds = EnsureText(cardGo.transform, "Diamonds", "DIAMONDS  0");
        TMP_Text kills = EnsureText(cardGo.transform, "EnemiesKilled", "ENEMIES KILLED  0");

        SetStatText(playTime, new Vector2(125f, 16f));
        SetStatText(diamonds, new Vector2(125f, -54f));
        SetStatText(kills, new Vector2(125f, -124f));
        diamonds.color = Accent;

        Button close = EnsureButton(cardGo.transform, "CloseButton", "BACK");
        SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(230f, 52f));

        panelController.avatarImage = avatar;
        panelController.playerNameInput = nameInput;
        panelController.playTimeText = playTime;
        panelController.diamondsText = diamonds;
        panelController.enemiesKilledText = kills;
        panelController.previousAvatarButton = prevAvatar;
        panelController.nextAvatarButton = nextAvatar;
        panelController.closeButton = close;

        SetupProfileButton(menu);

        panelRoot.SetActive(false);
        EditorUtility.SetDirty(panelController);
        EditorUtility.SetDirty(menu);
        EditorUtility.SetDirty(profile);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(menu.gameObject.scene);

        Selection.activeGameObject = panelRoot;
        EditorGUIUtility.PingObject(panelRoot);

        EditorUtility.DisplayDialog(
            "Player Profile Ready",
            "Profile page created/updated.\n\n" +
            "Saved data: player name, avatar index, total play time, Diamonds and lifetime enemy kills.\n\n" +
            "Assign your avatar Sprites in ProfilePanel > PlayerProfilePanel > Avatar Library. " +
            "Existing avatar list and manually assigned Sprite are preserved when setup is run again.",
            "OK");
    }

    private static void SetupProfileButton(MainMenuController menu)
    {
        if (menu.profileButton != null)
            return;

        Transform found = menu.mainPanel.transform.Find("ProfileButton");
        Button button = found != null ? found.GetComponent<Button>() : null;

        if (button == null)
        {
            Button template = menu.settingsButton != null ? menu.settingsButton : menu.playButton;
            if (template != null)
            {
                GameObject clone = Object.Instantiate(template.gameObject, menu.mainPanel.transform);
                Undo.RegisterCreatedObjectUndo(clone, "Create Profile Button");
                clone.name = "ProfileButton";
                button = clone.GetComponent<Button>();
                button.onClick.RemoveAllListeners();

                TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true);
                if (text != null) text.text = "PROFILE";

                RectTransform rect = clone.GetComponent<RectTransform>();
                if (menu.settingsButton != null)
                {
                    RectTransform settingsRect = menu.settingsButton.GetComponent<RectTransform>();
                    rect.anchoredPosition = settingsRect.anchoredPosition + new Vector2(0f, 56f);
                }
            }
            else
            {
                button = EnsureButton(menu.mainPanel.transform, "ProfileButton", "PROFILE");
                RectTransform rect = button.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(280f, 48f);
                rect.anchoredPosition = new Vector2(0f, -20f);
            }
        }

        menu.profileButton = button;
    }

    private static TMP_Text EnsureText(Transform parent, string name, string initial)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (found == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }

        TMP_Text text = go.GetComponent<TMP_Text>();
        if (text == null) text = Undo.AddComponent<TextMeshProUGUI>(go);
        if (string.IsNullOrEmpty(text.text)) text.text = initial;
        text.raycastTarget = false;
        return text;
    }

    private static Image EnsureImage(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        if (found == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }

        Image image = go.GetComponent<Image>();
        if (image == null) image = Undo.AddComponent<Image>(go);
        return image;
    }

    private static Button EnsureButton(Transform parent, string name, string label)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        if (found == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }

        Image image = go.GetComponent<Image>();
        if (image == null) image = Undo.AddComponent<Image>(go);
        if (found == null) image.color = new Color(0.03f, 0.28f, 0.42f, 0.98f);

        Button button = go.GetComponent<Button>();
        if (button == null) button = Undo.AddComponent<Button>(go);

        TMP_Text text = go.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            text = EnsureText(go.transform, "Label", label);
            Stretch(text.rectTransform);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18f;
            text.fontStyle = FontStyles.Bold;
        }
        else if (found == null)
        {
            text.text = label;
        }
        return button;
    }

    private static TMP_InputField EnsureInputField(Transform parent, string name, string placeholderText)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        if (found == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }

        Image image = go.GetComponent<Image>();
        if (image == null) image = Undo.AddComponent<Image>(go);
        if (found == null) image.color = new Color(0.015f, 0.045f, 0.07f, 0.98f);

        TMP_InputField input = go.GetComponent<TMP_InputField>();
        if (input == null) input = Undo.AddComponent<TMP_InputField>(go);

        TMP_Text text = EnsureText(go.transform, "Text", "");
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(16f, 6f);
        text.rectTransform.offsetMax = new Vector2(-16f, -6f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.fontSize = 21f;
        text.color = Color.white;

        TMP_Text placeholder = EnsureText(go.transform, "Placeholder", placeholderText);
        Stretch(placeholder.rectTransform);
        placeholder.rectTransform.offsetMin = new Vector2(16f, 6f);
        placeholder.rectTransform.offsetMax = new Vector2(-16f, -6f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.fontSize = 21f;
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);

        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }

    private static void SetStatText(TMP_Text text, Vector2 anchoredPosition)
    {
        SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(370f, 48f));
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.fontSize = 21f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
