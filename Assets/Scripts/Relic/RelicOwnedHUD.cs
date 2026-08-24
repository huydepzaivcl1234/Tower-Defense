using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays owned relics as a compact HUD plus an expandable full inventory panel.
/// All visual/layout tuning is exposed in the Inspector; this class owns no gameplay data.
/// </summary>
public class RelicOwnedHUD : MonoBehaviour
{
    [Header("Behaviour")]
    [Min(1)] public int maxCompactRelics = 5;
    [Min(0.05f)] public float refreshInterval = 0.15f;

    [Header("HUD Position / Size")]
    public Vector2 hudAnchor = new Vector2(0f, 0f);
    public Vector2 hudPivot = new Vector2(0f, 0f);
    public Vector2 hudPosition = new Vector2(12f, 12f);
    public Vector2 hudSize = new Vector2(220f, 132f);

    [Header("Compact Grid")]
    [Min(1)] public int compactColumns = 3;
    public Vector2 compactSize = new Vector2(220f, 132f);
    public Vector2 compactCellSize = new Vector2(64f, 55f);
    public Vector2 compactSpacing = new Vector2(6f, 6f);
    [Min(0)] public int compactPaddingLeft = 7;
    [Min(0)] public int compactPaddingRight = 7;
    [Min(0)] public int compactPaddingTop = 7;
    [Min(0)] public int compactPaddingBottom = 7;
    public GridLayoutGroup.Corner compactStartCorner = GridLayoutGroup.Corner.LowerLeft;
    public TextAnchor compactChildAlignment = TextAnchor.LowerLeft;

    [Header("Compact Relic Entry")]
    public Vector2 compactIconSize = new Vector2(46f, 46f);
    public Vector2 compactIconOffset = new Vector2(-6f, 0f);
    [Min(0f)] public float rarityLineHeight = 3f;

    [Header("Compact Stack Badge")]
    public Vector2 stackBadgeSize = new Vector2(34f, 23f);
    public Vector2 stackBadgeOffset = new Vector2(-2f, 3f);
    [Min(1f)] public float compactStackFontSize = 15f;
    public Color stackBadgeColor = new Color(0.015f, 0.025f, 0.04f, 0.94f);

    [Header("Expand Button")]
    public Vector2 expandButtonSize = new Vector2(64f, 55f);
    [Min(1f)] public float expandButtonFontSize = 28f;

    [Header("Full Relic Panel")]
    public Vector2 fullPanelPosition = new Vector2(0f, 142f);
    public Vector2 fullPanelSize = new Vector2(390f, 420f);
    public string fullPanelTitle = "OWNED RELICS";
    [Min(1f)] public float fullPanelTitleFontSize = 23f;
    public Vector2 closeButtonSize = new Vector2(42f, 42f);
    public Vector2 closeButtonOffset = new Vector2(-7f, -7f);
    [Min(1f)] public float fullPanelRowHeight = 68f;
    public Vector2 fullPanelIconSize = new Vector2(52f, 52f);
    [Min(1f)] public float fullPanelNameFontMin = 12f;
    [Min(1f)] public float fullPanelNameFontMax = 19f;
    [Min(1f)] public float fullPanelStackFontSize = 22f;
    [Min(1f)] public float scrollSensitivity = 24f;
    [Min(0f)] public float fullPanelRowSpacing = 7f;

    [Header("Colors")]
    public Color panelColor = new Color(0.025f, 0.045f, 0.070f, 0.94f);
    public Color entryColor = new Color(0.055f, 0.085f, 0.12f, 0.96f);
    public Color accentColor = new Color(0.08f, 0.72f, 0.92f, 1f);
    public Color textColor = new Color(0.90f, 0.96f, 1f, 1f);
    public Color viewportColor = new Color(0f, 0f, 0f, 0.12f);

    [Header("Runtime References - created automatically")]
    [SerializeField] private RectTransform compactBar;
    [SerializeField] private Button expandButton;
    [SerializeField] private GameObject allRelicsPanel;
    [SerializeField] private RectTransform allRelicsContent;

    private readonly List<GameObject> compactEntries = new List<GameObject>();
    private readonly List<GameObject> fullEntries = new List<GameObject>();
    private float nextRefreshTime;
    private int lastSignature = int.MinValue;
    private bool built;

    private struct OwnedRelic
    {
        public RelicData data;
        public int stacks;
    }

    private void Awake()
    {
        BuildIfNeeded();
    }

    private void Start()
    {
        Refresh(true);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
        Refresh(false);
    }

