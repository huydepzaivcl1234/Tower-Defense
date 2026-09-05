using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu flow for the current gameplay scene. Supports main menu, profile, settings,
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
    public GameObject profilePanel;
    public GameObject settingsPanel;
    public GameObject shopPanel;
    [Tooltip("Optional. If empty, the Story / Endless panel is created at runtime using the current Play button style.")]
    public GameObject gameModePanel;

    [Header("Main Buttons")]
    public Button playButton;
    public Button profileButton;
    public Button settingsButton;
    public Button exitButton;
    public Button shopButton;

    [Header("Settings")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Text masterValueText;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;
    public Button backButton;

    [Header("Reset Data")]
    [Tooltip("Optional Settings button. First click arms confirmation; second click within the confirmation window resets data.")]
    public Button resetDataButton;
    [Tooltip("Optional label belonging to Reset Data button.")]
    public TMP_Text resetDataButtonText;
    public string resetDataNormalLabel = "RESET DATA";
    public string resetDataConfirmLabel = "CLICK AGAIN TO CONFIRM";
    [Min(0.5f)] public float resetDataConfirmationSeconds = 4f;
    [Tooltip("Reset persistent PlayerProfileData such as Diamonds/shop progression/profile lifetime stats.")]
    public bool resetProfileData = true;
    [Tooltip("Also reset Master/Music/SFX PlayerPrefs to the customizable defaults in AudioSettingsManager.")]
    public bool resetAudioSettingsToo = false;
    [Tooltip("After profile reset, immediately create/save a clean profile using PlayerProfileManager starting values.")]
    public bool saveFreshProfileAfterReset = true;
    [Tooltip("Optional status label shown after reset.")]
    public TMP_Text resetDataStatusText;
    public string resetDataSuccessMessage = "DATA RESET COMPLETE";

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
    private float resetDataArmedUntil = -1f;
    private SettingsReturnTarget settingsReturnTarget = SettingsReturnTarget.MainMenu;

    public bool IsMainMenuVisible => mainPanel != null && mainPanel.activeSelf;
    public bool IsProfileVisible => profilePanel != null && profilePanel.activeSelf;
    public bool IsSettingsVisible => settingsPanel != null && settingsPanel.activeSelf;
    public bool IsShopVisible => shopPanel != null && shopPanel.activeSelf;
    public bool IsGameModeVisible => gameModePanel != null && gameModePanel.activeSelf;
    public bool IsAnyMenuVisible => IsMainMenuVisible || IsProfileVisible || IsSettingsVisible || IsShopVisible || IsGameModeVisible;
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
        if (playButton != null) playButton.onClick.AddListener(OpenGameModeSelection);
        if (profileButton != null) profileButton.onClick.AddListener(OpenProfile);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (shopButton != null) shopButton.onClick.AddListener(OpenShop);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);
        if (resetDataButton != null) resetDataButton.onClick.AddListener(RequestResetData);

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        SyncAudioUI();
        ResetResetDataConfirmationUI();

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

    private void Update()
    {
        if (resetDataArmedUntil > 0f && Time.unscaledTime > resetDataArmedUntil)
            ResetResetDataConfirmationUI();
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

        ResetResetDataConfirmationUI();
        if (profilePanel != null) profilePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (gameModePanel != null) gameModePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void OpenProfile()
    {
        if (playTransitionInProgress || gameplayStarted)
            return;

        menuBlocksGameplay = true;
        Time.timeScale = 0f;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (gameModePanel != null) gameModePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(true);
    }

    public void CloseProfile()
    {
        if (profilePanel != null) profilePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        menuBlocksGameplay = true;
        Time.timeScale = 0f;
    }

    public void OpenShop()
    {
        if (playTransitionInProgress || gameplayStarted)
            return;

        menuBlocksGameplay = true;
        Time.timeScale = 0f;
        if (profilePanel != null) profilePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (gameModePanel != null) gameModePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        menuBlocksGameplay = true;
        Time.timeScale = 0f;
    }

    public void OpenGameModeSelection()
    {
        if (playTransitionInProgress || gameplayStarted)
            return;

        // Returning to the menu from Pause keeps the current run alive; Play resumes it.
        if (WaveManager.Instance != null && WaveManager.Instance.CurrentWaveNumber > 0)
        {
            PlayGame();
            return;
        }

        GameModeSelectionPanel selector = gameModePanel != null
            ? gameModePanel.GetComponent<GameModeSelectionPanel>()
            : null;
        if (selector == null)
        {
            selector = GameModeSelectionPanel.CreateRuntime(this, playButton);
            gameModePanel = selector != null ? selector.gameObject : null;
        }

        // A scene without a usable selector keeps the old Play behaviour.
        if (selector == null)
        {
            WaveManager.Instance?.ConfigureStoryMode(0);
            PlayGame();
            return;
        }

        menuBlocksGameplay = true;
        Time.timeScale = 0f;
        if (profilePanel != null) profilePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        selector.ShowModeChoices();
    }

    public void CloseGameModeSelection()
    {
        if (gameModePanel != null) gameModePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        menuBlocksGameplay = true;
        Time.timeScale = 0f;
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
        ResetResetDataConfirmationUI();
        if (profilePanel != null) profilePanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (gameModePanel != null) gameModePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        ResetResetDataConfirmationUI();
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

    public void RequestResetData()
    {
        if (resetDataArmedUntil > 0f && Time.unscaledTime <= resetDataArmedUntil)
        {
            ConfirmResetData();
            return;
        }

        resetDataArmedUntil = Time.unscaledTime + Mathf.Max(0.5f, resetDataConfirmationSeconds);
        if (resetDataButtonText != null)
            resetDataButtonText.text = resetDataConfirmLabel;
        if (resetDataStatusText != null)
            resetDataStatusText.text = string.Empty;
    }

    public void ConfirmResetData()
    {
        resetDataArmedUntil = -1f;

        if (resetProfileData)
        {
            PlayerProfileManager profile = PlayerProfileManager.Instance;
            if (profile == null)
                profile = Object.FindAnyObjectByType<PlayerProfileManager>(FindObjectsInactive.Include);

            if (profile != null)
                profile.ResetProfileData(saveFreshProfileAfterReset);
            else
                Debug.LogWarning("Reset Data requested but no PlayerProfileManager exists in the scene.", this);
        }

        if (resetAudioSettingsToo)
        {
            if (audioSettings == null)
                audioSettings = AudioSettingsManager.Instance ?? Object.FindAnyObjectByType<AudioSettingsManager>(FindObjectsInactive.Include);
            audioSettings?.ResetToDefaults(true);
            SyncAudioUI();
        }

        if (resetDataStatusText != null)
            resetDataStatusText.text = resetDataSuccessMessage;
        if (resetDataButtonText != null)
            resetDataButtonText.text = resetDataNormalLabel;
    }

    public void CancelResetData()
    {
        ResetResetDataConfirmationUI();
    }

    private void ResetResetDataConfirmationUI()
    {
        resetDataArmedUntil = -1f;
        if (resetDataButtonText != null)
            resetDataButtonText.text = resetDataNormalLabel;
    }

    public void ReturnToMainMenuFromGameplay()
    {
        ShowMainMenu();
    }

    private void HideAllMenus()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (profilePanel != null) profilePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (gameModePanel != null) gameModePanel.SetActive(false);
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
