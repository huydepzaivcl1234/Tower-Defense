#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-destructive Aurora UI repair/upgrade. Restores original gameplay controls where they can be
/// identified safely, preserves assigned sprites/icons and existing layout, then adds visual polish.
/// </summary>
public static class AuroraSafeUIUpgradeTool
{
    private static readonly Color Border = Hex("#2585A7FF");
    private static readonly Color Cyan = Hex("#39E5FFFF");
    private static readonly Color Text = Hex("#F4FBFFFF");
    private static readonly Color Muted = Hex("#A6C5D1FF");

    [MenuItem("Tower Defense/UI/Repair + Upgrade Existing UI Safely")]
    public static void RepairAndUpgradeMenu()
    {
        RepairAndUpgradeGameplay(true);
        UpgradeAllExistingMenus();
    }

    public static bool RepairAndUpgradeGameplay(bool showDialog)
    {
        HUDManager hud = FindSceneObject<HUDManager>();
        if (hud == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Safe UI Repair", "HUDManager not found.", "OK");
            return false;
        }

        Canvas canvas = hud.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Safe UI Repair", "HUDManager is not under a Canvas.", "OK");
            return false;
        }

        BuildMenuUI buildMenu = canvas.GetComponentInChildren<BuildMenuUI>(true);
        TowerUpgradeUI upgrade = canvas.GetComponentInChildren<TowerUpgradeUI>(true);

        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Repair and upgrade existing UI");

        int recovered = 0;
        recovered += RecoverHudReferences(canvas, hud);
        recovered += RecoverBuildMenuReferences(buildMenu);

        // Keep the currently referenced upgrade panel if we cannot prove an older one is the original.
        // This avoids destroying valid designer work.
        if (upgrade != null && upgrade.panelRoot != null)
            StyleHierarchyInPlace(upgrade.panelRoot);

        StyleHierarchyInPlace(canvas.gameObject);

        if (buildMenu != null && buildMenu.towerButtons != null)
        {
            foreach (BuildMenuUI.TowerButtonBinding binding in buildMenu.towerButtons)
            {
                if (binding == null || binding.button == null) continue;
                EnsureClickable(binding.button);
                StyleButton(binding.button);
            }
            EditorUtility.SetDirty(buildMenu);
        }

