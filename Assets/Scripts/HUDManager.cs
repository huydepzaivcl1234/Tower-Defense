using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main HUD: gold, lives, wave counter, start-wave button, and end-game panels.
/// Visual layout can be replaced freely; gameplay/event wiring stays here.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("Status Text")]
    public TMP_Text goldText;
    public TMP_Text livesText;
    public TMP_Text waveText;

    [Header("Wave Control")]
    public Button startWaveButton;

    [Header("End Screens")]
    public GameObject gameOverPanel;
    public GameObject winPanel;
    [Tooltip("The restart button that lives ON the Game Over panel")]
    public Button gameOverRestartButton;
    [Tooltip("The restart button that lives ON the Win panel (a separate button, even though it does the same thing)")]
    public Button winRestartButton;

    private void OnEnable()
    {
        GameManager.OnGoldChanged += UpdateGold;
        GameManager.OnLivesChanged += UpdateLives;
        GameManager.OnGameOver += ShowGameOver;
        GameManager.OnGameWon += ShowWin;
    }

    private void OnDisable()
    {
        GameManager.OnGoldChanged -= UpdateGold;
        GameManager.OnLivesChanged -= UpdateLives;
        GameManager.OnGameOver -= ShowGameOver;
        GameManager.OnGameWon -= ShowWin;
    }

    private void Start()
    {
        if (startWaveButton != null) startWaveButton.onClick.AddListener(OnStartWavePressed);
        if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(() => GameManager.Instance?.RestartLevel());
        if (winRestartButton != null) winRestartButton.onClick.AddListener(() => GameManager.Instance?.RestartLevel());

        UpdateGold(GameManager.Instance != null ? GameManager.Instance.CurrentGold : 0);
        UpdateLives(GameManager.Instance != null ? GameManager.Instance.CurrentLives : 0);
        UpdateWaveText();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
    }

    private void Update()
    {
        UpdateWaveText();
        if (startWaveButton != null && WaveManager.Instance != null)
            startWaveButton.interactable = WaveManager.Instance.CanStartNextWave();
    }

    private void UpdateGold(int value)
    {
        if (goldText != null) goldText.text = CompactNumber.Format(value);
    }

    private void UpdateLives(int value)
    {
        if (livesText != null) livesText.text = CompactNumber.Format(value);
    }

    private void UpdateWaveText()
    {
        if (waveText == null || WaveManager.Instance == null) return;
        int total = Mathf.Max(1, WaveManager.Instance.TotalWaves);
        int current = Mathf.Clamp(WaveManager.Instance.CurrentWaveNumber, 1, total);
        waveText.text = $"Wave {CompactNumber.Format(current)} / {CompactNumber.Format(total)}";
    }

    private void OnStartWavePressed() => WaveManager.Instance?.StartNextWave();

    private void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    private void ShowWin()
    {
        if (winPanel != null) winPanel.SetActive(true);
    }
}
