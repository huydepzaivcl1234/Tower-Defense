#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds the optional custom-avatar (+) button to the existing PlayerProfilePanel without rebuilding the profile UI.
/// Existing positions/sprites/config are preserved when the tool is run again.
/// </summary>
public static class PlayerCustomAvatarSetupTool
{
    [MenuItem("Tower Defense/Data/Setup Custom Profile Avatar Picker")]
    public static void Setup()
    {
        PlayerProfilePanel panel = Object.FindAnyObjectByType<PlayerProfilePanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            EditorUtility.DisplayDialog(
                "Custom Avatar Setup",
                "PlayerProfilePanel was not found. Run Tower Defense → Data → Setup Player Profile Page first.",
                "OK");
            return;
        }

        Transform card = panel.transform.Find("ProfileCard");
        if (card == null)
            card = panel.transform;

        PlayerCustomAvatarPicker picker = panel.GetComponent<PlayerCustomAvatarPicker>();
        if (picker == null)
            picker = Undo.AddComponent<PlayerCustomAvatarPicker>(panel.gameObject);

        Button plusButton = null;
        Transform existing = card.Find("CustomAvatarButton");
        bool created = existing == null;

        if (existing != null)
        {
            plusButton = existing.GetComponent<Button>();
            if (plusButton == null)
                plusButton = Undo.AddComponent<Button>(existing.gameObject);
        }
        else
        {
            GameObject go = new GameObject("CustomAvatarButton", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Create Custom Avatar Button");
            go.transform.SetParent(card, false);
            plusButton = go.GetComponent<Button>();

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.03f, 0.36f, 0.52f, 0.98f);
            image.raycastTarget = true;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(44f, 44f);

            // Horizontally between < and >, placed just under the avatar so it does not cover the portrait.
            RectTransform avatarRect = panel.avatarImage != null ? panel.avatarImage.rectTransform : null;
            rect.anchoredPosition = avatarRect != null
                ? new Vector2(avatarRect.anchoredPosition.x, avatarRect.anchoredPosition.y - avatarRect.sizeDelta.y * 0.5f - 28f)
                : new Vector2(145f, -63f);
        }

        TMP_Text label = plusButton.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
        {
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelGo, "Create Custom Avatar Button Label");
            labelGo.transform.SetParent(plusButton.transform, false);
            label = labelGo.GetComponent<TextMeshProUGUI>();

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 28f;
            label.color = Color.white;
            label.raycastTarget = false;
        }
        label.text = "+";

        picker.avatarImage = panel.avatarImage;
        picker.customAvatarButton = plusButton;

        if (created)
            plusButton.transform.SetAsLastSibling();

        EditorUtility.SetDirty(picker);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(plusButton);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(panel.gameObject.scene);

        Selection.activeGameObject = plusButton.gameObject;
        EditorGUIUtility.PingObject(plusButton.gameObject);

        EditorUtility.DisplayDialog(
            "Custom Avatar Ready",
            "Added the + avatar picker button to the existing profile page.\n\n" +
            "< / > still select built-in avatars.\n" +
            "+ opens the Windows image library/file picker.\n" +
            "Selected images are normalized and stored under Application.persistentDataPath.\n\n" +
            "All limits, save folder/file name and picker text are configurable on PlayerCustomAvatarPicker.",
            "OK");
    }
}
#endif
