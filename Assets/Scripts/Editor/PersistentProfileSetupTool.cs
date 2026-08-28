#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PersistentProfileSetupTool
{
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

        Canvas gameplayCanvas = FindGameplayCanvas();
        SetupDiamondHud(profile, gameplayCanvas);
        SetupDiamondGainToast(gameplayCanvas);
        SetupWinDiamondSummary();

        EditorUtility.SetDirty(profile);
        if (menu != null) EditorUtility.SetDirty(menu);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(profile.gameObject.scene);

        Selection.activeGameObject = profile.gameObject;
        EditorGUIUtility.PingObject(profile.gameObject);

        EditorUtility.DisplayDialog(
            "Diamond System Ready",
            "Created/updated:\n\n" +
            "• Persistent Diamond save\n" +
            "• Diamond counter on the SAME Canvas as HUDManager\n" +
            "• Diamond gain slide toast\n" +
            "• Enemy DiamondDropSystem\n" +
            "• YOU WIN Diamond earned + total labels\n" +
            "• RESET DATA in Settings\n\n" +
            "Icons, positions, text, animation timings, drop chance/amount, prefab and win reward remain editable.",
            "OK");
    }

    private static void SetupResetDataButton(MainMenuController menu)
    {
        if (menu == null || menu.settingsPanel == null) return;

        Button button = menu.resetDataButton;
        if (button == null)
        {
            Transform existing = FindDeepChild(menu.settingsPanel.transform, "ResetDataButton");
            if (existing != null) button = existing.GetComponent<Button>();
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

            GameObject textGo = CreateText("Label", go.transform, 18f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(textGo.GetComponent<RectTransform>());
            TMP_Text text = textGo.GetComponent<TMP_Text>();
            text.text = menu.resetDataNormalLabel;
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
                GameObject go = CreateText("ResetDataStatus", menu.settingsPanel.transform, 14f, FontStyles.Normal, TextAlignmentOptions.Center);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(420f, 30f);
                rect.anchoredPosition = new Vector2(0f, 64f);
                status = go.GetComponent<TMP_Text>();
                status.color = new Color(0.75f, 0.92f, 1f, 1f);
                status.text = string.Empty;
            }
            menu.resetDataStatusText = status;
        }
    }

    private static void SetupDiamondHud(PlayerProfileManager profile, Canvas canvas)
    {
        if (canvas == null) return;

        DiamondHUD hud = Object.FindAnyObjectByType<DiamondHUD>(FindObjectsInactive.Include);
        bool created = hud == null;
        if (created)
        {
            GameObject root = new GameObject("DiamondHUD", typeof(RectTransform), typeof(Image), typeof(DiamondHUD));
            Undo.RegisterCreatedObjectUndo(root, "Create Diamond HUD");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(190f, 46f);
            rect.anchoredPosition = new Vector2(14f, -112f);
            root.GetComponent<Image>().color = new Color(0.025f, 0.075f, 0.11f, 0.94f);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(30f, 30f);
            iconRect.anchoredPosition = new Vector2(24f, 0f);
            Image icon = iconGo.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            GameObject textGo = CreateText("Value", root.transform, 20f, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(48f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);

            hud = root.GetComponent<DiamondHUD>();
            hud.valueText = textGo.GetComponent<TMP_Text>();
            hud.iconImage = icon;
            hud.hideIconWhenNoSprite = true;
        }
        else
        {
            Canvas currentCanvas = hud.GetComponentInParent<Canvas>();
            if (currentCanvas != canvas)
            {
                RectTransform rect = hud.transform as RectTransform;
                Vector2 savedPos = rect != null ? rect.anchoredPosition : Vector2.zero;
                Undo.SetTransformParent(hud.transform, canvas.transform, "Move Diamond HUD To Gameplay Canvas");
                if (rect != null) rect.anchoredPosition = savedPos;
            }
        }

        hud.gameObject.SetActive(true);
        EditorUtility.SetDirty(hud);
    }

    private static void SetupDiamondGainToast(Canvas canvas)
    {
        if (canvas == null) return;
        DiamondGainToast toast = Object.FindAnyObjectByType<DiamondGainToast>(FindObjectsInactive.Include);
        if (toast != null) return;

        GameObject root = new GameObject("DiamondGainToast", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(DiamondGainToast));
        Undo.RegisterCreatedObjectUndo(root, "Create Diamond Gain Toast");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(230f, 48f);
        rect.anchoredPosition = new Vector2(-240f, -84f);
        root.GetComponent<Image>().color = new Color(0.025f, 0.075f, 0.11f, 0.96f);

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root.transform, false);
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(28f, 28f);
        iconRect.anchoredPosition = new Vector2(28f, 0f);

        GameObject textGo = CreateText("Amount", root.transform, 21f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform tr = textGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(55f, 0f); tr.offsetMax = new Vector2(-12f, 0f);

        toast = root.GetComponent<DiamondGainToast>();
        toast.root = rect;
        toast.canvasGroup = root.GetComponent<CanvasGroup>();
        toast.iconImage = iconGo.GetComponent<Image>();
        toast.amountText = textGo.GetComponent<TMP_Text>();
        toast.hiddenOffset = new Vector2(0f, 60f);
        toast.canvasGroup.alpha = 0f;
    }

    private static void SetupWinDiamondSummary()
    {
        EndGameUIController end = Object.FindAnyObjectByType<EndGameUIController>(FindObjectsInactive.Include);
        if (end == null || end.winContent == null) return;

        if (end.diamondsEarnedText == null)
        {
            Transform found = FindDeepChild(end.winContent.transform, "DiamondsEarned");
            TMP_Text text = found != null ? found.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                GameObject go = CreateText("DiamondsEarned", end.winContent.transform, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(420f, 36f);
                rect.anchoredPosition = new Vector2(0f, -22f);
                text = go.GetComponent<TMP_Text>();
                text.color = new Color(0.35f, 0.92f, 1f, 1f);
                text.text = "+0 DIAMONDS";
            }
            end.diamondsEarnedText = text;
        }

        if (end.diamondsTotalText == null)
        {
            Transform found = FindDeepChild(end.winContent.transform, "DiamondsTotal");
            TMP_Text text = found != null ? found.GetComponent<TMP_Text>() : null;
            if (text == null)
            {
                GameObject go = CreateText("DiamondsTotal", end.winContent.transform, 16f, FontStyles.Normal, TextAlignmentOptions.Center);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(420f, 30f);
                rect.anchoredPosition = new Vector2(0f, -55f);
                text = go.GetComponent<TMP_Text>();
                text.color = new Color(0.78f, 0.9f, 0.96f, 1f);
                text.text = "TOTAL 0";
            }
            end.diamondsTotalText = text;
        }
        EditorUtility.SetDirty(end);
    }

    private static Canvas FindGameplayCanvas()
    {
        HUDManager hudManager = Object.FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include);
        if (hudManager != null)
        {
            Canvas canvas = hudManager.GetComponentInParent<Canvas>(true);
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace) return canvas;
        }

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) continue;
            string n = canvas.name.ToLowerInvariant();
            if (n.Contains("hud") || n.Contains("qol")) return canvas;
        }
        return null;
    }

    private static GameObject CreateText(string name, Transform parent, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return go;
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
