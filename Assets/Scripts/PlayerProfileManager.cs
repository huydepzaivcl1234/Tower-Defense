using System;
using UnityEngine;

/// <summary>
/// Persistent meta-progression owner. Diamonds live here because they survive run restarts
/// and are intentionally separate from GameManager's per-run Gold/Lives.
/// </summary>
[DisallowMultipleComponent]
public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance { get; private set; }

    [Header("Persistence")]
    [Tooltip("PlayerPrefs key containing the JSON profile payload.")]
    public string saveKey = "TowerDefense.PlayerProfile";
    [Min(1)] public int saveVersion = 1;
    public bool dontDestroyOnLoad = true;
    public bool autoSaveOnDiamondChange = true;
    public bool saveOnApplicationPause = true;
    public bool saveOnApplicationQuit = true;

    [Header("Diamond Currency")]
    [Min(0)] public int startingDiamonds = 0;
    [Min(0)] public int maxDiamonds = 999999999;

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentDiamonds;
    [SerializeField] private bool loaded;

    private PlayerProfileData data;

    public int CurrentDiamonds => currentDiamonds;
    public PlayerProfileData Data => data;
    public bool IsLoaded => loaded;

    public static event Action<int> OnDiamondsChanged;
    public static event Action OnProfileLoaded;
    public static event Action OnProfileSaved;
    public static event Action OnProfileReset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        Load();
    }

    private void Start()
    {
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    public void Load()
    {
        string key = string.IsNullOrWhiteSpace(saveKey) ? "TowerDefense.PlayerProfile" : saveKey;
        data = null;

        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    data = JsonUtility.FromJson<PlayerProfileData>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"PlayerProfileManager could not parse save data. A new profile will be created. {ex.Message}", this);
                }
            }
        }

        if (data == null)
        {
            data = new PlayerProfileData
            {
                saveVersion = Mathf.Max(1, saveVersion),
                diamonds = Mathf.Clamp(startingDiamonds, 0, Mathf.Max(0, maxDiamonds))
            };
        }

        data.saveVersion = Mathf.Max(1, saveVersion);
        data.Sanitize(Mathf.Max(0, maxDiamonds));
        currentDiamonds = data.diamonds;
        loaded = true;

        OnProfileLoaded?.Invoke();
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    public void Save()
    {
        EnsureData();
        data.saveVersion = Mathf.Max(1, saveVersion);
        data.diamonds = Mathf.Clamp(currentDiamonds, 0, Mathf.Max(0, maxDiamonds));
        data.Sanitize(Mathf.Max(0, maxDiamonds));

        string key = string.IsNullOrWhiteSpace(saveKey) ? "TowerDefense.PlayerProfile" : saveKey;
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        OnProfileSaved?.Invoke();
    }

    public int AddDiamonds(int amount)
    {
        if (amount <= 0)
            return 0;

        int oldValue = currentDiamonds;
        long wanted = (long)currentDiamonds + amount;
        currentDiamonds = Mathf.Clamp((int)Mathf.Min(wanted, int.MaxValue), 0, Mathf.Max(0, maxDiamonds));
        int granted = currentDiamonds - oldValue;

        if (granted != 0)
        {
            SyncDiamondData();
            OnDiamondsChanged?.Invoke(currentDiamonds);
            if (autoSaveOnDiamondChange) Save();
        }

        return granted;
    }

    public bool SpendDiamonds(int amount)
    {
        if (amount <= 0)
            return true;
        if (currentDiamonds < amount)
            return false;

        currentDiamonds -= amount;
        SyncDiamondData();
        OnDiamondsChanged?.Invoke(currentDiamonds);
        if (autoSaveOnDiamondChange) Save();
        return true;
    }

    public void SetDiamonds(int amount, bool saveImmediately = true)
    {
        int clamped = Mathf.Clamp(amount, 0, Mathf.Max(0, maxDiamonds));
        if (clamped == currentDiamonds)
            return;

        currentDiamonds = clamped;
        SyncDiamondData();
        OnDiamondsChanged?.Invoke(currentDiamonds);
        if (saveImmediately) Save();
    }

    /// <summary>Clears this profile save only. Audio/settings are deliberately not touched here.</summary>
    public void ResetProfileData(bool saveFreshProfile = true)
    {
        string key = string.IsNullOrWhiteSpace(saveKey) ? "TowerDefense.PlayerProfile" : saveKey;
        PlayerPrefs.DeleteKey(key);

        data = new PlayerProfileData
        {
            saveVersion = Mathf.Max(1, saveVersion),
            diamonds = Mathf.Clamp(startingDiamonds, 0, Mathf.Max(0, maxDiamonds))
        };
        data.Sanitize(Mathf.Max(0, maxDiamonds));
        currentDiamonds = data.diamonds;
        loaded = true;

        if (saveFreshProfile)
            Save();
        else
            PlayerPrefs.Save();

        OnDiamondsChanged?.Invoke(currentDiamonds);
        OnProfileReset?.Invoke();
    }

    private void EnsureData()
    {
        if (data != null)
            return;

        data = new PlayerProfileData
        {
            saveVersion = Mathf.Max(1, saveVersion),
            diamonds = currentDiamonds
        };
    }

    private void SyncDiamondData()
    {
        EnsureData();
        data.diamonds = currentDiamonds;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && saveOnApplicationPause && loaded)
            Save();
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationQuit && loaded)
            Save();
    }
}
