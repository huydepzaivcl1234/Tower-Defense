using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Per-button UI sound feedback. Each button can use its own click AudioClip,
/// volume and pitch settings. The generated AudioSource is categorized as SFX,
/// so AudioSettingsManager's SFX slider controls it automatically.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSFX : MonoBehaviour, IPointerClickHandler
{
    [Header("Click SFX")]
    public AudioClip clickClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.25f, 3f)] public float pitch = 1f;

    [Header("Optional Variation")]
    [Tooltip("Random +/- pitch variation applied per click. 0 disables random pitch.")]
    [Range(0f, 0.5f)] public float randomPitchRange = 0f;
    [Tooltip("Allow click sound even if the Button is currently not interactable.")]
    public bool playWhenDisabled = false;

    [Header("Optional Hover SFX")]
    public bool enableHoverSound = false;
    public AudioClip hoverClip;
    [Range(0f, 1f)] public float hoverVolume = 0.55f;

    private Button button;
    private AudioSource source;

    private void Awake()
    {
        button = GetComponent<Button>();
        EnsureAudioSource();
    }

    private void Start()
    {
        // Register immediately with the current SFX volume instead of waiting for
        // AudioSettingsManager's periodic source scan.
        AudioSettingsManager.Instance?.ApplyAll();
    }

    private void EnsureAudioSource()
    {
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;

        GameAudioCategory category = source.GetComponent<GameAudioCategory>();
        if (category == null)
            category = source.gameObject.AddComponent<GameAudioCategory>();
        category.type = GameAudioType.SFX;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        PlayClick();
    }

    public void PlayClick()
    {
        if (clickClip == null) return;
        if (!playWhenDisabled && button != null && !button.interactable) return;
        if (source == null) EnsureAudioSource();

        source.pitch = Mathf.Clamp(pitch + Random.Range(-randomPitchRange, randomPitchRange), 0.25f, 3f);
        source.PlayOneShot(clickClip, Mathf.Clamp01(volume));
    }

    public void PlayHover()
    {
        if (!enableHoverSound || hoverClip == null) return;
        if (!playWhenDisabled && button != null && !button.interactable) return;
        if (source == null) EnsureAudioSource();

        source.pitch = 1f;
        source.PlayOneShot(hoverClip, Mathf.Clamp01(hoverVolume));
    }
}
