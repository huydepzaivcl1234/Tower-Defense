#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor-only live preview for QuestLiveHUD.
/// Rebuilds a non-saved sample card whenever HUD settings change so layout can be tuned directly in Scene view.
/// </summary>
[CustomEditor(typeof(QuestLiveHUD))]
public class QuestLiveHUDEditor : Editor
{
    private const string PreviewName = "__QuestLiveHUD_EDITOR_PREVIEW__";

    static QuestLiveHUDEditor()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
    }

    private void OnEnable()
    {
        EditorApplication.delayCall += RebuildPreviewSafe;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool changed = EditorGUI.EndChangeCheck();

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Editor Live Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Preview card is editor-only and is not saved into the scene/build. Change position, size, fonts, padding and colors above to see the result immediately in Scene view.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Preview"))
                RebuildPreviewSafe();

            if (GUILayout.Button("Hide Preview"))
                DestroyPreview((QuestLiveHUD)target);
        }

        if (changed)
            EditorApplication.delayCall += RebuildPreviewSafe;
    }

    private void RebuildPreviewSafe()
    {
        if (this == null || target == null || Application.isPlaying)
            return;

        QuestLiveHUD hud = target as QuestLiveHUD;
        if (hud == null || hud.gameObject == null)
            return;

        BuildPreview(hud);
        SceneView.RepaintAll();
    }

    private static void BuildPreview(QuestLiveHUD hud)
    {
        DestroyPreview(hud);

        RectTransform hudRect = hud.transform as RectTransform;
        if (hudRect == null)
            return;

        // Keep the actual root RectTransform in sync immediately, matching QuestLiveHUD.OnValidate.
        hudRect.anchorMin = hud.anchor;
        hudRect.anchorMax = hud.anchor;
        hudRect.pivot = hud.pivot;
        hudRect.anchoredPosition = hud.anchoredPosition;
        hudRect.sizeDelta = hud.rootSize;

        GameObject card = new GameObject(
            PreviewName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));

        card.hideFlags = HideFlags.HideAndDontSave;
        card.transform.SetParent(hud.transform, false);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = hud.cardSize;
        rect.localScale = Vector3.one;

        Image background = card.GetComponent<Image>();
        background.color = hud.cardColor;
        background.raycastTarget = false;

        CanvasGroup group = card.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        TMP_Text header = CreatePreviewText(
            card.transform,
            "Header",
            string.IsNullOrEmpty(hud.headerText) ? "QUEST" : hud.headerText,
            hud.headerFontSize,
            hud.headerColor,
            hud.headerAlignment);

        TMP_Text title = CreatePreviewText(
            card.transform,
            "QuestTitle",
            "Preview Quest Title",
            hud.titleFontSize,
            hud.titleColor,
            hud.titleAlignment);

        string objective = hud.showObjectiveLabel
            ? (string.IsNullOrEmpty(hud.killLabel) ? "KILL" : hud.killLabel) + "  "
            : string.Empty;

        TMP_Text progress = CreatePreviewText(
            card.transform,
            "Progress",
            objective + "3/5",
            hud.progressFontSize,
            hud.progressColor,
            hud.progressAlignment);

        float left = hud.cardPadding.x;
        float right = hud.cardPadding.y;
        float top = hud.cardPadding.z;
        float bottom = hud.cardPadding.w;

        SetTextRect(header.rectTransform, left, right, hud.cardSize.y - top - 22f, 22f);
        SetTextRect(title.rectTransform, left, right + 110f, bottom + 8f, 42f);
        SetTextRect(progress.rectTransform, hud.cardSize.x - 150f, right, bottom + 8f, 42f);

        // Hide the preview hierarchy entries while keeping them visible in the Canvas/Scene view.
        SetHideFlagsRecursive(card, HideFlags.HideAndDontSave);
    }

    private static TMP_Text CreatePreviewText(
        Transform parent,
        string name,
        string value,
        int fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = Mathf.Max(8, fontSize);
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void SetTextRect(RectTransform rect, float left, float right, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(left, y);
        rect.offsetMax = new Vector2(-right, y + height);
    }

    private static void DestroyPreview(QuestLiveHUD hud)
    {
        if (hud == null) return;

        for (int i = hud.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = hud.transform.GetChild(i);
            if (child != null && child.name == PreviewName)
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void SetHideFlagsRecursive(GameObject root, HideFlags flags)
    {
        if (root == null) return;
        root.hideFlags = flags;
        for (int i = 0; i < root.transform.childCount; i++)
            SetHideFlagsRecursive(root.transform.GetChild(i).gameObject, flags);
    }

    private static void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        QuestLiveHUD[] huds = Object.FindObjectsByType<QuestLiveHUD>(FindObjectsInactive.Include);
        for (int i = 0; i < huds.Length; i++)
            DestroyPreview(huds[i]);
    }
}
#endif
