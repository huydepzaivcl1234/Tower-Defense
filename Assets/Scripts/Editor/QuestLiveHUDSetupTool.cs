#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class QuestLiveHUDSetupTool
{
    [MenuItem("Tower Defense/Quest/Setup Quest Live HUD")]
    public static void SetupQuestLiveHUD()
    {
        QuestLiveHUD existing = Object.FindAnyObjectByType<QuestLiveHUD>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            Debug.Log("Quest Live HUD already exists. Selected existing object.");
            return;
        }

        Canvas targetCanvas = FindPreferredCanvas();
        if (targetCanvas == null)
        {
            GameObject canvasGO = new GameObject("QuestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Quest Canvas");
            targetCanvas = canvasGO.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 210;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject root = new GameObject("QuestLiveHUD", typeof(RectTransform), typeof(QuestLiveHUD));
        Undo.RegisterCreatedObjectUndo(root, "Create Quest Live HUD");
        root.transform.SetParent(targetCanvas.transform, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(24f, 160f);
        rect.sizeDelta = new Vector2(430f, 360f);

        QuestLiveHUD hud = root.GetComponent<QuestLiveHUD>();
        hud.anchor = Vector2.zero;
        hud.pivot = Vector2.zero;
        hud.anchoredPosition = rect.anchoredPosition;
        hud.rootSize = rect.sizeDelta;

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(targetCanvas.gameObject);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("Quest Live HUD created. Position, card size, fonts, colors, labels and every animation timing can be tuned on QuestLiveHUD in the Inspector.");
    }

    private static Canvas FindPreferredCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        Canvas fallback = null;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.scene.IsValid()) continue;

            if (canvas.name == "QoLCanvas") return canvas;
            if (canvas.name == "HudCanvas" || canvas.name == "HUDCanvas") fallback = canvas;
            else if (fallback == null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) fallback = canvas;
        }

        return fallback;
    }
}
#endif
