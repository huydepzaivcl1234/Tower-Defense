#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies one Aurora visual language to the complete game UI while preserving existing hierarchy,
/// references, sprites, anchors and designer-authored layout. Gameplay HUD is rebuilt by the dedicated
/// safe gameplay tool; all other menus receive a non-destructive skin pass.
/// </summary>
public static class AuroraAllUISetupTool
{
    private static readonly Color Deep = Hex("#071724F2");
    private static readonly Color Panel = Hex("#0B2434F2");
    private static readonly Color PanelAlt = Hex("#0F3042F2");
    private static readonly Color Border = Hex("#2585A7FF");
    private static readonly Color Cyan = Hex("#39E5FFFF");
    private static readonly Color Text = Hex("#F4FBFFFF");
    private static readonly Color Muted = Hex("#A6C5D1FF");
    private static readonly Color Gold = Hex("#FFD45CFF");
    private static readonly Color Danger = Hex("#B8434FFF");

    [MenuItem("Tower Defense/UI/Apply Aurora ALL UI")]
    public static void ApplyAll()
    {
        bool gameplayApplied = AuroraSafeUIUpgradeTool.RepairAndUpgradeGameplay(false);

        int styledRoots = 0;

        MainMenuController main = FindSceneObject<MainMenuController>();
        if (main != null)
        {
            styledRoots += StyleRoot(main.mainPanel, "MAIN MENU");
            styledRoots += StyleRoot(main.settingsPanel, "SETTINGS");
            styledRoots += StyleRoot(main.profilePanel, "PROFILE");
        }

        PauseMenuController pause = FindSceneObject<PauseMenuController>();
        if (pause != null)
            styledRoots += StyleRoot(pause.pausePanel, "PAUSE");

        EndGameUIController end = FindSceneObject<EndGameUIController>();
        if (end != null)
            styledRoots += StyleRoot(end.rootPanel, "END GAME");

        RelicChoiceUI relic = FindSceneObject<RelicChoiceUI>();
        if (relic != null)
            styledRoots += StyleRoot(relic.panelRoot, "RELIC");

        PlayerProfilePanel profile = FindSceneObject<PlayerProfilePanel>();
        if (profile != null)
            styledRoots += StyleRoot(profile.gameObject, "PROFILE");

        DialogueHUDPresentationController dialogue = FindSceneObject<DialogueHUDPresentationController>();
        if (dialogue != null)
            styledRoots += StyleRoot(dialogue.gameObject, "DIALOGUE");

        QuestLiveHUD quest = FindSceneObject<QuestLiveHUD>();
        if (quest != null)
        {
            Undo.RecordObject(quest, "Apply Aurora Quest HUD");
            quest.cardColor = Deep;
            quest.headerColor = Cyan;
            quest.titleColor = Text;
            quest.progressColor = Muted;
            quest.completeCardColor = new Color(0.035f, 0.38f, 0.24f, 0.97f);
            quest.completeTextColor = new Color(0.76f, 1f, 0.84f, 1f);
            EditorUtility.SetDirty(quest);
            styledRoots++;
        }

        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || EditorUtility.IsPersistent(canvas) || !canvas.gameObject.scene.IsValid())
                continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                continue;

