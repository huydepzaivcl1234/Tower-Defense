using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime quest cards shown in the screen corner. Each accepted quest gets one live card.
/// Cards update progress, turn green on completion, animate out, then notify QuestManager
/// so auto rewards are granted only after the completion presentation finishes.
/// </summary>
[DisallowMultipleComponent]
public class QuestLiveHUD : MonoBehaviour
{
    public static QuestLiveHUD Instance { get; private set; }

    [Header("Root Layout")]
    public Vector2 anchor = new Vector2(0f, 0f);
    public Vector2 pivot = new Vector2(0f, 0f);
    public Vector2 anchoredPosition = new Vector2(24f, 160f);
    public Vector2 rootSize = new Vector2(430f, 360f);
    public bool stackUpward = true;
    [Min(0f)] public float cardSpacing = 10f;

    [Header("Card Size")]
    public Vector2 cardSize = new Vector2(390f, 92f);
    public Vector4 cardPadding = new Vector4(16f, 14f, 12f, 10f);

    [Header("Typography")]
    public string headerText = "QUEST";
    [Min(8)] public int headerFontSize = 16;
    [Min(8)] public int titleFontSize = 20;
    [Min(8)] public int progressFontSize = 17;
    public TextAlignmentOptions headerAlignment = TextAlignmentOptions.TopLeft;
    public TextAlignmentOptions titleAlignment = TextAlignmentOptions.Left;
    public TextAlignmentOptions progressAlignment = TextAlignmentOptions.Right;

    [Header("Colors")]
    public Color cardColor = new Color(0.035f, 0.08f, 0.12f, 0.94f);
    public Color headerColor = new Color(0.10f, 0.82f, 1f, 1f);
    public Color titleColor = Color.white;
    public Color progressColor = new Color(0.75f, 0.88f, 0.96f, 1f);
    public Color completeCardColor = new Color(0.08f, 0.48f, 0.22f, 0.97f);
    public Color completeTextColor = new Color(0.78f, 1f, 0.82f, 1f);

    [Header("Progress Formatting")]
    public bool showObjectiveLabel = true;
    public string killLabel = "KILL";
    public string spendGoldLabel = "SPEND";
    public string upgradeLabel = "UPGRADE";
    public string completedLabel = "COMPLETE";

    [Header("Animation")]
    [Min(0.01f)] public float enterDuration = 0.35f;
    [Min(0f)] public float enterOffsetX = -80f;
    public Ease enterEase = Ease.OutCubic;
    [Min(0.01f)] public float progressPunchDuration = 0.18f;
    [Range(1f, 1.3f)] public float progressPunchScale = 1.08f;
    [Min(0.01f)] public float completeColorDuration = 0.28f;
    [Min(0.01f)] public float completePunchDuration = 0.30f;
    [Range(1f, 1.3f)] public float completePunchScale = 1.06f;
    [Min(0f)] public float completeHoldDuration = 0.85f;
    [Min(0.01f)] public float exitDuration = 0.35f;
    [Min(0f)] public float exitOffsetX = -90f;
    public Ease exitEase = Ease.InCubic;

    private readonly Dictionary<ActiveQuest, QuestCard> cards = new Dictionary<ActiveQuest, QuestCard>();
    private RectTransform rootRect;

    private sealed class QuestCard
    {
        public ActiveQuest quest;
        public GameObject root;
        public RectTransform rect;
        public CanvasGroup canvasGroup;
        public Image background;
        public TMP_Text header;
        public TMP_Text title;
        public TMP_Text progress;
        public Sequence sequence;
        public bool completing;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        rootRect = transform as RectTransform;
        ApplyRootLayout();
    }

    private void OnEnable()
    {
        QuestManager.OnQuestAccepted += HandleQuestAccepted;
        QuestManager.OnQuestProgressChanged += HandleQuestProgressChanged;
        QuestManager.OnQuestCompleted += HandleQuestCompleted;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestAccepted -= HandleQuestAccepted;
        QuestManager.OnQuestProgressChanged -= HandleQuestProgressChanged;
        QuestManager.OnQuestCompleted -= HandleQuestCompleted;
        KillAllTweens();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start() => RebuildExistingQuests();

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            rootRect = transform as RectTransform;
            ApplyRootLayout();
        }
    }
