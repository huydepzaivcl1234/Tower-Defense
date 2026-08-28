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
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);

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
        if (rootPanel != null) rootPanel.SetActive(true);
        if (winContent != null) winContent.SetActive(false);
        if (loseContent != null) loseContent.SetActive(true);
    }

    public void Hide()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
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

    private static void ReloadScene(bool retry)
    {
        if (retry)
            MainMenuController.RequestGameplayAfterSceneReload();
        else
            MainMenuController.ClearSceneReloadRequest();

        Time.timeScale = 0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