            Undo.RecordObject(scaler, "Normalize Aurora UI Scaling");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (scaler.referenceResolution.x < 100f || scaler.referenceResolution.y < 100f)
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(scaler);
        }

        if (main != null && main.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(main.gameObject.scene);
            EditorSceneManager.SaveScene(main.gameObject.scene);
        }
        else
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        EditorUtility.DisplayDialog(
            "Aurora ALL UI",
            $"Aurora pass complete.\n\nGameplay HUD rebuilt safely: {(gameplayApplied ? "YES" : "NO")}\nMenu/UI roots styled: {styledRoots}\n\nLayouts, references and assigned sprites were preserved.",
            "OK");
    }

    private static int StyleRoot(GameObject root, string category)
    {
        if (root == null)
            return 0;

        Undo.RegisterFullObjectHierarchyUndo(root, "Apply Aurora " + category);

        Image[] images = root.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null)
                continue;

            Button ownerButton = image.GetComponent<Button>();
            if (ownerButton != null)
            {
                StyleButton(ownerButton);
                continue;
            }

            if (image.sprite != null)
                continue;

            string n = image.gameObject.name.ToLowerInvariant();
            if (!LooksLikeContainer(n))
                continue;

            image.color = n.Contains("card") || n.Contains("box") ? PanelAlt : Panel;
            image.raycastTarget = image.GetComponent<Selectable>() != null;

            Outline outline = image.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(image.gameObject);
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
            StyleButton(button);

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            string n = text.gameObject.name.ToLowerInvariant();
            string value = (text.text ?? string.Empty).ToLowerInvariant();

            if (n.Contains("title") || n.Contains("header"))
            {
                text.color = Cyan;
                text.fontStyle |= FontStyles.Bold;
            }
            else if (n.Contains("cost") || n.Contains("gold") || n.Contains("diamond") || value.Contains("diamond"))
            {
                text.color = Gold;
            }
            else if (n.Contains("label") || n.Contains("subtitle") || n.Contains("description"))
            {
                text.color = Muted;
            }
            else if (text.color.a > 0.01f)
            {
                // Preserve rarity/special colored text. Only normalize nearly-white/grey UI copy.
                float max = Mathf.Max(text.color.r, Mathf.Max(text.color.g, text.color.b));
                float min = Mathf.Min(text.color.r, Mathf.Min(text.color.g, text.color.b));
                if (max - min < 0.14f)
                    text.color = Text;
            }
        }

        TMP_InputField[] inputs = root.GetComponentsInChildren<TMP_InputField>(true);
        foreach (TMP_InputField input in inputs)
        {
            if (input == null)
                continue;
            Image image = input.GetComponent<Image>();
            if (image != null && image.sprite == null)
            {
                image.color = Deep;
                Outline outline = image.GetComponent<Outline>();
                if (outline == null)
                    outline = Undo.AddComponent<Outline>(image.gameObject);
                outline.effectColor = Border;
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }

        Slider[] sliders = root.GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            if (slider == null)
                continue;
            if (slider.fillRect != null)
            {
                Image fill = slider.fillRect.GetComponent<Image>();
                if (fill != null && fill.sprite == null)
                    fill.color = Cyan;
            }
            if (slider.handleRect != null)
            {
                Image handle = slider.handleRect.GetComponent<Image>();
                if (handle != null && handle.sprite == null)
                    handle.color = Text;
            }
        }

        EditorUtility.SetDirty(root);
        return 1;
    }

    private static void StyleButton(Button button)
    {
        if (button == null)
            return;

        Image image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();

        if (image != null && image.sprite == null)
        {
            string n = button.gameObject.name.ToLowerInvariant();
            if (n.Contains("exit") || n.Contains("delete") || n.Contains("reset"))
                image.color = Danger;
            else if (n.Contains("play") || n.Contains("continue") || n.Contains("retry") || n.Contains("upgrade") || n.Contains("start"))
                image.color = new Color(0.035f, 0.48f, 0.62f, 1f);
            else
                image.color = PanelAlt;

            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(button.gameObject);
            outline.effectColor = Border;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        if (button.targetGraphic != null)
            button.targetGraphic.raycastTarget = true;
        Graphic ownGraphic = button.GetComponent<Graphic>();
        if (ownGraphic != null)
            ownGraphic.raycastTarget = true;

        button.transition = Selectable.Transition.None;

        UIPunchButton punch = button.GetComponent<UIPunchButton>();
        if (punch == null)
            punch = Undo.AddComponent<UIPunchButton>(button.gameObject);
        punch.hoverScale = 1.035f;
        punch.pressedScale = 0.95f;
        punch.hoverDuration = 0.10f;
        punch.hoverBrightness = 1.10f;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = Text;
            label.fontStyle |= FontStyles.Bold;
        }
    }

    private static bool LooksLikeContainer(string n)
    {
        return n.Contains("panel") ||
               n.Contains("card") ||
               n.Contains("background") ||
               n.Contains("backdrop") ||
               n.Contains("box") ||
               n.Contains("frame") ||
               n.Contains("content");
    }

    private static T FindSceneObject<T>() where T : Object
    {
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in all)
        {
            if (obj == null || EditorUtility.IsPersistent(obj))
                continue;

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
