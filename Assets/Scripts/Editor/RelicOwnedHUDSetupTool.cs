#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click setup for the owned relic HUD. Reuses QoLCanvas when available,
/// otherwise creates a small overlay canvas. Refuses to create duplicate RelicOwnedHUD instances.
/// </summary>
public static class RelicOwnedHUDSetupTool
{
    [MenuItem("Tower Defense/UI/Setup Owned Relic HUD")]
    public static void Setup()
    {
        RelicOwnedHUD existing = Object.FindAnyObjectByType<RelicOwnedHUD>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog(
                "Owned Relic HUD",
                "RelicOwnedHUD already exists. No duplicate was created.\n\nSelected the existing HUD instead.",
                "OK");
            return;
        }

        RelicManager relicManager = Object.FindAnyObjectByType<RelicManager>(FindObjectsInactive.Include);
        if (relicManager == null)
        {
            EditorUtility.DisplayDialog(
                "Owned Relic HUD",
                "RelicManager was not found in the scene. Set up the relic system first.",
                "OK");
            return;
        }

        Canvas canvas = FindOrCreateCanvas();

        GameObject hudGO = new GameObject("RelicOwnedHUD", typeof(RectTransform), typeof(RelicOwnedHUD));
        Undo.RegisterCreatedObjectUndo(hudGO, "Create Owned Relic HUD");
        hudGO.transform.SetParent(canvas.transform, false);

        RectTransform rect = hudGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 24f);
        rect.sizeDelta = new Vector2(580f, 76f);

        EditorUtility.SetDirty(hudGO);
        EditorUtility.SetDirty(canvas);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = hudGO;

        EditorUtility.DisplayDialog(
            "Owned Relic HUD",
            "Done.\n\n" +
            "• Bottom-left HUD shows up to 5 owned relics.\n" +
            "• Format: [icon] x stack.\n" +
            "• If more than 5 unique relics are owned, a + button appears.\n" +
            "• + opens a scrollable panel containing every owned relic and stack count.\n" +
            "• Uses the existing RelicManager and RelicData icons; no gameplay data was duplicated.",
            "OK");
    }

    private static Canvas FindOrCreateCanvas()
    {
        GameObject qoL = GameObject.Find("QoLCanvas");
        if (qoL != null && qoL.TryGetComponent(out Canvas existingCanvas))
            return existingCanvas;

        GameObject existing = GameObject.Find("RelicHUDCanvas");
        if (existing != null && existing.TryGetComponent(out Canvas relicCanvas))
            return relicCanvas;

        GameObject go = new GameObject("RelicHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Relic HUD Canvas");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 185;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }
}
#endif
