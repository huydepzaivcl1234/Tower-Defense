using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tower-defense game speed control. Changes Time.timeScale between 1x/2x/3x without touching
/// gameplay stats. Game over/win can still set Time.timeScale to 0 through GameManager.
/// Selected speed is highlighted blue; inactive speeds keep the normal dark HUD color.
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
    [Tooltip("Color used by the currently selected speed button.")]
    public Color selectedColor = new Color(0.08f, 0.67f, 0.88f, 1f);
    [Tooltip("Color used by speed buttons that are not selected.")]
    public Color normalColor = new Color(0.055f, 0.105f, 0.145f, 1f);
    [Tooltip("How much an unselected speed button brightens while hovered.")]
    [Range(1f, 1.5f)] public float hoverBrightness = 1.18f;

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

        ConfigureButtonColors(speed1Button);
        ConfigureButtonColors(speed2Button);
        ConfigureButtonColors(speed3Button);

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

    private void ConfigureButtonColors(Button button)
    {
        if (button == null) return;

        // Keep Unity's Button transition from flashing back to its default white tint.
        ColorBlock colors = button.colors;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private void RefreshButton(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null) return;

        Color baseColor = selected ? selectedColor : normalColor;
        button.targetGraphic.color = baseColor;

        // Color Tint multiplies the target graphic color. Keep states near white so the explicit
        // selected/normal base color remains visible instead of getting replaced by Unity's white.
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.selectedColor = Color.white;
        colors.highlightedColor = selected ? Color.white : Brighten(Color.white, hoverBrightness);
        colors.pressedColor = new Color(0.84f, 0.90f, 0.96f, 1f);
        colors.disabledColor = new Color(0.55f, 0.58f, 0.62f, 0.7f);
        button.colors = colors;
    }

    private static Color Brighten(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a);
    }
}
