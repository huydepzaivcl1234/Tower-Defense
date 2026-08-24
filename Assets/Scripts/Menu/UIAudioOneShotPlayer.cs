using UnityEngine;

/// <summary>
/// Always-active UI one-shot SFX player. Button click sounds are routed here so
/// they can continue playing even when the clicked button or its menu is disabled
/// in the same frame. Uses a small voice pool so simultaneous UI sounds can have
/// independent pitch values.
/// </summary>
public sealed class UIAudioOneShotPlayer : MonoBehaviour
{
    private const int VoiceCount = 8;
    private static UIAudioOneShotPlayer instance;

    private AudioSource[] voices;
    private int nextVoice;

    private static UIAudioOneShotPlayer Instance
    {
        get
        {
            if (instance != null) return instance;

            GameObject go = new GameObject("UIAudioOneShotPlayer");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<UIAudioOneShotPlayer>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildVoices();
    }

    private void BuildVoices()
    {
        if (voices != null && voices.Length == VoiceCount) return;

        GameAudioCategory category = GetComponent<GameAudioCategory>();
        if (category == null)
            category = gameObject.AddComponent<GameAudioCategory>();
        category.type = GameAudioType.SFX;

        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < voices.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            voices[i] = source;
        }

        AudioSettingsManager.Instance?.ApplyAll();
    }

    public static void Play(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;

        UIAudioOneShotPlayer player = Instance;
        if (player.voices == null || player.voices.Length == 0)
            player.BuildVoices();

        AudioSource source = player.voices[player.nextVoice];
        player.nextVoice = (player.nextVoice + 1) % player.voices.Length;

        // Reusing this voice intentionally replaces an older UI sound only if all
        // voices are already busy, preventing unbounded AudioSource creation.
        source.Stop();
        source.clip = null;
        source.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
