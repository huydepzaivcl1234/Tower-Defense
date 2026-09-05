#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuShopSetupTool
{
    private static readonly Color Overlay = new Color(0.01f, 0.035f, 0.055f, 0.985f);
    private static readonly Color Card = new Color(0.025f, 0.095f, 0.14f, 0.98f);
    private static readonly Color CardLight = new Color(0.04f, 0.15f, 0.21f, 1f);
    private static readonly Color Cyan = new Color(0.05f, 0.78f, 0.96f, 1f);
    private static readonly Color Text = new Color(0.92f, 0.97f, 1f, 1f);
    private static readonly Color Muted = new Color(0.58f, 0.72f, 0.80f, 1f);

    [MenuItem("Tower Defense/UI/Setup Main Menu Shop")]
    public static void Setup()
    {
        MainMenuController menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (menu == null || menu.mainPanel == null)
        {
            EditorUtility.DisplayDialog("Main Menu Shop", "MainMenuController with Main Panel is required.", "OK");
            return;
        }

        Button shopButton = menu.shopButton;
        if (shopButton == null)
        {
            Transform existing = FindDeepChild(menu.mainPanel.transform, "ShopButton");
            shopButton = existing != null ? existing.GetComponent<Button>() : null;
        }
        if (shopButton == null)
            shopButton = CreateButton("ShopButton", menu.mainPanel.transform, "SHOP", false);

        ArrangeMainButtons(menu, shopButton);

        GameObject shopPanelObject = menu.shopPanel;
        if (shopPanelObject == null)
        {
            Transform existing = FindDeepChild(menu.transform, "ShopPanel");
            if (existing != null)
                shopPanelObject = existing.gameObject;
        }

        MainMenuShopPanel shop;
        if (shopPanelObject == null)
        {
            RectTransform shopPanel = CreateRect("ShopPanel", menu.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            shopPanelObject = shopPanel.gameObject;
            Image overlay = shopPanelObject.AddComponent<Image>();
            overlay.color = Overlay;
            overlay.raycastTarget = true;
            shop = shopPanelObject.AddComponent<MainMenuShopPanel>();
            BuildShopUI(shopPanel, shop);
        }
        else
        {
            shop = shopPanelObject.GetComponent<MainMenuShopPanel>();
            if (shop == null)
                shop = Undo.AddComponent<MainMenuShopPanel>(shopPanelObject);
        }

        menu.shopButton = shopButton;
        menu.shopPanel = shopPanelObject;
        shopPanelObject.SetActive(false);

        EditorUtility.SetDirty(menu);
        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        Selection.activeGameObject = shopPanelObject;

        EditorUtility.DisplayDialog(
            "Main Menu Shop Ready",
            "SHOP was added and the main buttons were rearranged.\n\n" +
            "Top: permanent Diamond Drop Chance upgrade (+1% per stack, max 10).\n" +
            "Bottom: configurable Diamond purchase packs. Add a list element and reopen Shop to generate another card.\n\n" +
            "Editor purchase simulation is enabled for testing. A real store bridge must call CompletePurchase after payment succeeds.",
            "OK");
    }

    private static void ArrangeMainButtons(MainMenuController menu, Button shopButton)
    {
        SetButtonRect(menu.playButton, new Vector2(0f, 80f));
        SetButtonRect(shopButton, new Vector2(0f, 0f));
        SetButtonRect(menu.profileButton, new Vector2(0f, -80f));
        SetButtonRect(menu.settingsButton, new Vector2(0f, -160f));
        SetButtonRect(menu.exitButton, new Vector2(0f, -240f));
    }

    private static void SetButtonRect(Button button, Vector2 position)
    {
        if (button == null)
            return;

        Undo.RecordObject(button.transform, "Arrange Main Menu Button");
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(380f, 64f);
        rect.localScale = Vector3.one;
    }

    private static void BuildShopUI(RectTransform root, MainMenuShopPanel shop)
    {
        TMP_Text title = CreateText("Title", root, "SHOP", 48f, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(500f, 68f));

        TMP_Text balance = CreateText("DiamondBalance", root, "DIAMONDS  0", 22f, FontStyles.Bold, TextAlignmentOptions.Right, Cyan);
        SetRect(balance.rectTransform, new Vector2(1f, 1f), new Vector2(-52f, -58f), new Vector2(300f, 54f));
        balance.rectTransform.pivot = new Vector2(1f, 0.5f);

        Button back = CreateButton("BackButton", root, "BACK", false);
        SetRect(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(70f, -58f), new Vector2(150f, 54f));
        back.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);

        RectTransform contentCard = CreateRect("ShopContent", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-560f, -430f), new Vector2(560f, 410f));
        AddPanelStyle(contentCard.gameObject, Card);

        TMP_Text upgradeHeader = CreateText("UpgradeHeader", contentCard, "PERMANENT UPGRADES", 23f, FontStyles.Bold, TextAlignmentOptions.Left, Cyan);
        SetRect(upgradeHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(-500f, -34f), new Vector2(960f, 42f));
        upgradeHeader.rectTransform.pivot = new Vector2(0f, 0.5f);

        RectTransform upgradeCard = CreateRect("DiamondDropUpgrade", contentCard, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(38f, -270f), new Vector2(-38f, -82f));
        AddPanelStyle(upgradeCard.gameObject, CardLight);

        TMP_Text upgradeName = CreateText("UpgradeLevel", upgradeCard, "DIAMOND DROP CHANCE  0/10", 25f, FontStyles.Bold, TextAlignmentOptions.Left, Text);
        SetRect(upgradeName.rectTransform, new Vector2(0f, 1f), new Vector2(34f, -42f), new Vector2(650f, 42f));
        upgradeName.rectTransform.pivot = new Vector2(0f, 0.5f);

        TMP_Text description = CreateText("UpgradeDescription", upgradeCard, "Permanent +1% drop chance per stack. Current bonus: +0%", 18f, FontStyles.Normal, TextAlignmentOptions.Left, Muted);
        SetRect(description.rectTransform, new Vector2(0f, 1f), new Vector2(34f, -91f), new Vector2(690f, 58f));
        description.rectTransform.pivot = new Vector2(0f, 0.5f);
        description.textWrappingMode = TextWrappingModes.Normal;

        TMP_Text stackHint = CreateText("StackHint", upgradeCard, "10 STACKS MAX", 16f, FontStyles.Bold, TextAlignmentOptions.Left, Cyan);
        SetRect(stackHint.rectTransform, new Vector2(0f, 0f), new Vector2(34f, 30f), new Vector2(360f, 32f));
        stackHint.rectTransform.pivot = new Vector2(0f, 0.5f);

        Button upgradeButton = CreateButton("UpgradeButton", upgradeCard, "UPGRADE", true);
        SetRect(upgradeButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-148f, 0f), new Vector2(250f, 70f));
        TMP_Text upgradeButtonLabel = upgradeButton.transform.Find("Label").GetComponent<TMP_Text>();
        SetRect(upgradeButtonLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(190f, 28f));
        TMP_Text cost = CreateText("UpgradeCost", upgradeButton.transform, "100", 18f, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        SetRect(cost.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -24f), new Vector2(180f, 26f));

        TMP_Text packHeader = CreateText("PackHeader", contentCard, "DIAMOND PACKS", 23f, FontStyles.Bold, TextAlignmentOptions.Left, Cyan);
        SetRect(packHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(-500f, -308f), new Vector2(960f, 42f));
        packHeader.rectTransform.pivot = new Vector2(0f, 0.5f);

        RectTransform viewport = CreateRect("PackViewport", contentCard, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(38f, 72f), new Vector2(-38f, -346f));
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.gameObject.AddComponent<RectMask2D>();

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;

        RectTransform packContent = CreateRect("PackContent", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        packContent.pivot = new Vector2(0.5f, 1f);
        VerticalLayoutGroup layout = packContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = packContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = packContent;

        RectTransform template = CreatePackTemplate(packContent);
        template.gameObject.SetActive(false);

        TMP_Text status = CreateText("Status", contentCard, "", 17f, FontStyles.Bold, TextAlignmentOptions.Center, Muted);
        SetRect(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(900f, 34f));

        shop.backButton = back;
        shop.diamondBalanceText = balance;
        shop.upgradeLevelText = upgradeName;
        shop.upgradeDescriptionText = description;
        shop.upgradeCostText = cost;
        shop.upgradeButton = upgradeButton;
        shop.statusText = status;
        shop.purchasePackContent = packContent;
        shop.purchasePackTemplate = template;
    }

    private static RectTransform CreatePackTemplate(Transform parent)
    {
        RectTransform card = CreateRect("PurchasePackTemplate", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 116f;
        element.minHeight = 116f;
        AddPanelStyle(card.gameObject, CardLight);

        Image icon = CreateImage("Icon", card);
        SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(66f, 0f), new Vector2(76f, 76f));
        icon.enabled = false;

        TMP_Text name = CreateText("PackName", card, "STARTER PACK", 22f, FontStyles.Bold, TextAlignmentOptions.Left, Text);
        SetRect(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(126f, 22f), new Vector2(430f, 36f));
        name.rectTransform.pivot = new Vector2(0f, 0.5f);

        TMP_Text amount = CreateText("DiamondAmount", card, "+100 DIAMONDS", 18f, FontStyles.Bold, TextAlignmentOptions.Left, Cyan);
        SetRect(amount.rectTransform, new Vector2(0f, 0.5f), new Vector2(126f, -22f), new Vector2(430f, 32f));
        amount.rectTransform.pivot = new Vector2(0f, 0.5f);

        Button buy = CreateButton("BuyButton", card, "BUY", true);
        SetRect(buy.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-125f, 0f), new Vector2(210f, 66f));
        TMP_Text buyLabel = buy.transform.Find("Label").GetComponent<TMP_Text>();
        SetRect(buyLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 11f), new Vector2(160f, 26f));

        TMP_Text price = CreateText("Price", buy.transform, "$0.99", 16f, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        SetRect(price.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -22f), new Vector2(160f, 24f));
        return card;
    }

    private static Button CreateButton(string name, Transform parent, string label, bool primary)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, -32f), new Vector2(150f, 32f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = primary ? new Color(0.025f, 0.52f, 0.68f, 1f) : new Color(0.045f, 0.16f, 0.22f, 1f);
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, primary ? 0.9f : 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.05f, 0.64f, 0.80f, 1f);
        colors.pressedColor = new Color(0.035f, 0.40f, 0.52f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.08f, 0.11f, 0.13f, 0.75f);
        button.colors = colors;

        TMP_Text text = CreateText("Label", rect, label, 22f, FontStyles.Bold, TextAlignmentOptions.Center, Text);
        Stretch(text.rectTransform);
        AddPunchIfAvailable(rect.gameObject);
        return button;
    }

    private static void AddPanelStyle(GameObject target, Color color)
    {
        Image image = target.AddComponent<Image>();
        image.color = color;
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.28f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private static Image CreateImage(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-30f, -30f), new Vector2(30f, 30f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-100f, -20f), new Vector2(100f, 20f));
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

    private static void AddPunchIfAvailable(GameObject target)
    {
        Type punchType = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            punchType = assembly.GetType("UIPunchButton");
            if (punchType != null) break;
        }
        if (punchType != null && target.GetComponent(punchType) == null)
            Undo.AddComponent(target, punchType);
    }
}
#endif