    private void OnValidate()
    {
        maxCompactRelics = Mathf.Max(1, maxCompactRelics);
        compactColumns = Mathf.Max(1, compactColumns);
        compactCellSize.x = Mathf.Max(1f, compactCellSize.x);
        compactCellSize.y = Mathf.Max(1f, compactCellSize.y);
        compactIconSize.x = Mathf.Max(1f, compactIconSize.x);
        compactIconSize.y = Mathf.Max(1f, compactIconSize.y);
        stackBadgeSize.x = Mathf.Max(1f, stackBadgeSize.x);
        stackBadgeSize.y = Mathf.Max(1f, stackBadgeSize.y);
        fullPanelRowHeight = Mathf.Max(1f, fullPanelRowHeight);

        if (Application.isPlaying && built)
        {
            ApplyRootLayout();
            ApplyCompactLayout();
            ApplyPanelLayout();
            lastSignature = int.MinValue;
            Refresh(true);
        }
    }

    private void BuildIfNeeded()
    {
        if (built) return;
        built = true;

        ApplyRootLayout();

        compactBar = CreateRect("CompactBar", transform);
        Image compactBg = compactBar.gameObject.AddComponent<Image>();
        compactBg.raycastTarget = false;

        GridLayoutGroup grid = compactBar.gameObject.AddComponent<GridLayoutGroup>();
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        GameObject plus = CreateButton("ExpandButton", compactBar, "+", expandButtonSize);
        expandButton = plus.GetComponent<Button>();
        expandButton.onClick.AddListener(ToggleAllRelicsPanel);
        plus.SetActive(false);

        BuildAllRelicsPanel();
        ApplyCompactLayout();
        ApplyPanelLayout();
    }

