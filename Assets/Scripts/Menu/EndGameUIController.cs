using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Unified Win/Lose screen with Retry and Main Menu actions.
/// Uses the same fade system as the main Play button and reloads the active scene
/// so the whole run (waves, towers, enemies, relics, gold/lives) starts clean.
/// </summary>
public class EndGameUIController : MonoBehaviour
{
    public static EndGameUIController Instance { get; private set; }

    [Header("Panels")]
    public GameObject rootPanel;
    public GameObject winContent;
    public GameObject loseContent;

    [Header("Buttons")]
    public Button retryButton;
    public Button mainMenuButton;

    [Header("Transition")]
    [Tooltip("When enabled, transition timing/color are copied from MainMenuController so Play/Retry/Main Menu feel identical.")]
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
        if (rootPanel != null) rootPanel.SetActive(true);
        if (winContent != null) winContent.SetActive(true);
        if (loseContent != null) loseContent.SetActive(false);
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