#endif

    private void ApplyRootLayout()
    {
        if (rootRect == null) return;
        rootRect.anchorMin = anchor;
        rootRect.anchorMax = anchor;
        rootRect.pivot = pivot;
        rootRect.anchoredPosition = anchoredPosition;
        rootRect.sizeDelta = rootSize;
    }

    private void RebuildExistingQuests()
    {
        QuestManager manager = QuestManager.Instance;
        if (manager == null) return;

        IReadOnlyList<ActiveQuest> active = manager.ActiveQuests;
        for (int i = 0; i < active.Count; i++)
        {
            ActiveQuest quest = active[i];
            if (quest == null || quest.data == null || cards.ContainsKey(quest)) continue;
            QuestCard card = CreateCard(quest, false);
            if (quest.completed) BeginCompletion(card);
        }
        ReflowCards(false);
    }

    private void HandleQuestAccepted(ActiveQuest quest)
    {
        if (quest == null || quest.data == null || cards.ContainsKey(quest)) return;
        CreateCard(quest, true);
        ReflowCards(true);
    }

    private void HandleQuestProgressChanged(ActiveQuest quest)
    {
        if (quest == null || quest.data == null) return;
        if (!cards.TryGetValue(quest, out QuestCard card))
            card = CreateCard(quest, false);

        RefreshCard(card);
        if (!card.completing && card.progress != null)
        {
            card.progress.transform.DOKill();
            card.progress.transform.localScale = Vector3.one;
            card.progress.transform.DOPunchScale(
                    Vector3.one * Mathf.Max(0f, progressPunchScale - 1f),
                    progressPunchDuration,
                    5,
                    0.45f)
                .SetUpdate(true);
        }
    }

    private void HandleQuestCompleted(ActiveQuest quest)
    {
        if (quest == null || quest.data == null) return;
        if (!cards.TryGetValue(quest, out QuestCard card))
            card = CreateCard(quest, false);
        BeginCompletion(card);
    }

    private QuestCard CreateCard(ActiveQuest quest, bool animateIn)
    {
        GameObject go = new GameObject($"QuestCard_{SafeName(quest.data.questTitle)}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = cardSize;
        rect.localScale = Vector3.one;

        Image bg = go.GetComponent<Image>();
        bg.color = cardColor;
        bg.raycastTarget = false;

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;

        TMP_Text header = CreateText(go.transform, "Header", headerText, headerFontSize, headerColor, headerAlignment);
        TMP_Text title = CreateText(go.transform, "QuestTitle", quest.data.questTitle, titleFontSize, titleColor, titleAlignment);
        TMP_Text progress = CreateText(go.transform, "Progress", string.Empty, progressFontSize, progressColor, progressAlignment);

        float left = cardPadding.x;
        float right = cardPadding.y;
        float top = cardPadding.z;
        float bottom = cardPadding.w;

        SetTextRect(header.rectTransform, left, right, cardSize.y - top - 22f, 22f);
        SetTextRect(title.rectTransform, left, right + 110f, bottom + 8f, 42f);
        SetTextRect(progress.rectTransform, cardSize.x - 150f, right, bottom + 8f, 42f);

        QuestCard card = new QuestCard
        {
            quest = quest,
            root = go,
            rect = rect,
            canvasGroup = cg,
            background = bg,
            header = header,
            title = title,
            progress = progress
        };

        cards.Add(quest, card);
        RefreshCard(card);

        if (animateIn)
        {
            cg.alpha = 0f;
            rect.anchoredPosition = new Vector2(enterOffsetX, 0f);
        }
        else
        {
            cg.alpha = 1f;
        }

        return card;
    }

    private void RefreshCard(QuestCard card)
    {
        if (card == null || card.quest == null || card.quest.data == null) return;
        QuestData data = card.quest.data;
        int target = Mathf.Max(1, data.targetAmount);

        if (card.header != null)
            card.header.text = card.quest.completed ? completedLabel : headerText;
        if (card.title != null)
            card.title.text = data.questTitle;
        if (card.progress != null)
        {
            string prefix = showObjectiveLabel ? GetObjectiveLabel(data.objectiveType) + "  " : string.Empty;
            card.progress.text = $"{prefix}{Mathf.Clamp(card.quest.progress, 0, target)}/{target}";
        }
    }

    private void BeginCompletion(QuestCard card)
    {
        if (card == null || card.completing) return;
        card.completing = true;
        RefreshCard(card);

        card.sequence?.Kill();
        card.rect.DOKill();
        card.background.DOKill();
        if (card.header != null) card.header.DOKill();
        if (card.title != null) card.title.DOKill();
        if (card.progress != null) card.progress.DOKill();

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(card.background.DOColor(completeCardColor, completeColorDuration));
        if (card.header != null) seq.Join(card.header.DOColor(completeTextColor, completeColorDuration));
        if (card.title != null) seq.Join(card.title.DOColor(completeTextColor, completeColorDuration));
        if (card.progress != null) seq.Join(card.progress.DOColor(completeTextColor, completeColorDuration));

        seq.Append(card.rect.DOScale(Vector3.one * completePunchScale, completePunchDuration * 0.45f).SetEase(Ease.OutQuad));
        seq.Append(card.rect.DOScale(Vector3.one, completePunchDuration * 0.55f).SetEase(Ease.OutBack));
        seq.AppendInterval(completeHoldDuration);

        Vector2 exitTarget = card.rect.anchoredPosition + new Vector2(exitOffsetX, 0f);
        seq.Append(card.canvasGroup.DOFade(0f, exitDuration));
        seq.Join(card.rect.DOAnchorPos(exitTarget, exitDuration).SetEase(exitEase));
        seq.OnComplete(() => FinishCard(card));
        card.sequence = seq;
    }

    private void FinishCard(QuestCard card)
    {
        if (card == null) return;
        ActiveQuest quest = card.quest;
        cards.Remove(quest);
        if (card.root != null) Destroy(card.root);
        ReflowCards(true);

        if (QuestManager.Instance != null)
            QuestManager.Instance.FinalizeQuestPresentation(quest);
    }

    private void ReflowCards(bool animate)
    {
        int index = 0;
        foreach (KeyValuePair<ActiveQuest, QuestCard> pair in cards)
        {
            QuestCard card = pair.Value;
            if (card == null || card.rect == null) continue;
            float step = cardSize.y + cardSpacing;
            float y = stackUpward ? index * step : -index * step;
            Vector2 target = new Vector2(0f, y);

            card.rect.DOKill();
            if (animate)
            {
                card.canvasGroup.DOKill();
                if (card.canvasGroup.alpha < 1f)
                    card.canvasGroup.DOFade(1f, enterDuration).SetUpdate(true);
                card.rect.DOAnchorPos(target, enterDuration).SetEase(enterEase).SetUpdate(true);
            }
            else
            {
                card.rect.anchoredPosition = target;
            }
            index++;
        }
    }

    private TMP_Text CreateText(Transform parent, string name, string value, int size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void SetTextRect(RectTransform rect, float left, float right, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(left, y);
        rect.offsetMax = new Vector2(-right, y + height);
    }

    private string GetObjectiveLabel(QuestObjectiveType type)
    {
        switch (type)
        {
            case QuestObjectiveType.SpendGold: return spendGoldLabel;
            case QuestObjectiveType.UpgradeTowers: return upgradeLabel;
            default: return killLabel;
        }
    }

    private void KillAllTweens()
    {
        foreach (KeyValuePair<ActiveQuest, QuestCard> pair in cards)
        {
            QuestCard card = pair.Value;
            if (card == null) continue;
            card.sequence?.Kill();
            card.rect?.DOKill();
            card.canvasGroup?.DOKill();
            card.background?.DOKill();
            card.header?.DOKill();
            card.title?.DOKill();
            card.progress?.DOKill();
        }
    }

    private static string SafeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Quest";
        return value.Replace(' ', '_').Replace('/', '_').Replace('\\', '_');
    }
}