        EditorUtility.SetDirty(hud);
        if (upgrade != null) EditorUtility.SetDirty(upgrade);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Safe UI Repair",
                $"Repair finished.\n\nRecovered original references: {recovered}\nExisting sprites/icons were preserved.\nNo layout was rebuilt and no generated root was deleted automatically.\n\nTest Build Deck clicks in Play Mode.",
                "OK");
        }

        return true;
    }

    [MenuItem("Tower Defense/UI/Upgrade ALL Existing Menus In Place")]
    public static void UpgradeAllExistingMenus()
    {
        MainMenuController main = FindSceneObject<MainMenuController>();
        if (main != null)
        {
            StyleHierarchyInPlace(main.mainPanel);
            StyleHierarchyInPlace(main.settingsPanel);
            StyleHierarchyInPlace(main.profilePanel);
        }

        PauseMenuController pause = FindSceneObject<PauseMenuController>();
        if (pause != null) StyleHierarchyInPlace(pause.pausePanel);

        EndGameUIController end = FindSceneObject<EndGameUIController>();
        if (end != null) StyleHierarchyInPlace(end.rootPanel);

        RelicChoiceUI relic = FindSceneObject<RelicChoiceUI>();
        if (relic != null) StyleHierarchyInPlace(relic.panelRoot);

        PlayerProfilePanel profile = FindSceneObject<PlayerProfilePanel>();
        if (profile != null) StyleHierarchyInPlace(profile.gameObject);

        DialogueHUDPresentationController dialogue = FindSceneObject<DialogueHUDPresentationController>();
        if (dialogue != null) StyleHierarchyInPlace(dialogue.gameObject);

        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static int RecoverHudReferences(Canvas canvas, HUDManager hud)
    {
        int count = 0;

        TMP_Text gold = FindLegacyText(canvas.transform, "gold");
        TMP_Text lives = FindLegacyText(canvas.transform, "lives", "life");
        TMP_Text wave = FindLegacyText(canvas.transform, "wave");
        Button start = FindLegacyButton(canvas.transform, "start", "wave");

        if (gold != null && hud.goldText != gold)
        {
            hud.goldText = gold;
            gold.gameObject.SetActive(true);
            count++;
        }
        if (lives != null && hud.livesText != lives)
        {
            hud.livesText = lives;
            lives.gameObject.SetActive(true);
            count++;
        }
        if (wave != null && hud.waveText != wave)
        {
            hud.waveText = wave;
            wave.gameObject.SetActive(true);
            count++;
        }
        if (start != null && hud.startWaveButton != start)
        {
            hud.startWaveButton = start;
            start.gameObject.SetActive(true);
            EnsureClickable(start);
            count++;
        }

        return count;
    }

    private static int RecoverBuildMenuReferences(BuildMenuUI buildMenu)
    {
        if (buildMenu == null || buildMenu.towerButtons == null)
            return 0;

        Button[] allButtons = buildMenu.GetComponentsInChildren<Button>(true);
        List<Button> legacy = new List<Button>();

        foreach (Button button in allButtons)
        {
            if (button == null || IsUnderGeneratedRoot(button.transform))
                continue;
            legacy.Add(button);
        }

        int recovered = 0;
        HashSet<Button> used = new HashSet<Button>();

        foreach (BuildMenuUI.TowerButtonBinding binding in buildMenu.towerButtons)
        {
            if (binding == null || binding.towerData == null)
                continue;

            Button match = FindBestTowerButton(legacy, used, binding.towerData);
            if (match == null)
                continue;

            used.Add(match);
            if (binding.button != match)
            {
                binding.button = match;
                recovered++;
            }

            match.gameObject.SetActive(true);
            EnsureClickable(match);
            StyleButton(match);

            // Do not replace image/icon sprites. Only reconnect existing authored text when present.
            TMP_Text[] texts = match.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t == null) continue;
                string n = t.name.ToLowerInvariant();
                if (binding.nameText == null && n.Contains("name")) binding.nameText = t;
                if (binding.costText == null && n.Contains("cost")) binding.costText = t;
            }
        }

        return recovered;
    }

    private static Button FindBestTowerButton(List<Button> candidates, HashSet<Button> used, TowerData tower)
    {
        string towerName = Normalize(tower.towerName);

        foreach (Button button in candidates)
        {
            if (button == null || used.Contains(button)) continue;

            if (Normalize(button.name).Contains(towerName))
                return button;

            TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text != null && Normalize(text.text).Contains(towerName))
                    return button;
            }
        }

        // Conservative fallback: only use order when candidate count matches binding count closely.
        foreach (Button button in candidates)
        {
            if (button != null && !used.Contains(button))
                return button;
        }

        return null;
    }

    private static TMP_Text FindLegacyText(Transform root, params string[] keywords)
    {
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null || IsUnderGeneratedRoot(text.transform))
                continue;

            string key = (text.name + " " + text.text).ToLowerInvariant();
            bool match = true;
            foreach (string keyword in keywords)
            {
                if (!key.Contains(keyword.ToLowerInvariant()))
                {
                    match = false;
                    break;
                }
            }
            if (match) return text;
        }
        return null;
    }

    private static Button FindLegacyButton(Transform root, params string[] keywords)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null || IsUnderGeneratedRoot(button.transform))
                continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string key = (button.name + " " + (label != null ? label.text : string.Empty)).ToLowerInvariant();
            bool match = true;
            foreach (string keyword in keywords)
            {
                if (!key.Contains(keyword.ToLowerInvariant()))
                {
                    match = false;
                    break;
                }
            }
            if (match) return button;
        }
        return null;
    }

    private static bool IsUnderGeneratedRoot(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "CleanUIRoot")
                return true;
            current = current.parent;
        }
        return false;
    }

    private static void StyleHierarchyInPlace(GameObject root)
    {
        if (root == null) return;

        Undo.RegisterFullObjectHierarchyUndo(root, "Upgrade UI in place");

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            EnsureClickable(button);
            StyleButton(button);
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null) continue;

            // Critical rule: assigned sprites/icons are designer content. Never replace or recolor them.
            if (image.sprite != null)
                continue;

            string n = image.name.ToLowerInvariant();
            if (!LooksLikeContainer(n))
                continue;

            Outline outline = image.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(image.gameObject);
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null) continue;
            string n = text.name.ToLowerInvariant();
            if (n.Contains("title") || n.Contains("header"))
            {
                text.fontStyle |= FontStyles.Bold;
                // Only recolor neutral text, never rarity/special authored colors.
                if (IsNeutral(text.color))
                    text.color = Cyan;
            }
            else if (IsNeutral(text.color))
            {
                text.color = n.Contains("label") || n.Contains("description") ? Muted : Text;
            }
        }

        EditorUtility.SetDirty(root);
    }

    private static void StyleButton(Button button)
    {
        if (button == null) return;

        EnsureClickable(button);
        button.transition = Selectable.Transition.None;

        UIPunchButton punch = button.GetComponent<UIPunchButton>();
        if (punch == null)
            punch = Undo.AddComponent<UIPunchButton>(button.gameObject);
        punch.hoverScale = 1.035f;
        punch.pressedScale = .95f;
        punch.hoverDuration = .10f;
        punch.hoverBrightness = 1.10f;

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            // Preserve button sprite and its authored color. Add outline only.
            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(button.gameObject);
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }
    }

    private static void EnsureClickable(Button button)
    {
        if (button == null) return;

        if (button.targetGraphic != null)
            button.targetGraphic.raycastTarget = true;

        Graphic ownGraphic = button.GetComponent<Graphic>();
        if (ownGraphic != null)
            ownGraphic.raycastTarget = true;
    }

    private static bool LooksLikeContainer(string n)
    {
        return n.Contains("panel") || n.Contains("card") || n.Contains("background") ||
               n.Contains("backdrop") || n.Contains("box") || n.Contains("frame");
    }

    private static bool IsNeutral(Color c)
    {
        float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
        return max - min < .14f;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        char[] chars = value.ToLowerInvariant().ToCharArray();
        System.Text.StringBuilder sb = new System.Text.StringBuilder(chars.Length);
        foreach (char ch in chars)
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        return sb.ToString();
    }

    private static T FindSceneObject<T>() where T : UnityEngine.Object
    {
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in all)
        {
            if (obj == null || EditorUtility.IsPersistent(obj)) continue;
            Component component = obj as Component;
            if (component != null && component.gameObject.scene.IsValid())
                return obj;
        }
        return null;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
#endif
