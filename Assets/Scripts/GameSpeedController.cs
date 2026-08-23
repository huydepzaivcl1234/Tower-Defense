using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tower-defense game speed control. Changes Time.timeScale between 1x/2x/3x without touching
/// gameplay stats. Game over/win can still set Time.timeScale to 0 through GameManager.
/// </summary>
public class GameSpeedController : MonoBehaviour
{
    public static GameSpeedController Instance { get; private set; }

    [Header("Buttons")]
    public Button speed1Button;
    public Button speed2Button;
    public Button speed3Button;
    public TMP_Text currentSpeedText;

    [Header("Visual State")]
    public Color selectedColor = new Color(0.28f, 0.78f, 1f, 1f);
    public Color normalColor = Color.white;

    public int CurrentMultiplier { get; private set; } = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (speed1Button != null) speed1Button.onClick.AddListener(() => SetSpeed(1));
        if (speed2Button != null) speed2Button.onClick.AddListener(() => SetSpeed(2));
        if (speed3Button != null) speed3Button.onClick.AddListener(() => SetSpeed(3));
        SetSpeed(1);
    }

    public void SetSpeed(int multiplier)
    {
        multiplier = Mathf.Clamp(multiplier, 1, 3);
        CurrentMultiplier = multiplier;

        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
            Time.timeScale = multiplier;

        if (currentSpeedText != null)
            currentSpeedText.text = $"{multiplier}x";

        RefreshButton(speed1Button, multiplier == 1);
        RefreshButton(speed2Button, multiplier == 2);
        RefreshButton(speed3Button, multiplier == 3);
    }

    private void RefreshButton(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null) return;
        button.targetGraphic.color = selected ? selectedColor : normalColor;
    }
}
