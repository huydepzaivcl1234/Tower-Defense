using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows owned relics in a compact bottom-left grid as [icon] x stack.
/// Up to five unique relics are shown. If more are owned, a + button opens
/// a scrollable panel containing every owned relic and stack count.
/// Reads the existing RelicManager/RelicData state only; it owns no gameplay data.
/// </summary>
public class RelicOwnedHUD : MonoBehaviour
{
    [Header("Layout")]
    [Min(1)] public int maxCompactRelics = 5;
    [Min(0.05f)] public float refreshInterval = 0.15f;

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

    private static readonly Color PanelColor = new Color(0.025f, 0.045f, 0.070f, 0.94f);
    private static readonly Color EntryColor = new Color(0.055f, 0.085f, 0.12f, 0.96f);
    private static readonly Color AccentColor = new Color(0.08f, 0.72f, 0.92f, 1f);
    private static readonly Color TextColor = new Color(0.90f, 0.96f, 1f, 1f);

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
        nextRefreshTime = Time.unscaledTime + refreshInterval;
        Refresh(false);
    }

    private void BuildIfNeeded()
    {
        if (built) return;
        built = true;

        RectTransform root = transform as RectTransform;
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(0f, 0f);
        root.pivot = new Vector2(0f, 0f);
        root.anchoredPosition = new Vector2(12f, 12f);
        root.sizeDelta = new Vector2(174f, 102f);

        compactBar = CreateRect("CompactBar", transform);
        compactBar.anchorMin = new Vector2(0f, 0f);
        compactBar.anchorMax = new Vector2(0f, 0f);
        compactBar.pivot = new Vector2(0f, 0f);
        compactBar.anchoredPosition = Vector2.zero;
        compactBar.sizeDelta = new Vector2(174f, 102f);

        Image compactBg = compactBar.gameObject.AddComponent<Image>();
        compactBg.color = PanelColor;
        compactBg.raycastTarget = false;

        GridLayoutGroup grid = compactBar.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(7, 7, 7, 7);
        grid.spacing = new Vector2(5f, 5f);
        grid.cellSize = new Vector2(50f, 41.5f);
        grid.startCorner = GridLayoutGroup.Corner.LowerLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.LowerLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        GameObject plus = CreateButton("ExpandButton", compactBar, "+", new Vector2(50f, 41.5f));
        expandButton = plus.GetComponent<Button>();
        expandButton.onClick.AddListener(ToggleAllRelicsPanel);
        plus.SetActive(false);

        BuildAllRelicsPanel();
    }

    private void BuildAllRelicsPanel()
    {
        allRelicsPanel = CreateRect("AllRelicsPanel", transform).gameObject;
        RectTransform panelRect = allRelicsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 112f);
        panelRect.sizeDelta = new Vector2(350f, 360f);

        Image panelBg = allRelicsPanel.AddComponent<Image>();
        panelBg.color = PanelColor;

        RectTransform title = CreateRect("Title", panelRect);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.offsetMin = new Vector2(14f, -48f);
        title.offsetMax = new Vector2(-54f, -7f);
        TextMeshProUGUI titleText = title.gameObject.AddComponent<TextMeshProUGUI>();
        titleText.text = "OWNED RELICS";
        titleText.fontSize = 21f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = TextColor;
        titleText.raycastTarget = false;

        GameObject close = CreateButton("CloseButton", panelRect, "×", new Vector2(38f, 38f));
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-7f, -7f);
        close.GetComponent<Button>().onClick.AddListener(() => allRelicsPanel.SetActive(false));

        RectTransform viewport = CreateRect("Viewport", panelRect);
        viewport.anchorMin = new Vector2(0f, 0f);
        viewport.anchorMax = new Vector2(1f, 1f);
        viewport.offsetMin = new Vector2(10f, 10f);
        viewport.offsetMax = new Vector2(-10f, -54f);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.12f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        allRelicsContent = CreateRect("Content", viewport);
        allRelicsContent.anchorMin = new Vector2(0f, 1f);
        allRelicsContent.anchorMax = new Vector2(1f, 1f);
        allRelicsContent.pivot = new Vector2(0.5f, 1f);
        allRelicsContent.anchoredPosition = Vector2.zero;
        allRelicsContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vertical = allRelicsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(7, 7, 7, 7);
        vertical.spacing = 6f;
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
        scroll.scrollSensitivity = 24f;

        allRelicsPanel.SetActive(false);
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
        {
            GameObject entry = CreateCompactEntry(owned[i]);
            compactEntries.Add(entry);
        }

        bool hasOverflow = owned.Count > maxCompactRelics;
        expandButton.gameObject.SetActive(hasOverflow);
        expandButton.transform.SetAsLastSibling();

        if (!hasOverflow && allRelicsPanel.activeSelf)
            allRelicsPanel.SetActive(false);
    }

    private GameObject CreateCompactEntry(OwnedRelic owned)
    {
        RectTransform entry = CreateRect("Relic_" + SafeName(owned.data.relicName), compactBar);
        entry.sizeDelta = new Vector2(50f, 41.5f);

        Image bg = entry.gameObject.AddComponent<Image>();
        bg.color = EntryColor;
        bg.raycastTarget = false;

        RectTransform rarityLine = CreateRect("Rarity", entry);
        rarityLine.anchorMin = new Vector2(0f, 0f);
        rarityLine.anchorMax = new Vector2(1f, 0f);
        rarityLine.pivot = new Vector2(0.5f, 0f);
        rarityLine.anchoredPosition = Vector2.zero;
        rarityLine.sizeDelta = new Vector2(0f, 2f);
        Image rarityImage = rarityLine.gameObject.AddComponent<Image>();
        rarityImage.color = RelicManager.GetRarityColor(owned.data.rarity);
        rarityImage.raycastTarget = false;

        RectTransform iconRect = CreateRect("Icon", entry);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-5f, 0f);
        iconRect.sizeDelta = new Vector2(32f, 32f);
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = owned.data.icon;
        icon.preserveAspect = true;
        icon.color = owned.data.icon != null ? Color.white : RelicManager.GetRarityColor(owned.data.rarity);
        icon.raycastTarget = false;

        RectTransform badge = CreateRect("StackBadge", entry);
        badge.anchorMin = new Vector2(1f, 0f);
        badge.anchorMax = new Vector2(1f, 0f);
        badge.pivot = new Vector2(1f, 0f);
        badge.anchoredPosition = new Vector2(-2f, 3f);
        badge.sizeDelta = new Vector2(25f, 18f);
        Image badgeBg = badge.gameObject.AddComponent<Image>();
        badgeBg.color = new Color(0.015f, 0.025f, 0.04f, 0.94f);
        badgeBg.raycastTarget = false;

        RectTransform stackRect = CreateRect("Stack", badge);
        stackRect.anchorMin = Vector2.zero;
        stackRect.anchorMax = Vector2.one;
        stackRect.offsetMin = Vector2.zero;
        stackRect.offsetMax = Vector2.zero;
        TextMeshProUGUI stack = stackRect.gameObject.AddComponent<TextMeshProUGUI>();
        stack.text = "x" + owned.stacks;
        stack.fontSize = 11f;
        stack.fontStyle = FontStyles.Bold;
        stack.alignment = TextAlignmentOptions.Center;
        stack.color = TextColor;
        stack.raycastTarget = false;

        return entry.gameObject;
    }

    private void RebuildFullPanel(List<OwnedRelic> owned)
    {
        ClearEntries(fullEntries);

        foreach (OwnedRelic relic in owned)
        {
            RectTransform row = CreateRect("Relic_" + SafeName(relic.data.relicName), allRelicsContent);
            row.sizeDelta = new Vector2(0f, 58f);
            LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 58f;

            Image bg = row.gameObject.AddComponent<Image>();
            bg.color = EntryColor;

            RectTransform iconRect = CreateRect("Icon", row);
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(7f, 0f);
            iconRect.sizeDelta = new Vector2(44f, 44f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = relic.data.icon;
            icon.preserveAspect = true;
            icon.color = relic.data.icon != null ? Color.white : RelicManager.GetRarityColor(relic.data.rarity);
            icon.raycastTarget = false;

            RectTransform nameRect = CreateRect("Name", row);
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(60f, 3f);
            nameRect.offsetMax = new Vector2(-75f, -3f);
            TextMeshProUGUI name = nameRect.gameObject.AddComponent<TextMeshProUGUI>();
            name.text = relic.data.relicName;
            name.fontSize = 16f;
            name.fontStyle = FontStyles.Bold;
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.color = RelicManager.GetRarityColor(relic.data.rarity);
            name.enableAutoSizing = true;
            name.fontSizeMin = 11f;
            name.fontSizeMax = 16f;
            name.raycastTarget = false;

            RectTransform stackRect = CreateRect("Stack", row);
            stackRect.anchorMin = new Vector2(1f, 0f);
            stackRect.anchorMax = new Vector2(1f, 1f);
            stackRect.pivot = new Vector2(1f, 0.5f);
            stackRect.anchoredPosition = new Vector2(-8f, 0f);
            stackRect.sizeDelta = new Vector2(62f, 0f);
            TextMeshProUGUI stack = stackRect.gameObject.AddComponent<TextMeshProUGUI>();
            stack.text = "x" + relic.stacks;
            stack.fontSize = 19f;
            stack.fontStyle = FontStyles.Bold;
            stack.alignment = TextAlignmentOptions.Center;
            stack.color = TextColor;
            stack.raycastTarget = false;

            fullEntries.Add(row.gameObject);
        }
    }

    private void ToggleAllRelicsPanel()
    {
        if (allRelicsPanel == null) return;
        allRelicsPanel.SetActive(!allRelicsPanel.activeSelf);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static GameObject CreateButton(string name, Transform parent, string text, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.sizeDelta = size;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = EntryColor;

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
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = AccentColor;
        label.raycastTarget = false;

        return rect.gameObject;
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
