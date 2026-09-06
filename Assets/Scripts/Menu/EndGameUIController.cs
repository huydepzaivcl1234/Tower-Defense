using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Unified Win/Lose screen with Retry and Main Menu actions.
/// </summary>
public class EndGameUIController : MonoBehaviour
{
    public static EndGameUIController Instance { get; private set; }

    [Header("Panels")]
    public GameObject rootPanel;
    public GameObject winContent;
    public GameObject loseContent;

    [Header("Win Diamond Summary")]
    public TMP_Text diamondsEarnedText;
    public TMP_Text diamondsTotalText;
    public string diamondsEarnedFormat = "+{0} DIAMONDS";
    public string diamondsTotalFormat = "TOTAL {0}";
    public bool useCompactDiamondNumbers = true;
    public bool hideEarnedTextWhenZero = false;

    [Header("Buttons")]
    public Button continueButton;
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Transition")]
    public bool useMainMenuFadeSettings = true;
    public bool fadeOnAction = true;
    [Min(0f)] public float fadeOutDuration = 0.55f;
    [Min(0f)] public float blackHoldDuration = 0.12f;
    [Min(0f)] public float fadeInDuration = 0.70f;
    public Color fadeColor = Color.black;

    private bool transitionInProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.OnGameWon += ShowWin;
        GameManager.OnGameOver += ShowLose;
    }

    private void OnDisable()
    {
        GameManager.OnGameWon -= ShowWin;
        GameManager.OnGameOver -= ShowLose;
    }

    private void Start()
    {
        EnsureContinueButton();
        if (continueButton != null) continueButton.onClick.AddListener(ContinueToNextStoryLevel);
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        PrepareWinSummaryLayout();
        Hide();
        SyncFadeSettings();
    }

    private void SyncFadeSettings()
    {
        if (!useMainMenuFadeSettings || MainMenuController.Instance == null) return;

        MainMenuController menu = MainMenuController.Instance;
        fadeOnAction = menu.fadeOnPlay;
        fadeOutDuration = menu.fadeOutDuration;
        blackHoldDuration = menu.blackHoldDuration;
        fadeInDuration = menu.fadeInDuration;
        fadeColor = menu.fadeColor;
    }

    private void ShowWin()
    {
        transitionInProgress = false;
        SyncFadeSettings();
        RefreshDiamondSummary();
        RefreshContinueOption();
        if (rootPanel != null) rootPanel.SetActive(true);
        if (winContent != null) winContent.SetActive(true);
        if (loseContent != null) loseContent.SetActive(false);
    }

    private void RefreshDiamondSummary()
    {
        int earned = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DiamondsEarnedThisRun : 0;
        int total = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.CurrentDiamonds : 0;

        string earnedValue = useCompactDiamondNumbers ? CompactNumber.Format(earned) : earned.ToString("N0");
        string totalValue = useCompactDiamondNumbers ? CompactNumber.Format(total) : total.ToString("N0");

        if (diamondsEarnedText != null)
        {
            diamondsEarnedText.text = string.Format(diamondsEarnedFormat, earnedValue);
            diamondsEarnedText.gameObject.SetActive(!hideEarnedTextWhenZero || earned > 0);
        }

        if (diamondsTotalText != null)
            diamondsTotalText.text = string.Format(diamondsTotalFormat, totalValue);
    }

    private void ShowLose()
    {
        transitionInProgress = false;
        SyncFadeSettings();
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        ApplyButtonLayout(false);
        if (rootPanel != null) rootPanel.SetActive(true);
        if (winContent != null) winContent.SetActive(false);
        if (loseContent != null) loseContent.SetActive(true);
    }

    public void Hide()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    public void ContinueToNextStoryLevel()
    {
        if (transitionInProgress) return;

        WaveManager waveManager = WaveManager.Instance;
        if (waveManager == null || !waveManager.SelectNextStoryLevelForReload())
        {
            RefreshContinueOption();
            return;
        }

        StartReloadTransition(true);
    }

    public void Retry()
    {
        if (transitionInProgress) return;
        StartReloadTransition(true);
    }

    public void ReturnToMainMenu()
    {
        if (transitionInProgress) return;
        StartReloadTransition(false);
    }

    private void StartReloadTransition(bool retry)
    {
        transitionInProgress = true;
        SyncFadeSettings();
        Time.timeScale = 0f;

        if (!fadeOnAction || (fadeOutDuration <= 0f && blackHoldDuration <= 0f && fadeInDuration <= 0f))
        {
            ReloadScene(retry);
            return;
        }

        MenuScreenFader fader = MenuScreenFader.GetOrCreate();
        fader.SetFadeColor(fadeColor);
        fader.PlayTransition(
            fadeOutDuration,
            blackHoldDuration,
            fadeInDuration,
            () => ReloadScene(retry));
    }

    private void EnsureContinueButton()
    {
        if (continueButton != null || retryButton == null)
            return;

        continueButton = Instantiate(retryButton, retryButton.transform.parent);
        continueButton.name = "ContinueButton";
        continueButton.onClick.RemoveAllListeners();

        TMP_Text label = continueButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "CONTINUE";

        continueButton.gameObject.SetActive(false);
    }

    private void RefreshContinueOption()
    {
        bool canContinue = WaveManager.Instance != null && WaveManager.Instance.CanContinueToNextStoryLevel;
        if (continueButton != null)
            continueButton.gameObject.SetActive(canContinue);

        ApplyButtonLayout(canContinue);
    }

    private void PrepareWinSummaryLayout()
    {
        if (diamondsEarnedText != null)
        {
            RectTransform earnedRect = diamondsEarnedText.rectTransform;
            earnedRect.anchoredPosition = new Vector2(earnedRect.anchoredPosition.x, 34f);
            earnedRect.sizeDelta = new Vector2(Mathf.Max(420f, earnedRect.sizeDelta.x), 42f);
            diamondsEarnedText.fontSize = Mathf.Max(26f, diamondsEarnedText.fontSize);
            diamondsEarnedText.fontStyle |= FontStyles.Bold;
            diamondsEarnedText.color = new Color(0.10f, 0.82f, 1f, 1f);

            Shadow shadow = diamondsEarnedText.GetComponent<Shadow>();
            if (shadow == null)
                shadow = diamondsEarnedText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.04f, 0.08f, 0.92f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }

        if (diamondsTotalText != null)
        {
            RectTransform totalRect = diamondsTotalText.rectTransform;
            totalRect.anchoredPosition = new Vector2(totalRect.anchoredPosition.x, -2f);
            diamondsTotalText.color = Color.white;
        }

        Transform prompt = FindDeepChild(rootPanel != null ? rootPanel.transform : transform, "Prompt");
        if (prompt is RectTransform promptRect)
            promptRect.anchoredPosition = new Vector2(promptRect.anchoredPosition.x, -42f);
    }

    private void ApplyButtonLayout(bool showContinue)
    {
        if (showContinue)
        {
            SetButtonLayout(continueButton, -96f, 62f);
            SetButtonLayout(retryButton, -166f, 62f);
            SetButtonLayout(mainMenuButton, -236f, 62f);
            return;
        }

        SetButtonLayout(retryButton, -90f, 76f);
        SetButtonLayout(mainMenuButton, -188f, 70f);
    }

    private static void SetButtonLayout(Button button, float y, float height)
    {
        if (button == null)
            return;

        RectTransform rect = button.transform as RectTransform;
        if (rect == null)
            return;

        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
            return null;
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void ReloadScene(bool retry)
    {
        if (retry)
            MainMenuController.RequestGameplayAfterSceneReload();
        else
        {
            MainMenuController.ClearSceneReloadRequest();
            WaveManager.ResetRunSelection();
        }

        Time.timeScale = 0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
