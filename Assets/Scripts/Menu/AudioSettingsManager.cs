using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Run-wide audio settings without requiring an AudioMixer migration.
/// Master uses AudioListener.volume. Music/SFX scale individual AudioSource volumes.
/// Values are persisted with PlayerPrefs.
/// </summary>
public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance { get; private set; }

    private const string MasterKey = "Audio.Master";
    private const string MusicKey = "Audio.Music";
    private const string SfxKey = "Audio.SFX";

    [Header("Defaults")]
    [Range(0f, 1f)] public float defaultMaster = 1f;
    [Range(0f, 1f)] public float defaultMusic = 0.8f;
    [Range(0f, 1f)] public float defaultSfx = 1f;

    [Header("Runtime")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Tooltip("How often new AudioSources are discovered, using unscaled time.")]
    [Min(0.1f)] public float rescanInterval = 1f;

    private readonly Dictionary<AudioSource, float> baseVolumes = new Dictionary<AudioSource, float>();
    private float nextScanTime;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        masterVolume = PlayerPrefs.GetFloat(MasterKey, defaultMaster);
        musicVolume = PlayerPrefs.GetFloat(MusicKey, defaultMusic);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, defaultSfx);
        ApplyAll();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime) return;
        nextScanTime = Time.unscaledTime + Mathf.Max(0.1f, rescanInterval);
        RefreshSources();
    }

    public void SetMaster(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterKey, masterVolume);
        PlayerPrefs.Save();
        AudioListener.volume = masterVolume;
    }

    public void SetMusic(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, musicVolume);
        PlayerPrefs.Save();
        RefreshSources();
    }

    public void SetSfx(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, sfxVolume);
        PlayerPrefs.Save();
        RefreshSources();
    }

    public void ApplyAll()
    {
        AudioListener.volume = masterVolume;
        RefreshSources();
    }

    private void RefreshSources()
    {
        AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
        HashSet<AudioSource> alive = new HashSet<AudioSource>();

        foreach (AudioSource source in sources)
        {
            if (source == null) continue;
            alive.Add(source);

            if (!baseVolumes.ContainsKey(source))
                baseVolumes[source] = source.volume;

            float categoryMultiplier = GetCategory(source) == GameAudioType.Music ? musicVolume : sfxVolume;
            source.volume = baseVolumes[source] * categoryMultiplier;
        }

        List<AudioSource> stale = null;
        foreach (var pair in baseVolumes)
        {
            if (pair.Key != null && alive.Contains(pair.Key)) continue;
            if (stale == null) stale = new List<AudioSource>();
            stale.Add(pair.Key);
        }
        if (stale != null)
        {
            foreach (AudioSource source in stale)
                baseVolumes.Remove(source);
        }
    }

    private static GameAudioType GetCategory(AudioSource source)
    {
        GameAudioCategory explicitCategory = source.GetComponent<GameAudioCategory>();
        if (explicitCategory != null) return explicitCategory.type;
        return source.loop ? GameAudioType.Music : GameAudioType.SFX;
    }
}
