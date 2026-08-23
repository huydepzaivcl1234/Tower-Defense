using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Per-button UI sound feedback. Click and hover use separate AudioSources so
/// leaving a button can stop only its hover sound without cutting off click SFX.
/// Both sources are categorized as SFX and follow AudioSettingsManager.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSFX : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Click SFX")]
    public AudioClip clickClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.25f, 3f)] public float pitch = 1f;

    [Header("Optional Variation")]
    [Tooltip("Random +/- pitch variation applied per click. 0 disables random pitch.")]
    [Range(0f, 0.5f)] public float randomPitchRange = 0f;
    [Tooltip("Allow sounds even if the Button is currently not interactable.")]
    public bool playWhenDisabled = false;

    [Header("Hover SFX")]
    [Tooltip("Play Hover Clip only while the pointer is over this button.")]
    public bool enableHoverSound = false;
    public AudioClip hoverClip;
    [Range(0f, 1f)] public float hoverVolume = 0.55f;
    [Range(0.25f, 3f)] public float hoverPitch = 1f;
    [Tooltip("If enabled, the hover clip loops for as long as the mouse stays over the button. Pointer exit always stops it immediately.")]
    public bool loopHoverWhileInside = false;

    private Button button;
    private AudioSource clickSource;
    private AudioSource hoverSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        EnsureAudioSources();
    }

    private void Start()
    {
        // Apply the currently saved SFX volume immediately instead of waiting for
        // AudioSettingsManager's periodic source scan.
        AudioSettingsManager.Instance?.ApplyAll();
    }

    private void OnDisable()
    {
        StopHover();
    }

    private void EnsureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length > 0)
            clickSource = sources[0];
        else
            clickSource = gameObject.AddComponent<AudioSource>();

        if (sources.Length > 1)
            hoverSource = sources[1];
        else
            hoverSource = gameObject.AddComponent<AudioSource>();

        ConfigureSource(clickSource, false);
        ConfigureSource(hoverSource, false);
    }

    private static void ConfigureSource(AudioSource audioSource, bool loop)
    {
        if (audioSource == null) return;

        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.spatialBlend = 0f;

        GameAudioCategory category = audioSource.GetComponent<GameAudioCategory>();
        if (category == null)
            category = audioSource.gameObject.AddComponent<GameAudioCategory>();
        category.type = GameAudioType.SFX;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        PlayClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHover();
    }

    public void PlayClick()
    {
        if (clickClip == null) return;
        if (!CanPlay()) return;
        if (clickSource == null) EnsureAudioSources();

        clickSource.pitch = Mathf.Clamp(
            pitch + Random.Range(-randomPitchRange, randomPitchRange),
            0.25f,
            3f);
        clickSource.PlayOneShot(clickClip, Mathf.Clamp01(volume));
    }

    public void PlayHover()
    {
        if (!enableHoverSound || hoverClip == null) return;
        if (!CanPlay()) return;
        if (hoverSource == null) EnsureAudioSources();

        // Re-entering a button restarts the hover cue from the beginning rather
        // than stacking multiple copies of the same sound.
        hoverSource.Stop();
        hoverSource.clip = hoverClip;
        hoverSource.pitch = Mathf.Clamp(hoverPitch, 0.25f, 3f);
        hoverSource.loop = loopHoverWhileInside;
        hoverSource.volume = Mathf.Clamp01(hoverVolume);
        hoverSource.Play();
    }

    public void StopHover()
    {
        if (hoverSource == null) return;

        // Pointer exit must silence the hover cue immediately.
        hoverSource.Stop();
        hoverSource.clip = null;
        hoverSource.loop = false;
    }

    private bool CanPlay()
    {
        return playWhenDisabled || button == null || button.interactable;
    }
}
