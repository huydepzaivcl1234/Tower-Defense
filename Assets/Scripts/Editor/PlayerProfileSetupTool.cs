#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PlayerProfileSetupTool
{
    [MenuItem("Tower Defense/Data/Setup Persistent Profile + Reset Data")]
    public static void Setup()
    {
        PlayerProfileManager profile = Object.FindAnyObjectByType<PlayerProfileManager>(FindObjectsInactive.Include);
        if (profile == null)
        {
            GameObject go = new GameObject("PlayerProfileManager");
            Undo.RegisterCreatedObjectUndo(go, "Create Player Profile Manager");
            profile = go.AddComponent<PlayerProfileManager>();
        }

        MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (menu != null)
            EnsureResetDataButton(menu);

        EditorUtility.SetDirty(profile);
        if (menu != null) EditorUtility.SetDirty(menu);
        Selection.activeGameObject = profile.gameObject;
        EditorGUIUtility.PingObject(profile.gameObject);

        EditorUtility.DisplayDialog(
            "Persistent Profile Ready",
            "PlayerProfileManager is ready. Diamonds are now persistent and reserved for meta/shop use.\n\n" +
            (menu != null
                ? "Reset Data has been assigned in the existing Settings panel. First click arms confirmation; second click confirms."
                : "No MainMenuController was found, so the profile was created without editing UI."),
            "OK");
    }

    private static void EnsureResetDataButton(MainMenuController menu)
    {
        if (menu == null || menu.settingsPanel == null)
            return;

        if (menu.resetDataButton != null)
        {
            if (menu.resetDataButtonText == null)
                menu.resetDataButtonText = menu.resetDataButton.GetComponentInChildren<TMP_Text>(true);
            return;
        }

        Transform existing = FindDeepChild(menu.settingsPanel.transform, "ResetDataButton");
        GameObject buttonGo;
        if (existing != null)
        {
            buttonGo = existing.gameObject;
        }
        else
        {
            buttonGo = new GameObject("ResetDataButton", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(buttonGo, "Create Reset Data Button");
            buttonGo.transform.SetParent(menu.settingsPanel.transform, false);
        }

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(270f, 48f);
        rect.anchoredPosition = new Vector2(0f, 28f);

        Image image = buttonGo.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.34f, 0.07f, 0.08f, 0.96f);
            image.raycastTarget = true;
        }

        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.85f, 0.85f, 1f);
        colors.pressedColor = new Color(0.82f, 0.65f, 0.65f, 1f);
        button.colors = colors;

        TMP_Text label = buttonGo.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(buttonGo.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            label = tmp;
        }

        label.text = string.IsNullOrWhiteSpace(menu.resetDataNormalLabel)
            ? "RESET DATA"
            : menu.resetDataNormalLabel;

        menu.resetDataButton = button;
        menu.resetDataButtonText = label;

        // Keep runtime listener ownership inside MainMenuController.Start().
        // The tool intentionally does not write a persistent UnityEvent, preventing duplicate callbacks.
        EditorUtility.SetDirty(menu);
        EditorUtility.SetDirty(buttonGo);
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
