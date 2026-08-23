using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Main HUD: gold, lives, wave counter, start-wave button, and end-game panels.
/// Adds lightweight UI juice only; gameplay values still come from GameManager.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("Status Text")]
    public TMP_Text goldText;
    public TMP_Text livesText;
    public TMP_Text waveText;

    [Header("Gold Count Animation")]
    [Min(0.05f)] public float goldTweenDuration = 0.35f;

    [Header("Low Lives Warning")]
    [Tooltip("Lives at or below this value pulse red.")]
    [Min(1)] public int lowLivesThreshold = 5;
    public Color lowLivesColor = new Color(1f, 0.18f, 0.18f, 1f);
    [Min(0.1f)] public float lowLivesPulseDuration = 0.45f;
    [Range(1.02f, 1.35f)] public float lowLivesPulseScale = 1.12f;

    [Header("Wave Control")]
    public Button startWaveButton;

    [Header("End Screens")]
    public GameObject gameOverPanel;
    public GameObject winPanel;
    [Tooltip("The restart button that lives ON the Game Over panel")]
    public Button gameOverRestartButton;
    [Tooltip("The restart button that lives ON the Win panel (a separate button, even though it does the same thing)")]
    public Button winRestartButton;

    private Tween goldTween;
    private Sequence livesWarningTween;
    private float displayedGold;
    private Color livesNormalColor = Color.white;
    private Vector3 livesBaseScale = Vector3.one;
    private bool initialized;

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

        goldTween?.Kill();
        livesWarningTween?.Kill();
    }

    private void Start()
    {
        if (startWaveButton != null) startWaveButton.onClick.AddListener(OnStartWavePressed);
        if (gameOverRestartButton != null) gameOverRestartButton.onClick.AddListener(() => GameManager.Instance?.RestartLevel());
        if (winRestartButton != null) winRestartButton.onClick.AddListener(() => GameManager.Instance?.RestartLevel());

        if (livesText != null)
        {
            livesNormalColor = livesText.color;
            livesBaseScale = livesText.transform.localScale;
        }

        int initialGold = GameManager.Instance != null ? GameManager.Instance.CurrentGold : 0;
        displayedGold = initialGold;
        if (goldText != null) goldText.text = CompactNumber.Format(initialGold);

        UpdateLives(GameManager.Instance != null ? GameManager.Instance.CurrentLives : 0);
        UpdateWaveText();
        initialized = true;

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
        if (goldText == null) return;

        // Before Start() finishes, snap once so the scene boots cleanly.
        if (!initialized)
        {
            displayedGold = value;
            goldText.text = CompactNumber.Format(value);
            return;
        }

        goldTween?.Kill();
        float start = displayedGold;
        goldTween = DOTween.To(
                () => start,
                x =>
                {
                    start = x;
                    displayedGold = x;
                    goldText.text = CompactNumber.Format(Mathf.RoundToInt(x));
                },
                value,
                goldTweenDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                displayedGold = value;
                goldText.text = CompactNumber.Format(value);
            });
    }

    private void UpdateLives(int value)
    {
        if (livesText == null) return;
        livesText.text = CompactNumber.Format(value);

        livesWarningTween?.Kill();
        livesText.transform.localScale = livesBaseScale;

        if (value > 0 && value <= lowLivesThreshold)
        {
            livesText.color = lowLivesColor;
            livesWarningTween = DOTween.Sequence()
                .SetUpdate(true)
                .Append(livesText.transform.DOScale(livesBaseScale * lowLivesPulseScale, lowLivesPulseDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(livesText.transform.DOScale(livesBaseScale, lowLivesPulseDuration * 0.5f).SetEase(Ease.InQuad))
                .SetLoops(-1, LoopType.Restart);
        }
        else
        {
            livesText.color = livesNormalColor;
        }
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
