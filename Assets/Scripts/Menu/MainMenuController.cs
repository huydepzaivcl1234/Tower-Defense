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

    private static bool startGameplayAfterSceneReload;

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

    [Header("Play Fade Transition")]
    [Tooltip("Fade the screen to black before entering gameplay, then fade back in.")]
    public bool fadeOnPlay = true;
    [Min(0f)] public float fadeOutDuration = 0.55f;
    [Min(0f)] public float blackHoldDuration = 0.12f;
    [Min(0f)] public float fadeInDuration = 0.70f;
    public Color fadeColor = Color.black;

    private AudioSettingsManager audioSettings;
    private MenuScreenFader screenFader;
    private bool menuBlocksGameplay;
    private bool gameplayStarted;
    private bool playTransitionInProgress;
    private SettingsReturnTarget settingsReturnTarget = SettingsReturnTarget.MainMenu;

    public bool IsMainMenuVisible => mainPanel != null && mainPanel.activeSelf;
    public bool IsSettingsVisible => settingsPanel != null && settingsPanel.activeSelf;
    public bool IsAnyMenuVisible => IsMainMenuVisible || IsSettingsVisible;
    public bool GameplayStarted => gameplayStarted;

    public static void RequestGameplayAfterSceneReload() => startGameplayAfterSceneReload = true;
    public static void ClearSceneReloadRequest() => startGameplayAfterSceneReload = false;

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
            audioSettings = Object.FindAnyObjectByType<AudioSettingsManager>(FindObjectsInactive.Include);
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

        if (startGameplayAfterSceneReload)
        {
            startGameplayAfterSceneReload = false;
            EnterGameplayImmediately();
            return;
        }

        if (showMenuOnSceneStart)
            ShowMainMenu();
        else
            EnterGameplayImmediately();
    }

    private void LateUpdate()
    {
        if (menuBlocksGameplay && Time.timeScale != 0f)
            Time.timeScale = 0f;
    }

    public void ShowMainMenu()
    {
        gameplayStarted = false;
        playTransitionInProgress = false;
        settingsReturnTarget = SettingsReturnTarget.MainMenu;
        menuBlocksGameplay = true;
        Time.timeScale = 0f;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void PlayGame()
    {
        if (playTransitionInProgress) return;

        if (!fadeOnPlay || (fadeOutDuration <= 0f && blackHoldDuration <= 0f && fadeInDuration <= 0f))
        {
            EnterGameplayImmediately();
            return;
        }

        playTransitionInProgress = true;
        menuBlocksGameplay = true;
        Time.timeScale = 0f;

        screenFader = MenuScreenFader.GetOrCreate();
        screenFader.SetFadeColor(fadeColor);
        screenFader.PlayTransition(
            fadeOutDuration,
            blackHoldDuration,
            fadeInDuration,
            EnterGameplayAtBlack,
            () => playTransitionInProgress = false);
    }

    private void EnterGameplayAtBlack()
    {
        gameplayStarted = true;
        settingsReturnTarget = SettingsReturnTarget.Gameplay;
        HideAllMenus();
        menuBlocksGameplay = false;
        RestoreGameplaySpeed();
    }

    private void EnterGameplayImmediately()
    {
        gameplayStarted = true;
        settingsReturnTarget = SettingsReturnTarget.Gameplay;
        menuBlocksGameplay = false;
        HideAllMenus();
        RestoreGameplaySpeed();
        playTransitionInProgress = false;
    }

    public void OpenSettings()
    {
        if (playTransitionInProgress) return;
        settingsReturnTarget = SettingsReturnTarget.MainMenu;
        OpenSettingsInternal();
    }

    public void OpenSettingsFromGameplay(bool returnToPauseMenu)
    {
        if (!gameplayStarted || playTransitionInProgress) return;
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
            audioSettings = AudioSettingsManager.Instance ?? Object.FindAnyObjectByType<AudioSettingsManager>(FindObjectsInactive.Include);

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
