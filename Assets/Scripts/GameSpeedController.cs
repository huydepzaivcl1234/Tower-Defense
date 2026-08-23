using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tower-defense game speed control. Changes Time.timeScale between 1x/2x/3x without touching
/// gameplay stats. Game over/win can still set Time.timeScale to 0 through GameManager.
/// Selected speed is highlighted blue; inactive speeds always use the dark HUD color.
/// </summary>
public class GameSpeedController : MonoBehaviour
{
    public static GameSpeedController Instance { get; private set; }

    [Header("Buttons")]
    public Button speed1Button;
    public Button speed2Button;
    public Button speed3Button;
    public TMP_Text currentSpeedText;

    // Intentionally not serialized: old scene instances had Color.white saved in the component,
    // so changing inspector defaults did not update them. Keeping these runtime theme colors fixed
    // guarantees the speed selector always renders correctly on existing scenes.
    private static readonly Color SelectedThemeColor = new Color(0.08f, 0.67f, 0.88f, 1f);
    private static readonly Color NormalThemeColor = new Color(0.055f, 0.105f, 0.145f, 1f);

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

        ConfigureButton(speed1Button);
        ConfigureButton(speed2Button);
        ConfigureButton(speed3Button);

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

    private void ConfigureButton(Button button)
    {
        if (button == null) return;

        // UIPunchButton owns hover/press feedback. Unity Color Tint must not overwrite our theme.
        button.transition = Selectable.Transition.None;
    }

    private void RefreshButton(Button button, bool selected)
    {
        if (button == null || button.targetGraphic == null) return;

        Color baseColor = selected ? SelectedThemeColor : NormalThemeColor;
        button.targetGraphic.color = baseColor;

        UIPunchButton feedback = button.GetComponent<UIPunchButton>();
        if (feedback != null)
            feedback.SetBaseGraphicColor(baseColor);
    }
}