    private void ApplyRootLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = hudAnchor;
        root.anchorMax = hudAnchor;
        root.pivot = hudPivot;
        root.anchoredPosition = hudPosition;
        root.sizeDelta = hudSize;
    }

    private void ApplyCompactLayout()
    {
        if (compactBar == null) return;

        compactBar.anchorMin = Vector2.zero;
        compactBar.anchorMax = Vector2.zero;
        compactBar.pivot = Vector2.zero;
        compactBar.anchoredPosition = Vector2.zero;
        compactBar.sizeDelta = compactSize;

        Image bg = compactBar.GetComponent<Image>();
        if (bg != null) bg.color = panelColor;

        GridLayoutGroup grid = compactBar.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.padding = new RectOffset(
                compactPaddingLeft,
                compactPaddingRight,
                compactPaddingTop,
                compactPaddingBottom);
            grid.spacing = compactSpacing;
            grid.cellSize = compactCellSize;
            grid.startCorner = compactStartCorner;
            grid.childAlignment = compactChildAlignment;
            grid.constraintCount = compactColumns;
        }

        if (expandButton != null)
        {
            RectTransform rect = expandButton.transform as RectTransform;
            if (rect != null) rect.sizeDelta = expandButtonSize;
            Image image = expandButton.GetComponent<Image>();
            if (image != null) image.color = entryColor;
            TMP_Text label = expandButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = expandButtonFontSize;
                label.color = accentColor;
            }
        }
    }

    private void BuildAllRelicsPanel()
    {
        allRelicsPanel = CreateRect("AllRelicsPanel", transform).gameObject;
        RectTransform panelRect = allRelicsPanel.GetComponent<RectTransform>();
        Image panelBg = allRelicsPanel.AddComponent<Image>();

        RectTransform title = CreateRect("Title", panelRect);
        TextMeshProUGUI titleText = title.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.raycastTarget = false;

        GameObject close = CreateButton("CloseButton", panelRect, "×", closeButtonSize);
        close.GetComponent<Button>().onClick.AddListener(() => allRelicsPanel.SetActive(false));

        RectTransform viewport = CreateRect("Viewport", panelRect);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        allRelicsContent = CreateRect("Content", viewport);
        VerticalLayoutGroup vertical = allRelicsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = true;
        vertical.childControlHeight = false;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        ContentSizeFitter fitter = allRelicsContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scroll = allRelicsPanel.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = allRelicsContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        allRelicsPanel.SetActive(false);
    }

    private void ApplyPanelLayout()
    {
        if (allRelicsPanel == null) return;

        RectTransform panelRect = allRelicsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = fullPanelPosition;
        panelRect.sizeDelta = fullPanelSize;

        Image panelBg = allRelicsPanel.GetComponent<Image>();
        if (panelBg != null) panelBg.color = panelColor;

        Transform titleTransform = panelRect.Find("Title");
        if (titleTransform != null)
        {
            RectTransform title = titleTransform as RectTransform;
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.offsetMin = new Vector2(14f, -52f);
            title.offsetMax = new Vector2(-58f, -7f);
            TMP_Text titleText = title.GetComponent<TMP_Text>();
            titleText.text = fullPanelTitle;
            titleText.fontSize = fullPanelTitleFontSize;
            titleText.color = textColor;
        }

        Transform closeTransform = panelRect.Find("CloseButton");
        if (closeTransform != null)
        {
            RectTransform close = closeTransform as RectTransform;
            close.anchorMin = Vector2.one;
            close.anchorMax = Vector2.one;
            close.pivot = Vector2.one;
            close.anchoredPosition = closeButtonOffset;
            close.sizeDelta = closeButtonSize;
        }

        Transform viewportTransform = panelRect.Find("Viewport");
        if (viewportTransform != null)
        {
            RectTransform viewport = viewportTransform as RectTransform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(10f, 10f);
            viewport.offsetMax = new Vector2(-10f, -58f);
            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null) viewportImage.color = viewportColor;
        }

        if (allRelicsContent != null)
        {
            allRelicsContent.anchorMin = new Vector2(0f, 1f);
            allRelicsContent.anchorMax = new Vector2(1f, 1f);
            allRelicsContent.pivot = new Vector2(0.5f, 1f);
            allRelicsContent.anchoredPosition = Vector2.zero;
            allRelicsContent.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vertical = allRelicsContent.GetComponent<VerticalLayoutGroup>();
            if (vertical != null)
            {
                vertical.padding = new RectOffset(7, 7, 7, 7);
                vertical.spacing = fullPanelRowSpacing;
            }
        }

        ScrollRect scroll = allRelicsPanel.GetComponent<ScrollRect>();
        if (scroll != null) scroll.scrollSensitivity = scrollSensitivity;
    }

    private void Refresh(bool force)
    {
        RelicManager manager = RelicManager.Instance;
        if (manager == null)
        {
            if (compactBar != null) compactBar.gameObject.SetActive(false);
            if (allRelicsPanel != null) allRelicsPanel.SetActive(false);
            return;
        }

        List<OwnedRelic> owned = CollectOwnedRelics(manager);
        int signature = ComputeSignature(owned);
        if (!force && signature == lastSignature) return;
        lastSignature = signature;

        compactBar.gameObject.SetActive(owned.Count > 0);
        RebuildCompact(owned);
        RebuildFullPanel(owned);
    }

    private List<OwnedRelic> CollectOwnedRelics(RelicManager manager)
    {
        var owned = new List<OwnedRelic>();
        if (manager.relicPool == null) return owned;

        var seen = new HashSet<RelicData>();
        foreach (RelicData relic in manager.relicPool)
        {
            if (relic == null || !seen.Add(relic)) continue;
            int count = manager.GetStacks(relic);
            if (count <= 0) continue;
            owned.Add(new OwnedRelic { data = relic, stacks = count });
        }
        return owned;
    }

    private static int ComputeSignature(List<OwnedRelic> owned)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < owned.Count; i++)
            {
                hash = hash * 31 + (owned[i].data != null ? owned[i].data.GetEntityId().GetHashCode() : 0);
                hash = hash * 31 + owned[i].stacks;
            }
            return hash;
        }
    }

    private void RebuildCompact(List<OwnedRelic> owned)
    {
        ClearEntries(compactEntries);

        int visible = Mathf.Min(Mathf.Max(1, maxCompactRelics), owned.Count);
        for (int i = 0; i < visible; i++)
            compactEntries.Add(CreateCompactEntry(owned[i]));

        bool hasOverflow = owned.Count > maxCompactRelics;
        expandButton.gameObject.SetActive(hasOverflow);
        expandButton.transform.SetAsLastSibling();

        if (!hasOverflow && allRelicsPanel.activeSelf)
            allRelicsPanel.SetActive(false);
    }

    private GameObject CreateCompactEntry(OwnedRelic owned)
    {
        RectTransform entry = CreateRect("Relic_" + SafeName(owned.data.relicName), compactBar);
        entry.sizeDelta = compactCellSize;

        Image bg = entry.gameObject.AddComponent<Image>();
        bg.color = entryColor;
        bg.raycastTarget = false;

        RectTransform rarityLine = CreateRect("Rarity", entry);
        rarityLine.anchorMin = new Vector2(0f, 0f);
        rarityLine.anchorMax = new Vector2(1f, 0f);
        rarityLine.pivot = new Vector2(0.5f, 0f);
        rarityLine.anchoredPosition = Vector2.zero;
        rarityLine.sizeDelta = new Vector2(0f, rarityLineHeight);
        Image rarityImage = rarityLine.gameObject.AddComponent<Image>();
        rarityImage.color = RelicManager.GetRarityColor(owned.data.rarity);
        rarityImage.raycastTarget = false;

        RectTransform iconRect = CreateRect("Icon", entry);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = compactIconOffset;
        iconRect.sizeDelta = compactIconSize;
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = owned.data.icon;
        icon.preserveAspect = true;
        icon.color = owned.data.icon != null ? Color.white : RelicManager.GetRarityColor(owned.data.rarity);
        icon.raycastTarget = false;

        RectTransform badge = CreateRect("StackBadge", entry);
        badge.anchorMin = new Vector2(1f, 0f);
        badge.anchorMax = new Vector2(1f, 0f);
        badge.pivot = new Vector2(1f, 0f);
        badge.anchoredPosition = stackBadgeOffset;
        badge.sizeDelta = stackBadgeSize;
        Image badgeBg = badge.gameObject.AddComponent<Image>();
        badgeBg.color = stackBadgeColor;
        badgeBg.raycastTarget = false;

        RectTransform stackRect = CreateRect("Stack", badge);
        stackRect.anchorMin = Vector2.zero;
        stackRect.anchorMax = Vector2.one;
        stackRect.offsetMin = Vector2.zero;
        stackRect.offsetMax = Vector2.zero;
        TextMeshProUGUI stack = stackRect.gameObject.AddComponent<TextMeshProUGUI>();
        stack.text = "x" + owned.stacks;
        stack.fontSize = compactStackFontSize;
        stack.fontStyle = FontStyles.Bold;
        stack.alignment = TextAlignmentOptions.Center;
        stack.color = textColor;
        stack.raycastTarget = false;

        return entry.gameObject;
    }

    private void RebuildFullPanel(List<OwnedRelic> owned)
    {
        ClearEntries(fullEntries);

        foreach (OwnedRelic relic in owned)
        {
            RectTransform row = CreateRect("Relic_" + SafeName(relic.data.relicName), allRelicsContent);
            row.sizeDelta = new Vector2(0f, fullPanelRowHeight);
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = fullPanelRowHeight;

            Image bg = row.gameObject.AddComponent<Image>();
            bg.color = entryColor;

            RectTransform iconRect = CreateRect("Icon", row);
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
            iconRect.sizeDelta = fullPanelIconSize;
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = relic.data.icon;
            icon.preserveAspect = true;
            icon.color = relic.data.icon != null ? Color.white : RelicManager.GetRarityColor(relic.data.rarity);
            icon.raycastTarget = false;

            float leftText = 16f + fullPanelIconSize.x;
            RectTransform nameRect = CreateRect("Name", row);
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(leftText, 4f);
            nameRect.offsetMax = new Vector2(-92f, -4f);
            TextMeshProUGUI name = nameRect.gameObject.AddComponent<TextMeshProUGUI>();
            name.text = relic.data.relicName;
            name.fontSize = fullPanelNameFontMax;
            name.fontStyle = FontStyles.Bold;
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.color = RelicManager.GetRarityColor(relic.data.rarity);
            name.enableAutoSizing = true;
            name.fontSizeMin = fullPanelNameFontMin;
            name.fontSizeMax = fullPanelNameFontMax;
            name.raycastTarget = false;

            RectTransform stackRect = CreateRect("Stack", row);
            stackRect.anchorMin = new Vector2(1f, 0f);
            stackRect.anchorMax = new Vector2(1f, 1f);
            stackRect.pivot = new Vector2(1f, 0.5f);
            stackRect.anchoredPosition = new Vector2(-8f, 0f);
            stackRect.sizeDelta = new Vector2(78f, 0f);
            TextMeshProUGUI stack = stackRect.gameObject.AddComponent<TextMeshProUGUI>();
            stack.text = "x" + relic.stacks;
            stack.fontSize = fullPanelStackFontSize;
            stack.fontStyle = FontStyles.Bold;
            stack.alignment = TextAlignmentOptions.Center;
            stack.color = textColor;
            stack.raycastTarget = false;

            fullEntries.Add(row.gameObject);
        }
    }

    private void ToggleAllRelicsPanel()
    {
        if (allRelicsPanel == null) return;
        allRelicsPanel.SetActive(!allRelicsPanel.activeSelf);
    }

    private GameObject CreateButton(string name, Transform parent, string text, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.sizeDelta = size;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = entryColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.75f, 0.90f, 1f, 1f);
        button.colors = colors;

        if (rect.gameObject.GetComponent<UIPunchButton>() == null)
            rect.gameObject.AddComponent<UIPunchButton>();

        RectTransform labelRect = CreateRect("Label", rect);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = expandButtonFontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = accentColor;
        label.raycastTarget = false;

        return rect.gameObject;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void ClearEntries(List<GameObject> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null) Destroy(entries[i]);
        }
        entries.Clear();
    }

    private static string SafeName(string value)
    {
        if (string.IsNullOrEmpty(value)) return "Relic";
        return value.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
    }
}
