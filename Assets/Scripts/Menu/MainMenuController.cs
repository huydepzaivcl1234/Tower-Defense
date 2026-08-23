using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu flow for the current gameplay scene. The game starts paused behind
/// an opaque/semi-opaque menu and resumes only after Play is pressed.
/// </summary>
public class MainMenuController : MonoBehaviour
{
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

    private void Awake()
    {
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
            HideAllMenus();
    }

    public void ShowMainMenu()
    {
        Time.timeScale = 0f;
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        HideAllMenus();
        Time.timeScale = Mathf.Max(0.01f, gameplayTimeScale);
    }

    public void OpenSettings()
    {
        SyncAudioUI();
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    private void HideAllMenus()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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
