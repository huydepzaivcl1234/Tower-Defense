#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds UIPunchButton to every Button in the currently open scene, including inactive UI.
/// Safe to run repeatedly.
/// </summary>
public static class LivelyUISetupTool
{
    [MenuItem("Tower Defense/UI/Setup Lively UI Feedback")]
    public static void Setup()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int added = 0;
        int existing = 0;

        foreach (Button button in buttons)
        {
            if (button == null) continue;
            if (button.GetComponent<UIPunchButton>() != null)
            {
                existing++;
                continue;
            }

            Undo.AddComponent<UIPunchButton>(button.gameObject);
            added++;
        }

        EditorUtility.DisplayDialog(
            "Lively UI",
            $"Setup complete.\n\nAdded punch feedback to {added} buttons.\nAlready configured: {existing}.\n\nGold count-up and low-lives pulse are handled automatically by HUDManager.",
            "OK");
    }
}
#endif
