#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PersistentProfileSetupTool
{
    private static readonly Color Panel2 = new Color(0.063f, 0.145f, 0.196f, 0.933f);
    private static readonly Color TextColor = new Color(0.937f, 0.969f, 0.98f, 1f);
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
        if (menu != null)
            SetupResetDataButton(menu);

        HUDManager hudManager = Object.FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include);
        Canvas gameplayCanvas = GetGameplayCanvas(hudManager);
        bool diamondHudReady = SetupDiamondHud(profile, hudManager);
        bool toastReady = SetupDiamondGainToast(gameplayCanvas);
        SetupWinDiamondSummary();

        EditorUtility.SetDirty(profile);
        if (menu != null) EditorUtility.SetDirty(menu);
        if (hudManager != null) EditorUtility.SetDirty(hudManager);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(profile.gameObject.scene);

        Selection.activeGameObject = diamondHudReady
            ? Object.FindAnyObjectByType<DiamondHUD>(FindObjectsInactive.Include)?.gameObject
            : profile.gameObject;

        string hudResult = diamondHudReady
            ? "DiamondRow attached directly to the existing Gold/Lives ResourceHUD."
            : "WARNING: HUDManager Gold/Lives ResourceHUD was not found, so no Diamond HUD was moved/created.";
        string toastResult = toastReady
            ? "Diamond gain toast is on the exact HUDManager gameplay Canvas."
            : "WARNING: gameplay Canvas was not found, so the Diamond toast was not created.";

        EditorUtility.DisplayDialog(
            "Diamond System Setup",
            hudResult + "\n\n" + toastResult + "\n\n" +
            "Also checked:\n" +
            "• Persistent Diamond save\n" +
            "• Enemy DiamondDropSystem\n" +
            "• YOU WIN Diamond earned + total labels\n" +
            "• RESET DATA in Settings\n\n" +
            "No Relic/MainMenu/Pause/EndGame Canvas is used as a fallback for gameplay Diamond UI.",
            "OK");
    }

    private static bool SetupDiamondHud(PlayerProfileManager profile, HUDManager hudManager)
    {
        if (hudManager == null || hudManager.goldText == null || hudManager.livesText == null)
        {
            Debug.LogError("PersistentProfileSetupTool: HUDManager with assigned Gold/Lives text is required for Diamond HUD setup.");
            return false;
        }

        Transform goldRow = hudManager.goldText.transform.parent;
        Transform livesRow = hudManager.livesText.transform.parent;
        if (goldRow == null || livesRow == null || goldRow.parent == null || livesRow.parent != goldRow.parent)
        {
            Debug.LogError("PersistentProfileSetupTool: GoldRow and LivesRow do not share the same ResourceHUD parent.", hudManager);
            return false;
        }

        RectTransform resourceHud = goldRow.parent as RectTransform;
        if (resourceHud == null)
        {
            Debug.LogError("PersistentProfileSetupTool: ResourceHUD is not a RectTransform.", hudManager);
            return false;
        }

        Undo.RecordObject(resourceHud, "Resize Resource HUD For Diamonds");
        Vector2 size = resourceHud.sizeDelta;
        size.x = Mathf.Max(size.x, 320f);
        size.y = Mathf.Max(size.y, 206f);
        resourceHud.sizeDelta = size;
        resourceHud.gameObject.SetActive(true);

        DiamondHUD hud = Object.FindAnyObjectByType<DiamondHUD>(FindObjectsInactive.Include);
        Sprite existingSprite = hud != null ? hud.diamondIcon : null;

        if (hud == null)
        {
            Transform existing = resourceHud.Find("DiamondRow");
            if (existing != null)
                hud = existing.GetComponent<DiamondHUD>();
        }

        if (hud == null)
        {
            GameObject row = new GameObject("DiamondRow", typeof(RectTransform), typeof(Image), typeof(DiamondHUD));
            Undo.RegisterCreatedObjectUndo(row, "Create Diamond Row");
            row.transform.SetParent(resourceHud, false);
            hud = row.GetComponent<DiamondHUD>();
        }
        else if (hud.transform.parent != resourceHud)
        {
            Undo.SetTransformParent(hud.transform, resourceHud, "Move Diamond HUD Into Resource HUD");
        }

        GameObject root = hud.gameObject;
        root.name = "DiamondRow";
        root.SetActive(true);
        root.transform.SetAsLastSibling();

        RectTransform rowRect = root.GetComponent<RectTransform>();
        if (rowRect == null)
            rowRect = Undo.AddComponent<RectTransform>(root);
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(0f, 1f);
        rowRect.pivot = new Vector2(0f, 1f);
        rowRect.sizeDelta = new Vector2(304f, 58f);
        rowRect.anchoredPosition = new Vector2(8f, -136f);
        rowRect.localScale = Vector3.one;

        Image background = root.GetComponent<Image>();
        if (background == null)
            background = Undo.AddComponent<Image>(root);
        background.color = Panel2;
        background.raycastTarget = false;

        Image icon = EnsureDiamondIcon(root.transform);
        TMP_Text label = EnsureText(root.transform, "Label", "Diamonds", 22f, TextColor, TextAlignmentOptions.MidlineLeft);
        TMP_Text value = EnsureText(root.transform, "Value", "0", 29f, DiamondColor, TextAlignmentOptions.MidlineRight);
        value.fontStyle = FontStyles.Bold;

        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.sizeDelta = new Vector2(40f, 40f);
        iconRect.anchoredPosition = new Vector2(13f, -9f);

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.sizeDelta = new Vector2(110f, 48f);
        labelRect.anchoredPosition = new Vector2(64f, -5f);

        RectTransform valueRect = value.rectTransform;
        valueRect.anchorMin = valueRect.anchorMax = new Vector2(0f, 1f);
        valueRect.pivot = new Vector2(0f, 1f);
        valueRect.sizeDelta = new Vector2(112f, 48f);
        valueRect.anchoredPosition = new Vector2(178f, -5f);

        hud.valueText = value;
        hud.iconImage = icon;
        if (existingSprite != null)
            hud.diamondIcon = existingSprite;
        hud.hideIconWhenNoSprite = true;
        hud.previewValue = profile != null ? profile.CurrentDiamonds : 0;

        icon.sprite = hud.diamondIcon;
        icon.preserveAspect = true;
        icon.enabled = hud.diamondIcon != null || !hud.hideIconWhenNoSprite;
        value.text = CompactNumber.Format(profile != null ? profile.CurrentDiamonds : 0);

        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(resourceHud);
        return true;
    }

    private static Image EnsureDiamondIcon(Transform parent)
    {
        Transform found = parent.Find("Icon");
        GameObject go = found != null ? found.gameObject : new GameObject("Icon", typeof(RectTransform), typeof(Image));
        if (found == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create Diamond Icon");
            go.transform.SetParent(parent, false);
        }

        Image image = go.GetComponent<Image>();
        if (image == null)
            image = Undo.AddComponent<Image>(go);
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static bool SetupDiamondGainToast(Canvas canvas)
    {
        if (canvas == null)
        {
            Debug.LogError("PersistentProfileSetupTool: exact HUDManager gameplay Canvas not found. Toast setup aborted instead of using a wrong Canvas.");
            return false;
        }

        DiamondGainToast toast = Object.FindAnyObjectByType<DiamondGainToast>(FindObjectsInactive.Include);
        bool created = toast == null;

        if (created)
        {
            GameObject root = new GameObject("DiamondGainToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(DiamondGainToast));
            Undo.RegisterCreatedObjectUndo(root, "Create Diamond Gain Toast");
            root.transform.SetParent(canvas.transform, false);
            toast = root.GetComponent<DiamondGainToast>();

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(230f, 48f);
            rect.anchoredPosition = new Vector2(-240f, -84f);

            Image bg = root.GetComponent<Image>();
            bg.color = new Color(0.025f, 0.075f, 0.11f, 0.96f);
            bg.raycastTarget = false;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            iconRect.anchoredPosition = new Vector2(28f, 0f);

            TMP_Text amount = EnsureText(root.transform, "Amount", "+1", 21f, Color.white, TextAlignmentOptions.Center);
            RectTransform amountRect = amount.rectTransform;
            amountRect.anchorMin = Vector2.zero;
            amountRect.anchorMax = Vector2.one;
            amountRect.offsetMin = new Vector2(55f, 0f);
            amountRect.offsetMax = new Vector2(-12f, 0f);

            toast.root = rect;
            toast.canvasGroup = root.GetComponent<CanvasGroup>();
            toast.iconImage = icon;
            toast.amountText = amount;
            toast.hiddenOffset = new Vector2(0f, 60f);
        }
        else if (toast.transform.GetComponentInParent<Canvas>() != canvas)
        {
            Undo.SetTransformParent(toast.transform, canvas.transform, "Move Diamond Toast To Gameplay Canvas");
        }

        toast.gameObject.SetActive(true);
        if (toast.root == null) toast.root = toast.transform as RectTransform;
        if (toast.canvasGroup == null) toast.canvasGroup = toast.GetComponent<CanvasGroup>();
        if (toast.canvasGroup == null) toast.canvasGroup = Undo.AddComponent<CanvasGroup>(toast.gameObject);
        toast.HideInstant();
        EditorUtility.SetDirty(toast);
        return true;
    }

    private static Canvas GetGameplayCanvas(HUDManager hudManager)
    {
        if (hudManager == null)
            return null;

        Canvas canvas = hudManager.GetComponentInParent<Canvas>(true);
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            return null;

        string n = canvas.name.ToLowerInvariant();
        if (n.Contains("relic") || n.Contains("menu") || n.Contains("pause") || n.Contains("endgame"))
        {
            Debug.LogError($"PersistentProfileSetupTool: HUDManager resolved to suspicious Canvas '{canvas.name}'. Setup will not use it.", hudManager);
            return null;
        }

        return canvas;
    }

    private static void SetupWinDiamondSummary()
    {
        EndGameUIController end = Object.FindAnyObjectByType<EndGameUIController>(FindObjectsInactive.Include);
        if (end == null || end.winContent == null)
            return;

        if (end.diamondsEarnedText == null)
        {
            Transform found = FindDeepChild(end.winContent.transform, "DiamondsEarned");
            TMP_Text text = found != null ? found.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                text = EnsureText(end.winContent.transform, "DiamondsEarned", "+0 DIAMONDS", 22f, DiamondColor, TextAlignmentOptions.Center);
                RectTransform rect = text.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(420f, 36f);
                rect.anchoredPosition = new Vector2(0f, -22f);
            }
            end.diamondsEarnedText = text;
        }

        if (end.diamondsTotalText == null)
        {
            Transform found = FindDeepChild(end.winContent.transform, "DiamondsTotal");
            TMP_Text text = found != null ? found.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                text = EnsureText(end.winContent.transform, "DiamondsTotal", "TOTAL 0", 16f, new Color(0.78f, 0.9f, 0.96f, 1f), TextAlignmentOptions.Center);
                RectTransform rect = text.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(420f, 30f);
                rect.anchoredPosition = new Vector2(0f, -55f);
            }
            end.diamondsTotalText = text;
        }

        EditorUtility.SetDirty(end);
    }

    private static void SetupResetDataButton(MainMenuController menu)
    {
        if (menu == null || menu.settingsPanel == null)
            return;

        Button button = menu.resetDataButton;
        if (button == null)
        {
            Transform existing = FindDeepChild(menu.settingsPanel.transform, "ResetDataButton");
            if (existing != null)
                button = existing.GetComponent<Button>();
        }

        if (button == null)
        {
            GameObject go = new GameObject("ResetDataButton", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Create Reset Data Button");
            go.transform.SetParent(menu.settingsPanel.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 48f);
            rect.anchoredPosition = new Vector2(0f, 100f);
            go.GetComponent<Image>().color = new Color(0.22f, 0.055f, 0.055f, 0.96f);
            button = go.GetComponent<Button>();

            TMP_Text text = EnsureText(go.transform, "Label", menu.resetDataNormalLabel, 18f, Color.white, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.fontStyle = FontStyles.Bold;
            menu.resetDataButtonText = text;
        }
        else if (menu.resetDataButtonText == null)
        {
            menu.resetDataButtonText = button.GetComponentInChildren<TMP_Text>(true);
        }

        menu.resetDataButton = button;

        if (menu.resetDataStatusText == null)
        {
            Transform found = FindDeepChild(menu.settingsPanel.transform, "ResetDataStatus");
            TMP_Text status = found != null ? found.GetComponent<TMP_Text>() : null;
            if (status == null)
            {
                status = EnsureText(menu.settingsPanel.transform, "ResetDataStatus", string.Empty, 14f, new Color(0.75f, 0.92f, 1f, 1f), TextAlignmentOptions.Center);
                RectTransform rect = status.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(420f, 30f);
                rect.anchoredPosition = new Vector2(0f, 64f);
            }
            menu.resetDataStatusText = status;
        }
    }

    private static TMP_Text EnsureText(Transform parent, string name, string defaultText, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null ? found.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        if (found == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
        }

        TMP_Text text = go.GetComponent<TMP_Text>();
        if (text == null)
            text = Undo.AddComponent<TextMeshProUGUI>(go);
        if (string.IsNullOrEmpty(text.text))
            text.text = defaultText;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
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
