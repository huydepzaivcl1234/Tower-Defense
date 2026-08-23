using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game pause/settings controller. ESC toggles pause, Continue restores the selected game speed,
/// Main Menu returns to the existing main menu, and the gear button opens the shared Settings panel.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("References")]
    public MainMenuController mainMenu;
    public GameObject pausePanel;
    public Button continueButton;
    public Button mainMenuButton;
    public Button gearButton;

    [Header("Input")]
    public KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (mainMenu == null)
            mainMenu = MainMenuController.Instance ?? Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);

        if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (gearButton != null) gearButton.onClick.AddListener(OpenSettingsFromGear);

        if (pausePanel != null) pausePanel.SetActive(false);
        RefreshGearVisibility();
    }

    private void Update()
    {
        if (mainMenu == null)
            mainMenu = MainMenuController.Instance;

        RefreshGearVisibility();

        if (!Input.GetKeyDown(pauseKey)) return;
        if (mainMenu == null || !mainMenu.GameplayStarted) return;

        // ESC inside Settings acts like Back and returns to the exact previous gameplay state.
        if (mainMenu.IsSettingsVisible)
        {
            mainMenu.CloseSettings();
            return;
        }

        if (isPaused) ContinueGame();
        else OpenPauseMenu();
    }

    private void LateUpdate()
    {
        // Other systems such as GameSpeedController must not unpause the game behind this panel.
        if (isPaused && (mainMenu == null || !mainMenu.IsSettingsVisible) && Time.timeScale != 0f)
            Time.timeScale = 0f;
    }

    public void OpenPauseMenu()
    {
        if (mainMenu == null || !mainMenu.GameplayStarted || mainMenu.IsAnyMenuVisible) return;

        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        RefreshGearVisibility();
    }

    public void ContinueGame()
    {
        if (mainMenu == null || !mainMenu.GameplayStarted) return;

        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        mainMenu.RestoreGameplaySpeed();
        RefreshGearVisibility();
    }

    public void ReturnToMainMenu()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (mainMenu != null) mainMenu.ReturnToMainMenuFromGameplay();
        RefreshGearVisibility();
    }

    public void OpenSettingsFromGear()
    {
        if (mainMenu == null || !mainMenu.GameplayStarted || mainMenu.IsSettingsVisible) return;

        bool returnToPause = isPaused;
        if (pausePanel != null) pausePanel.SetActive(false);
        mainMenu.OpenSettingsFromGameplay(returnToPause);
        RefreshGearVisibility();
    }

    public void ReturnFromSettingsToPause()
    {
        if (mainMenu == null || !mainMenu.GameplayStarted) return;

        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        RefreshGearVisibility();
    }

    public void ReturnFromSettingsToGameplay()
    {
        if (mainMenu == null || !mainMenu.GameplayStarted) return;

        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        mainMenu.RestoreGameplaySpeed();
        RefreshGearVisibility();
    }

    private void RefreshGearVisibility()
    {
        if (gearButton == null) return;

        bool show = mainMenu != null &&
                    mainMenu.GameplayStarted &&
                    !mainMenu.IsMainMenuVisible &&
                    !mainMenu.IsSettingsVisible;

        if (gearButton.gameObject.activeSelf != show)
            gearButton.gameObject.SetActive(show);
    }
}
