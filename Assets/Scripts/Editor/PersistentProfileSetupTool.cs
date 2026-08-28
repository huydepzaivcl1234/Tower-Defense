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

        MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (menu == null)
        {
            EditorUtility.DisplayDialog(
                "Persistent Profile",
                "PlayerProfileManager was created, but MainMenuController was not found. Reset Data UI was not changed.",
                "OK");
            Selection.activeGameObject = profile.gameObject;
            return;
        }

        SetupResetDataButton(menu);
        SetupDiamondHud(profile);

        EditorUtility.SetDirty(profile);
        EditorUtility.SetDirty(menu);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

        Selection.activeGameObject = profile.gameObject;
        EditorGUIUtility.PingObject(profile.gameObject);

        EditorUtility.DisplayDialog(
            "Persistent Profile Ready",
            "Created/updated:\n\n" +
            "• PlayerProfileManager persistent Diamond save\n" +
            "• Diamond HUD under the current HUD canvas\n" +
            "• RESET DATA button inside the existing Settings panel\n\n" +
            "All references and presentation values remain editable in Inspector. Assign your own Diamond Sprite in DiamondHUD.",
            "OK");
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
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 48f);
            rect.anchoredPosition = new Vector2(0f, 100f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.22f, 0.055f, 0.055f, 0.96f);

            button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.88f, 1f);
            colors.pressedColor = new Color(0.78f, 0.72f, 0.72f, 1f);
            button.colors = colors;

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = menu.resetDataNormalLabel;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 18f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.raycastTarget = false;

            menu.resetDataButtonText = text;
        }
        else if (menu.resetDataButtonText == null)
        {
            menu.resetDataButtonText = button.GetComponentInChildren<TMP_Text>(true);
        }

        menu.resetDataButton = button;

        if (menu.resetDataStatusText == null)
        {
            Transform statusExisting = FindDeepChild(menu.settingsPanel.transform, "ResetDataStatus");
            TMP_Text status = statusExisting != null ? statusExisting.GetComponent<TMP_Text>() : null;
            if (status == null)
            {
                GameObject go = new GameObject("ResetDataStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(go, "Create Reset Data Status");
                go.transform.SetParent(menu.settingsPanel.transform, false);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(420f, 30f);
                rect.anchoredPosition = new Vector2(0f, 64f);

                status = go.GetComponent<TextMeshProUGUI>();
                status.alignment = TextAlignmentOptions.Center;
                status.fontSize = 14f;
                status.color = new Color(0.75f, 0.92f, 1f, 1f);
                status.text = string.Empty;
                status.raycastTarget = false;
            }
            menu.resetDataStatusText = status;
        }
    }

    private static void SetupDiamondHud(PlayerProfileManager profile)
    {
        DiamondHUD hud = Object.FindAnyObjectByType<DiamondHUD>(FindObjectsInactive.Include);
        if (hud != null)
        {
            EditorUtility.SetDirty(hud);
            return;
        }

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
            return;

        GameObject root = new GameObject("DiamondHUD", typeof(RectTransform), typeof(Image), typeof(DiamondHUD));
        Undo.RegisterCreatedObjectUndo(root, "Create Diamond HUD");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(190f, 46f);
        rect.anchoredPosition = new Vector2(14f, -108f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.025f, 0.075f, 0.11f, 0.94f);
        bg.raycastTarget = false;

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

        GameObject textGo = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(root.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(48f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI value = textGo.GetComponent<TextMeshProUGUI>();
        value.alignment = TextAlignmentOptions.MidlineRight;
        value.fontSize = 20f;
        value.fontStyle = FontStyles.Bold;
        value.color = Color.white;
        value.raycastTarget = false;
        value.text = CompactNumber.Format(profile != null ? profile.CurrentDiamonds : 0);

        hud = root.GetComponent<DiamondHUD>();
        hud.valueText = value;
        hud.iconImage = icon;
        hud.hideIconWhenNoSprite = true;

        EditorUtility.SetDirty(hud);
    }

    private static Canvas FindHudCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas fallback = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                continue;

            if (fallback == null)
                fallback = canvas;

            string n = canvas.name.ToLowerInvariant();
            if (n.Contains("hud") || n.Contains("qol"))
                return canvas;
        }
        return fallback;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
#endif
