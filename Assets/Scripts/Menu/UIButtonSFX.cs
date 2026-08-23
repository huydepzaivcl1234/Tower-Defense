using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Per-button UI sound feedback. Click sounds are routed through an always-active
/// global one-shot player so they keep playing even if the clicked menu is disabled
/// in the same frame. Hover keeps a local AudioSource so pointer exit can stop it
/// immediately. All sounds are categorized as SFX and follow AudioSettingsManager.
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
    private AudioSource hoverSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        EnsureHoverSource();
    }

    private void Start()
    {
        AudioSettingsManager.Instance?.ApplyAll();
    }

    private void OnDisable()
    {
        StopHover();
    }

    private void EnsureHoverSource()
    {
        if (hoverSource != null) return;

        AudioSource[] sources = GetComponents<AudioSource>();
        hoverSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();

        hoverSource.playOnAwake = false;
        hoverSource.loop = false;
        hoverSource.spatialBlend = 0f;

        GameAudioCategory category = hoverSource.GetComponent<GameAudioCategory>();
        if (category == null)
            category = hoverSource.gameObject.AddComponent<GameAudioCategory>();
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

        float finalPitch = Mathf.Clamp(
            pitch + Random.Range(-randomPitchRange, randomPitchRange),
            0.25f,
            3f);

        UIAudioOneShotPlayer.Play(clickClip, Mathf.Clamp01(volume), finalPitch);
    }

    public void PlayHover()
    {
        if (!enableHoverSound || hoverClip == null) return;
        if (!CanPlay()) return;
        if (hoverSource == null) EnsureHoverSource();
        if (hoverSource == null || !hoverSource.isActiveAndEnabled) return;

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

        hoverSource.Stop();
        hoverSource.clip = null;
        hoverSource.loop = false;
    }

    private bool CanPlay()
    {
        return playWhenDisabled || button == null || button.interactable;
    }
}
