#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIButtonSFXSetupTool
{
    [MenuItem("Tower Defense/UI/Setup Button SFX")]
    public static void SetupButtonSfx()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int added = 0;
        int already = 0;

        foreach (Button button in buttons)
        {
            if (button == null) continue;
            if (button.GetComponent<UIButtonSFX>() != null)
            {
                already++;
                continue;
            }

            Undo.AddComponent<UIButtonSFX>(button.gameObject);
            added++;
        }

        EditorUtility.DisplayDialog(
            "Button SFX Ready",
            $"Added UIButtonSFX to {added} buttons.\nAlready configured: {already}.\n\nAssign a different Click Clip / Volume / Pitch on each button in the Inspector.",
            "OK");
    }
}
#endif
