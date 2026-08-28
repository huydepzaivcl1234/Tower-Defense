using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Optional HUD binding for the persistent Diamond currency.</summary>
[DisallowMultipleComponent]
public class DiamondHUD : MonoBehaviour
{
    [Header("References")]
    public TMP_Text valueText;
    public Image iconImage;

    [Header("Presentation")]
    [Tooltip("Optional override Sprite. If empty, a Sprite assigned directly to Icon Image is preserved and used.")]
    public Sprite diamondIcon;
    public bool useCompactNumbers = true;
    public string prefix = "";
    public string suffix = "";
    public string zeroText = "0";
    public bool hideIconWhenNoSprite = true;

    [Header("Editor / Fallback")]
    [Min(0)] public int previewValue = 125;

    private void OnEnable()
    {
        PlayerProfileManager.OnDiamondsChanged += HandleDiamondsChanged;
        Refresh();
    }

    private void OnDisable()
    {
        PlayerProfileManager.OnDiamondsChanged -= HandleDiamondsChanged;
    }

    private void Start() => Refresh();

    public void Refresh()
    {
        int value = PlayerProfileManager.Instance != null
            ? PlayerProfileManager.Instance.CurrentDiamonds
            : 0;
        HandleDiamondsChanged(value);
        RefreshIcon();
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        // Inspector-authored Image.sprite is valid presentation data and must never be erased just
        // because the optional DiamondHUD override field is empty.
        Sprite resolved = diamondIcon != null ? diamondIcon : iconImage.sprite;
        if (diamondIcon != null && iconImage.sprite != diamondIcon)
            iconImage.sprite = diamondIcon;

        iconImage.preserveAspect = true;
        iconImage.enabled = resolved != null || !hideIconWhenNoSprite;
    }

    private void HandleDiamondsChanged(int value)
    {
        if (valueText == null) return;

        string formatted;
        if (value <= 0)
            formatted = string.IsNullOrEmpty(zeroText) ? "0" : zeroText;
        else if (useCompactNumbers)
            formatted = CompactNumber.Format(value);
        else
            formatted = value.ToString("N0");

        valueText.text = prefix + formatted + suffix;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshIcon();

        if (!Application.isPlaying && valueText != null)
        {
            string formatted = previewValue <= 0
                ? (string.IsNullOrEmpty(zeroText) ? "0" : zeroText)
                : (useCompactNumbers ? CompactNumber.Format(previewValue) : previewValue.ToString("N0"));
            valueText.text = prefix + formatted + suffix;
        }
    }
#endif
}
