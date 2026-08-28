#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PersistentProfileSetupTool
{
    private static readonly Color Panel = new Color(0.025f, 0.075f, 0.11f, 0.94f);
    private static readonly Color DiamondColor = new Color(0.30f, 0.90f, 1f, 1f);

    [MenuItem("Tower Defense/Data/Setup Persistent Profile + Reset Data")]
    public static void Setup()
    {
        PlayerProfileManager profile = Object.FindAnyObjectByType<PlayerProfileManager>(FindObjectsInactive.Include);
        if (profile == null)
        {
            GameObject go = new GameObject("PlayerProfileManager");
            Undo.RegisterCreatedObjectUndo(go, "Create PlayerProfileManager");
            profile = go.AddComponent<PlayerProfileManager>();
        }

        if (profile.GetComponent<DiamondDropSystem>() == null)
            Undo.AddComponent<DiamondDropSystem>(profile.gameObject);

        MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
        HUDManager hudManager = Object.FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include);
        EndGameUIController endGame = Object.FindAnyObjectByType<EndGameUIController>(FindObjectsInactive.Include);

        bool menuHudReady = SetupMainMenuDiamondHud(profile, menu);
        bool gameplayToastReady = SetupGameplayDiamondToast(hudManager);
        SetupResetDataButton(menu);
        SetupWinDiamondSummary(endGame);

        EditorUtility.SetDirty(profile);
        if (menu != null) EditorUtility.SetDirty(menu);
        if (endGame != null) EditorUtility.SetDirty(endGame);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(profile.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(profile.gameObject.scene);

        DiamondHUD menuHud = FindMainMenuDiamondHud();
        Selection.activeGameObject = menuHud != null ? menuHud.gameObject : profile.gameObject;

        EditorUtility.DisplayDialog(
            "Diamond System Ready",
            (menuHudReady ? "Main Menu Diamond HUD ready.\n" : "WARNING: Main Menu HUD could not be created.\n") +
            (gameplayToastReady ? "Gameplay Diamond gain HUD ready.\n" : "WARNING: gameplay HUD canvas could not be resolved.\n") +
            "\nBehaviour:\n" +
            "• Main Menu always shows total Diamonds.\n" +
            "• Gameplay Diamond HUD stays hidden until Diamonds are gained.\n" +
            "• On gain it slides in, counts old total -> new total, then slides out.\n" +
            "• EnemyData.Diamond Drop Prefab accepts your own 3D Diamond model/prefab.\n" +
            "• Main Menu and gameplay notification icons are editable Sprites in Inspector.\n" +
            "• Scene was saved automatically.",
            "OK");
    }

    private static bool SetupMainMenuDiamondHud(PlayerProfileManager profile, MainMenuController menu)
    {
        if (menu == null || menu.mainPanel == null)
        {
            Debug.LogError("PersistentProfileSetupTool: MainMenuController.mainPanel is required for Main Menu Diamond HUD.");
            return false;
        }

        DiamondHUD hud = FindMainMenuDiamondHud();
        if (hud == null)
        {
            DiamondHUD[] all = Object.FindObjectsByType<DiamondHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                hud = all[i];
                break;
            }
        }

        Sprite preservedIcon = hud != null ? hud.diamondIcon : null;
        bool newlyCreated = hud == null;

        if (hud == null)
        {
            GameObject root = new GameObject("MainMenuDiamondHUD", typeof(RectTransform), typeof(Image), typeof(DiamondHUD));
            Undo.RegisterCreatedObjectUndo(root, "Create Main Menu Diamond HUD");
            root.transform.SetParent(menu.mainPanel.transform, false);
            hud = root.GetComponent<DiamondHUD>();
        }
        else
        {
            if (hud.transform.parent != menu.mainPanel.transform)
                Undo.SetTransformParent(hud.transform, menu.mainPanel.transform, "Move Diamond HUD To Main Menu");
            hud.gameObject.name = "MainMenuDiamondHUD";
        }

        GameObject go = hud.gameObject;
        go.SetActive(true);
        go.transform.SetAsLastSibling();

        RectTransform rect = go.GetComponent<RectTransform>();
        if (newlyCreated || go.name == "MainMenuDiamondHUD")
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(220f, 54f);
            if (newlyCreated || rect.anchoredPosition == Vector2.zero)
                rect.anchoredPosition = new Vector2(-26f, -24f);
            rect.localScale = Vector3.one;
        }

        Image bg = go.GetComponent<Image>();
        if (bg == null) bg = Undo.AddComponent<Image>(go);
        if (newlyCreated) bg.color = Panel;
        bg.raycastTarget = false;

        Image icon = EnsureImage(go.transform, "Icon");
        TMP_Text value = EnsureText(go.transform, "Value", "0");

        if (newlyCreated || icon.rectTransform.sizeDelta == Vector2.zero)
        {
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(34f, 34f);
            ir.anchoredPosition = new Vector2(28f, 0f);
        }

        RectTransform vr = value.rectTransform;
        vr.anchorMin = Vector2.zero;
        vr.anchorMax = Vector2.one;
        vr.offsetMin = new Vector2(56f, 0f);
        vr.offsetMax = new Vector2(-14f, 0f);
        value.alignment = TextAlignmentOptions.MidlineRight;
        value.fontSize = 24f;
        value.fontStyle = FontStyles.Bold;
        value.color = DiamondColor;
        value.raycastTarget = false;

        hud.valueText = value;
        hud.iconImage = icon;
        if (preservedIcon != null)
            hud.diamondIcon = preservedIcon;
        hud.hideIconWhenNoSprite = true;
        hud.previewValue = profile != null ? profile.CurrentDiamonds : 0;
        hud.Refresh();

        EditorUtility.SetDirty(hud);
        return true;
    }

    private static DiamondHUD FindMainMenuDiamondHud()
    {
        DiamondHUD[] all = Object.FindObjectsByType<DiamondHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            DiamondHUD hud = all[i];
            if (hud != null && hud.gameObject.name == "MainMenuDiamondHUD")
                return hud;
        }
        return null;
    }

    private static bool SetupGameplayDiamondToast(HUDManager hudManager)
    {
        Canvas canvas = GetGameplayCanvas(hudManager);
        if (canvas == null)
        {
            Debug.LogError("PersistentProfileSetupTool: gameplay Canvas containing HUDManager was not found.");
            return false;
        }

        DiamondGainToast toast = Object.FindAnyObjectByType<DiamondGainToast>(FindObjectsInactive.Include);
        bool created = toast == null;
        Sprite preservedIcon = toast != null ? toast.diamondIcon : null;

        if (toast == null)
        {
            GameObject root = new GameObject("DiamondGainToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(DiamondGainToast));
            Undo.RegisterCreatedObjectUndo(root, "Create Diamond Gain Toast");
            root.transform.SetParent(canvas.transform, false);
            toast = root.GetComponent<DiamondGainToast>();
        }
        else if (toast.GetComponentInParent<Canvas>() != canvas)
        {
            Undo.SetTransformParent(toast.transform, canvas.transform, "Move Diamond Gain Toast To Gameplay Canvas");
        }

        GameObject go = toast.gameObject;
        go.name = "DiamondGainToast";
        go.SetActive(true);
        go.transform.SetAsLastSibling();

        RectTransform rect = go.GetComponent<RectTransform>();
        if (created)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(290f, 62f);
            rect.anchoredPosition = new Vector2(-245f, -86f);
        }

        Image bg = go.GetComponent<Image>();
        if (bg == null) bg = Undo.AddComponent<Image>(go);
        if (created) bg.color = Panel;
        bg.raycastTarget = false;

        CanvasGroup group = go.GetComponent<CanvasGroup>();
        if (group == null) group = Undo.AddComponent<CanvasGroup>(go);
        group.interactable = false;
        group.blocksRaycasts = false;

        Image icon = EnsureImage(go.transform, "Icon");
        TMP_Text total = EnsureText(go.transform, "Total", "0");
        TMP_Text gain = EnsureText(go.transform, "Gain", "+1");

        if (created)
        {
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = ir.anchorMax = new Vector2(0f, 0.5f);
            ir.pivot = new Vector2(0.5f, 0.5f);
            ir.sizeDelta = new Vector2(34f, 34f);
            ir.anchoredPosition = new Vector2(30f, 0f);

            RectTransform tr = total.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.sizeDelta = new Vector2(130f, 44f);
            tr.anchoredPosition = new Vector2(15f, 0f);

            RectTransform gr = gain.rectTransform;
            gr.anchorMin = gr.anchorMax = new Vector2(1f, 0.5f);
            gr.pivot = new Vector2(1f, 0.5f);
            gr.sizeDelta = new Vector2(72f, 36f);
            gr.anchoredPosition = new Vector2(-14f, 0f);
        }

        total.alignment = TextAlignmentOptions.Center;
        total.fontSize = 24f;
        total.fontStyle = FontStyles.Bold;
        total.color = DiamondColor;
        total.raycastTarget = false;

        gain.alignment = TextAlignmentOptions.Center;
        gain.fontSize = 17f;
        gain.fontStyle = FontStyles.Bold;
        gain.color = Color.white;
        gain.raycastTarget = false;

        toast.root = rect;
        toast.canvasGroup = group;
        toast.totalText = total;
        toast.gainText = gain;
        toast.iconImage = icon;
        if (preservedIcon != null)
            toast.diamondIcon = preservedIcon;
        toast.hideIconWhenNoSprite = true;
        toast.HideInstant();

        EditorUtility.SetDirty(toast);
        return true;
    }

    private static Canvas GetGameplayCanvas(HUDManager hudManager)
    {
        if (hudManager == null) return null;
        Canvas canvas = hudManager.GetComponentInParent<Canvas>(true);
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) return null;
        return canvas;
    }

    private static void SetupWinDiamondSummary(EndGameUIController end)
    {
        if (end == null || end.winContent == null) return;

        if (end.diamondsEarnedText == null)
        {
            TMP_Text text = EnsureText(end.winContent.transform, "DiamondsEarned", "+0 DIAMONDS");
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 36f);
            rect.anchoredPosition = new Vector2(0f, -22f);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 22f;
            text.fontStyle = FontStyles.Bold;
            text.color = DiamondColor;
            end.diamondsEarnedText = text;
        }

        if (end.diamondsTotalText == null)
        {
            TMP_Text text = EnsureText(end.winContent.transform, "DiamondsTotal", "TOTAL 0");
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 30f);
            rect.anchoredPosition = new Vector2(0f, -55f);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 16f;
            text.color = Color.white;
            end.diamondsTotalText = text;
        }
    }

    private static void SetupResetDataButton(MainMenuController menu)
    {
        if (menu == null || menu.settingsPanel == null) return;

        Button button = menu.resetDataButton;
        if (button == null)
        {
            Transform found = FindDeepChild(menu.settingsPanel.transform, "ResetDataButton");
            if (found != null) button = found.GetComponent<Button>();
        }

        if (button == null)
        {
            GameObject go = new GameObject("ResetDataButton", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Create Reset Data Button");
            go.transform.SetParent(menu.settingsPanel.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(280f, 48f);
            rect.anchoredPosition = new Vector2(0f, 28f);
            go.GetComponent<Image>().color = new Color(0.34f, 0.07f, 0.08f, 0.96f);
            button = go.GetComponent<Button>();
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            label = EnsureText(button.transform, "Label", menu.resetDataNormalLabel);
            Stretch(label.rectTransform);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
        }

        menu.resetDataButton = button;
        menu.resetDataButtonText = label;
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
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static TMP_Text EnsureText(Transform parent, string name, string initialText)
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
        if (string.IsNullOrEmpty(text.text)) text.text = initialText;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
