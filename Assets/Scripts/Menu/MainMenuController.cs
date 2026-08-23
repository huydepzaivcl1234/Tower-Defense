using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu flow for the current gameplay scene. Supports normal main-menu settings
/// plus settings opened from active gameplay/pause without forcing the player back to main menu.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    private enum SettingsReturnTarget
    {
        MainMenu,
        Gameplay,
        PauseMenu
    }

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Main Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button exitButton;

    [Header("Settings")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Text masterValueText;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;
    public Button backButton;

    [Header("Behaviour")]
    public bool showMenuOnSceneStart = true;
    public float gameplayTimeScale = 1f;

    private AudioSettingsManager audioSettings;
    private bool menuBlocksGameplay;
    private bool gameplayStarted;
    private SettingsReturnTarget settingsReturnTarget = SettingsReturnTarget.MainMenu;

    public bool IsMainMenuVisible => mainPanel != null && mainPanel.activeSelf;
    public bool IsSettingsVisible => settingsPanel != null && settingsPanel.activeSelf;
    public bool IsAnyMenuVisible => IsMainMenuVisible || IsSettingsVisible;
    public bool GameplayStarted => gameplayStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSettings = AudioSettingsManager.Instance;
        if (audioSettings == null)
            audioSettings = Object.FindFirstObjectByType<AudioSettingsManager>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        SyncAudioUI();

        if (showMenuOnSceneStart)
            ShowMainMenu();
        else
        {
            gameplayStarted = true;
            menuBlocksGameplay = false;
            HideAllMenus();
        }
    }

    private void LateUpdate()
    {
        // Main menu/settings are modal. GameSpeedController must not be able to unpause behind them.
        if (menuBlocksGameplay && Time.timeScale != 0f)
            Time.timeScale = 0f;
    }

    public void ShowMainMenu()
    {
        gameplayStarted = false;
        settingsReturnTarget = SettingsReturnTarget.MainMenu;
        menuBlocksGameplay = true;
        Time.timeScale = 0f;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void PlayGame()
    {
        gameplayStarted = true;
        settingsReturnTarget = SettingsReturnTarget.Gameplay;
        menuBlocksGameplay = false;
        HideAllMenus();
        RestoreGameplaySpeed();
    }

    public void OpenSettings()
    {
        settingsReturnTarget = SettingsReturnTarget.MainMenu;
        OpenSettingsInternal();
    }

    /// <summary>
    /// Opens the existing Settings panel from gameplay. Back returns either to the pause menu
    /// or straight to gameplay depending on where the gear button was pressed.
    /// </summary>
    public void OpenSettingsFromGameplay(bool returnToPauseMenu)
    {
        if (!gameplayStarted) return;
        settingsReturnTarget = returnToPauseMenu ? SettingsReturnTarget.PauseMenu : SettingsReturnTarget.Gameplay;
        OpenSettingsInternal();
    }

    private void OpenSettingsInternal()
    {
        menuBlocksGameplay = true;
        Time.timeScale = 0f;
        SyncAudioUI();
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        switch (settingsReturnTarget)
        {
            case SettingsReturnTarget.PauseMenu:
                menuBlocksGameplay = false;
                if (PauseMenuController.Instance != null)
                    PauseMenuController.Instance.ReturnFromSettingsToPause();
                else
                    ShowMainMenu();
                break;

            case SettingsReturnTarget.Gameplay:
                menuBlocksGameplay = false;
                if (PauseMenuController.Instance != null)
                    PauseMenuController.Instance.ReturnFromSettingsToGameplay();
                else
                    RestoreGameplaySpeed();
                break;

            default:
                menuBlocksGameplay = true;
                if (mainPanel != null) mainPanel.SetActive(true);
                Time.timeScale = 0f;
                break;
        }
    }

    public void ReturnToMainMenuFromGameplay()
    {
        ShowMainMenu();
    }

    private void HideAllMenus()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void RestoreGameplaySpeed()
    {
        if (!gameplayStarted) return;

        int multiplier = GameSpeedController.Instance != null
            ? GameSpeedController.Instance.CurrentMultiplier
            : Mathf.Max(1, Mathf.RoundToInt(gameplayTimeScale));

        Time.timeScale = Mathf.Clamp(multiplier, 1, 3);
    }

    private void SyncAudioUI()
    {
        if (audioSettings == null)
            audioSettings = AudioSettingsManager.Instance ?? Object.FindFirstObjectByType<AudioSettingsManager>(FindObjectsInactive.Include);

        float master = audioSettings != null ? audioSettings.MasterVolume : 1f;
        float music = audioSettings != null ? audioSettings.MusicVolume : 0.8f;
        float sfx = audioSettings != null ? audioSettings.SfxVolume : 1f;

        if (masterSlider != null) masterSlider.SetValueWithoutNotify(master);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);
        RefreshValueLabels(master, music, sfx);
    }

    private void OnMasterChanged(float value)
    {
        audioSettings?.SetMaster(value);
        if (masterValueText != null) masterValueText.text = ToPercent(value);
    }

    private void OnMusicChanged(float value)
    {
        audioSettings?.SetMusic(value);
        if (musicValueText != null) musicValueText.text = ToPercent(value);
    }

    private void OnSfxChanged(float value)
    {
        audioSettings?.SetSfx(value);
        if (sfxValueText != null) sfxValueText.text = ToPercent(value);
    }

    private void RefreshValueLabels(float master, float music, float sfx)
    {
        if (masterValueText != null) masterValueText.text = ToPercent(master);
        if (musicValueText != null) musicValueText.text = ToPercent(music);
        if (sfxValueText != null) sfxValueText.text = ToPercent(sfx);
    }

    private static string ToPercent(float value) => $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
