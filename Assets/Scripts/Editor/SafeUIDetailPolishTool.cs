#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Non-destructive detail pass for the existing Tower Defense UI.
/// Never replaces sprites, listeners, references, or original RectTransform values.
/// All added visual children use a stable UIDetail_* prefix and raycastTarget=false.
/// Safe to run repeatedly.
/// </summary>
public static class SafeUIDetailPolishTool
{
    private const string Prefix = "UIDetail_";
    private static readonly Color Cyan = Hex("#35DDF7");
    private static readonly Color CyanSoft = new Color(0.21f, 0.87f, 0.97f, 0.28f);
    private static readonly Color Gold = Hex("#FFD25A");
    private static readonly Color DarkShadow = new Color(0f, 0.03f, 0.06f, 0.62f);

    [MenuItem("Tower Defense/UI/Polish Existing UI - Safe Detail Pass")]
    public static void Apply()
    {
        int panels = 0;
        int buttons = 0;
        int titles = 0;

        MainMenuController main = FindSceneObject<MainMenuController>();
        if (main != null)
        {
            panels += DecoratePanel(main.mainPanel);
            panels += DecoratePanel(main.profilePanel);
            panels += DecoratePanel(main.settingsPanel);

            buttons += DecorateButton(main.playButton, true);
            buttons += DecorateButton(main.profileButton, false);
            buttons += DecorateButton(main.settingsButton, false);
            buttons += DecorateButton(main.exitButton, false);
            buttons += DecorateButton(main.backButton, false);
            buttons += DecorateButton(main.resetDataButton, false);
        }

        PauseMenuController pause = FindSceneObject<PauseMenuController>();
        if (pause != null)
        {
            panels += DecoratePanel(pause.pausePanel);
            buttons += DecorateButton(pause.continueButton, true);
            buttons += DecorateButton(pause.mainMenuButton, false);
            buttons += DecorateButton(pause.gearButton, false);
        }

        EndGameUIController end = FindSceneObject<EndGameUIController>();
        if (end != null)
        {
            panels += DecoratePanel(end.rootPanel);
            panels += DecoratePanel(end.winContent);
            panels += DecoratePanel(end.loseContent);
            buttons += DecorateButton(end.retryButton, true);
            buttons += DecorateButton(end.mainMenuButton, false);
        }

        PlayerProfilePanel profile = FindSceneObject<PlayerProfilePanel>();
        if (profile != null)
        {
            panels += DecoratePanel(profile.gameObject);
            buttons += DecorateButton(profile.previousAvatarButton, false);
            buttons += DecorateButton(profile.nextAvatarButton, false);
            buttons += DecorateButton(profile.closeButton, false);
            if (profile.avatarImage != null)
                AddFrameAround(profile.avatarImage.rectTransform, Gold, "AvatarFrame");
        }

        RelicChoiceUI relic = FindSceneObject<RelicChoiceUI>();
        if (relic != null)
        {
            panels += DecoratePanel(relic.panelRoot);
            titles += DecorateTitle(relic.titleText);
            if (relic.cards != null)
            {
                for (int i = 0; i < relic.cards.Length; i++)
                {
                    RelicChoiceUI.RelicCard card = relic.cards[i];
                    if (card == null) continue;
                    buttons += DecorateButton(card.button, true);
                    titles += DecorateTitle(card.nameText);
                    if (card.icon != null)
                        AddFrameAround(card.icon.rectTransform, Cyan, "RelicIconFrame_" + i);
                }
            }
        }

        HUDManager hud = FindSceneObject<HUDManager>();
        if (hud != null)
        {
            DecorateContainingPanel(hud.goldText, Gold);
            DecorateContainingPanel(hud.livesText, Cyan);
            DecorateContainingPanel(hud.waveText, Cyan);
            buttons += DecorateButton(hud.startWaveButton, true);
            titles += DecorateTitle(hud.waveText);
        }

        BuildMenuUI build = FindSceneObject<BuildMenuUI>();
        if (build != null && build.towerButtons != null)
        {
            foreach (BuildMenuUI.TowerButtonBinding binding in build.towerButtons)
            {
                if (binding == null || binding.button == null) continue;
                buttons += DecorateButton(binding.button, false);
                titles += DecorateTitle(binding.nameText);
                if (binding.selectedFrame != null)
                    AddFrameAround(binding.selectedFrame.transform as RectTransform, Cyan, "SelectedGlow");
            }
        }

        TowerUpgradeUI upgrade = FindSceneObject<TowerUpgradeUI>();
        if (upgrade != null)
        {
            panels += DecoratePanel(upgrade.panelRoot);
            panels += DecoratePanel(upgrade.nextLevelRoot);
            titles += DecorateTitle(upgrade.towerNameText);
            titles += DecorateTitle(upgrade.nextLevelTitleText);
            buttons += DecorateButton(upgrade.upgradeButton, true);
            buttons += DecorateButton(upgrade.sellButton, false);
            buttons += DecorateButton(upgrade.closeButton, false);
            buttons += DecorateButton(upgrade.secondaryCloseButton, false);
        }

        QuestLiveHUD quest = FindSceneObject<QuestLiveHUD>();
        if (quest != null)
        {
            Undo.RecordObject(quest, "Polish Quest HUD");
            // Keep layout and authored sizes. Only enrich existing theme values.
            quest.cardColor = BlendKeepAlpha(quest.cardColor, new Color(0.025f, 0.11f, 0.16f, 1f), 0.22f);
            quest.headerColor = BlendKeepAlpha(quest.headerColor, Cyan, 0.30f);
            quest.progressColor = BlendKeepAlpha(quest.progressColor, new Color(0.76f, 0.92f, 1f, 1f), 0.18f);
            EditorUtility.SetDirty(quest);
        }

        // Existing plugin/component: keep button feedback consistent without changing click ownership.
        Button[] allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in allButtons)
        {
            if (button == null || EditorUtility.IsPersistent(button)) continue;
            if (button.GetComponent<UIPunchButton>() == null)
                Undo.AddComponent<UIPunchButton>(button.gameObject);
        }

        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        EditorUtility.DisplayDialog(
            "Safe UI Detail Pass",
            $"Done.\n\nPanels detailed: {panels}\nButtons detailed: {buttons}\nTitles enhanced: {titles}\n\nNo original sprite, button reference, listener, or RectTransform was replaced.",
            "OK");
    }

    private static int DecoratePanel(GameObject root)
    {
        if (root == null) return 0;
        RectTransform rect = root.transform as RectTransform;
        if (rect == null) return 0;

        Undo.RegisterFullObjectHierarchyUndo(root, "Decorate existing UI panel");

        Image image = root.GetComponent<Image>();
        if (image != null)
        {
            Outline outline = root.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(root);
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.52f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            Shadow shadow = root.GetComponent<Shadow>();
            if (shadow == null)
                shadow = Undo.AddComponent<Shadow>(root);
            shadow.effectColor = DarkShadow;
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;
        }

        AddEdge(rect, "TopGlow", new Vector2(0.04f, 0.985f), new Vector2(0.96f, 1f), CyanSoft, true);
        AddEdge(rect, "BottomGlow", new Vector2(0.16f, 0f), new Vector2(0.84f, 0.006f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.16f), false);
        AddCorner(rect, "CornerTL", new Vector2(0f, 1f), new Vector2(1f, -1f));
        AddCorner(rect, "CornerTR", new Vector2(1f, 1f), new Vector2(-1f, -1f));
        return 1;
    }

    private static int DecorateButton(Button button, bool strong)
    {
        if (button == null) return 0;

        if (button.GetComponent<UIPunchButton>() == null)
            Undo.AddComponent<UIPunchButton>(button.gameObject);

        Image image = button.targetGraphic as Image;
        if (image != null)
        {
            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = Undo.AddComponent<Outline>(button.gameObject);
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, strong ? 0.75f : 0.42f);
            outline.effectDistance = strong ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        RectTransform rect = button.transform as RectTransform;
        if (rect != null)
        {
            Color accent = strong ? new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f) : new Color(Cyan.r, Cyan.g, Cyan.b, 0.30f);
            AddEdge(rect, "ButtonAccent", new Vector2(0.10f, 0f), new Vector2(0.90f, 0.045f), accent, strong);
        }

        return 1;
    }

    private static int DecorateTitle(TMP_Text text)
    {
        if (text == null) return 0;
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = Undo.AddComponent<Shadow>(text.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
        shadow.useGraphicAlpha = true;
        return 1;
    }

    private static void DecorateContainingPanel(Component child, Color accent)
    {
        if (child == null) return;
        Transform current = child.transform.parent;
        while (current != null)
        {
            Image image = current.GetComponent<Image>();
            RectTransform rect = current as RectTransform;
            if (image != null && rect != null)
            {
                Outline outline = current.GetComponent<Outline>();
                if (outline == null)
                    outline = Undo.AddComponent<Outline>(current.gameObject);
                outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.48f);
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;
                AddEdge(rect, "ResourceAccent", new Vector2(0f, 0.96f), new Vector2(1f, 1f),
                    new Color(accent.r, accent.g, accent.b, 0.34f), true);
                return;
            }
            current = current.parent;
        }
    }

    private static void AddFrameAround(RectTransform rect, Color accent, string id)
    {
        if (rect == null) return;
        string name = Prefix + id;
        Transform existing = rect.Find(name);
        if (existing != null) return;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        Undo.RegisterCreatedObjectUndo(go, "Add UI detail frame");
        go.transform.SetParent(rect, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-4f, -4f);
        rt.offsetMax = new Vector2(4f, 4f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(accent.r, accent.g, accent.b, 0.035f);
        img.raycastTarget = false;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(accent.r, accent.g, accent.b, 0.68f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
    }

    private static void AddEdge(RectTransform parent, string id, Vector2 anchorMin, Vector2 anchorMax, Color color, bool pulse)
    {
        string name = Prefix + id;
        Transform existing = parent.Find(name);
        if (existing != null) return;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Add UI edge detail");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        if (pulse)
        {
            UIDetailPulse fx = go.AddComponent<UIDetailPulse>();
            fx.target = img;
            fx.minAlpha = Mathf.Max(0.05f, color.a * 0.45f);
            fx.maxAlpha = Mathf.Min(1f, color.a);
            fx.speed = 0.65f;
        }
    }

    private static void AddCorner(RectTransform parent, string id, Vector2 anchor, Vector2 direction)
    {
        string name = Prefix + id;
        Transform existing = parent.Find(name);
        if (existing != null) return;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Add UI corner detail");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.sizeDelta = new Vector2(18f, 18f);
        rt.anchoredPosition = new Vector2(direction.x * -4f, direction.y * -4f);
        rt.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
        img.raycastTarget = false;
    }

    private static Color BlendKeepAlpha(Color original, Color target, float amount)
    {
        Color blended = Color.Lerp(original, target, Mathf.Clamp01(amount));
        blended.a = original.a;
        return blended;
    }

    private static T FindSceneObject<T>() where T : Object
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
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
#endif
