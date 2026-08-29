using System;
using UnityEngine;

/// <summary>
/// Persistent meta-progression/profile owner. Diamonds and lifetime profile stats survive run restarts;
/// per-run Gold/Lives stay in GameManager.
/// </summary>
[DisallowMultipleComponent]
public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance { get; private set; }

    [Header("Persistence")]
    [Tooltip("PlayerPrefs key containing the JSON profile payload.")]
    public string saveKey = "TowerDefense.PlayerProfile";
    [Min(1)] public int saveVersion = 2;
    public bool dontDestroyOnLoad = true;
    public bool autoSaveOnDiamondChange = true;
    public bool saveOnApplicationPause = true;
    public bool saveOnApplicationQuit = true;

    [Header("Player Identity")]
    public string defaultPlayerName = "Player";
    [Min(1)] public int maxPlayerNameLength = 24;

    [Header("Lifetime Tracking")]
    [Tooltip("Only accumulate play time while MainMenuController reports active gameplay and no menu is covering gameplay.")]
    public bool trackGameplayTime = true;
    [Tooltip("Count Enemy.OnAnyEnemyDied as lifetime kills while gameplay is active.")]
    public bool trackLifetimeKills = true;
    [Tooltip("How often accumulated play time is persisted. Avoids writing PlayerPrefs every frame.")]
    [Min(1f)] public float playTimeAutoSaveInterval = 30f;

    [Header("Diamond Currency")]
    [Min(0)] public int startingDiamonds = 0;
    [Min(0)] public int maxDiamonds = 999999999;

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentDiamonds;
    [SerializeField] private int diamondsEarnedThisRun;
    [SerializeField] private double totalPlaySeconds;
    [SerializeField] private long totalEnemiesKilled;
    [SerializeField] private bool loaded;

    private PlayerProfileData data;
    private float playTimeSinceLastSave;

    public int CurrentDiamonds => currentDiamonds;
    public int DiamondsEarnedThisRun => diamondsEarnedThisRun;
    public string PlayerName => data != null ? data.playerName : defaultPlayerName;
    public int AvatarIndex => data != null ? data.avatarIndex : 0;
    public double TotalPlaySeconds => totalPlaySeconds;
    public long TotalEnemiesKilled => totalEnemiesKilled;
    public PlayerProfileData Data => data;
    public bool IsLoaded => loaded;

    public static event Action<int> OnDiamondsChanged;
    public static event Action<int, int> OnDiamondsGranted;
    public static event Action OnProfileLoaded;
    public static event Action OnProfileSaved;
    public static event Action OnProfileReset;
    public static event Action OnProfileIdentityChanged;
    public static event Action OnProfileStatsChanged;

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

    private void OnEnable()
    {
        Enemy.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        Enemy.OnAnyEnemyDied -= HandleEnemyDied;
    }

    private void Start()
    {
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    private void Update()
    {
        if (!loaded || !trackGameplayTime || !ShouldTrackGameplayNow())
            return;

        float delta = Time.unscaledDeltaTime;
        if (delta <= 0f)
            return;

        totalPlaySeconds += delta;
        playTimeSinceLastSave += delta;
        SyncLifetimeData();

        if (playTimeSinceLastSave >= Mathf.Max(1f, playTimeAutoSaveInterval))
        {
            playTimeSinceLastSave = 0f;
            Save();
            OnProfileStatsChanged?.Invoke();
        }
    }

    public void BeginRun()
    {
        diamondsEarnedThisRun = 0;
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
            data = CreateFreshData();
        }

        data.saveVersion = Mathf.Max(1, saveVersion);
        if (string.IsNullOrWhiteSpace(data.playerName))
            data.playerName = string.IsNullOrWhiteSpace(defaultPlayerName) ? "Player" : defaultPlayerName.Trim();
        data.Sanitize(Mathf.Max(0, maxDiamonds), Mathf.Max(1, maxPlayerNameLength));

        currentDiamonds = data.diamonds;
        totalPlaySeconds = data.totalPlaySeconds;
        totalEnemiesKilled = data.totalEnemiesKilled;
        playTimeSinceLastSave = 0f;
        loaded = true;

        OnProfileLoaded?.Invoke();
        OnDiamondsChanged?.Invoke(currentDiamonds);
        OnProfileIdentityChanged?.Invoke();
        OnProfileStatsChanged?.Invoke();
    }

    public void Save()
    {
        EnsureData();
        data.saveVersion = Mathf.Max(1, saveVersion);
        data.diamonds = Mathf.Clamp(currentDiamonds, 0, Mathf.Max(0, maxDiamonds));
        SyncLifetimeData();
        data.Sanitize(Mathf.Max(0, maxDiamonds), Mathf.Max(1, maxPlayerNameLength));

        string key = string.IsNullOrWhiteSpace(saveKey) ? "TowerDefense.PlayerProfile" : saveKey;
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        OnProfileSaved?.Invoke();
    }

    public void SetPlayerName(string value, bool saveImmediately = true)
    {
        EnsureData();
        string fallback = string.IsNullOrWhiteSpace(defaultPlayerName) ? "Player" : defaultPlayerName.Trim();
        string sanitized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        int maxLength = Mathf.Max(1, maxPlayerNameLength);
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        if (data.playerName == sanitized)
            return;

        data.playerName = sanitized;
        OnProfileIdentityChanged?.Invoke();
        if (saveImmediately) Save();
    }

    public void SetAvatarIndex(int index, bool saveImmediately = true)
    {
        EnsureData();
        int sanitized = Mathf.Max(0, index);
        if (data.avatarIndex == sanitized)
            return;

        data.avatarIndex = sanitized;
        OnProfileIdentityChanged?.Invoke();
        if (saveImmediately) Save();
    }

    public int AddDiamonds(int amount, bool countAsRunEarning = true, bool notifyGain = true)
    {
        if (amount <= 0)
            return 0;

        int oldValue = currentDiamonds;
        long wanted = (long)currentDiamonds + amount;
        currentDiamonds = Mathf.Clamp((int)Mathf.Min(wanted, int.MaxValue), 0, Mathf.Max(0, maxDiamonds));
        int granted = currentDiamonds - oldValue;

        if (granted != 0)
        {
            if (countAsRunEarning)
                diamondsEarnedThisRun += granted;

            SyncDiamondData();
            OnDiamondsChanged?.Invoke(currentDiamonds);
            OnProfileStatsChanged?.Invoke();
            if (notifyGain)
                OnDiamondsGranted?.Invoke(granted, currentDiamonds);
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
        OnProfileStatsChanged?.Invoke();
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
        OnProfileStatsChanged?.Invoke();
        if (saveImmediately) Save();
    }

    public void ResetProfileData(bool saveFreshProfile = true)
    {
        string key = string.IsNullOrWhiteSpace(saveKey) ? "TowerDefense.PlayerProfile" : saveKey;
        PlayerPrefs.DeleteKey(key);

        data = CreateFreshData();
        data.Sanitize(Mathf.Max(0, maxDiamonds), Mathf.Max(1, maxPlayerNameLength));
        currentDiamonds = data.diamonds;
        totalPlaySeconds = data.totalPlaySeconds;
        totalEnemiesKilled = data.totalEnemiesKilled;
        diamondsEarnedThisRun = 0;
        playTimeSinceLastSave = 0f;
        loaded = true;

        if (saveFreshProfile)
            Save();
        else
            PlayerPrefs.Save();

        OnDiamondsChanged?.Invoke(currentDiamonds);
        OnProfileIdentityChanged?.Invoke();
        OnProfileStatsChanged?.Invoke();
        OnProfileReset?.Invoke();
    }

    private PlayerProfileData CreateFreshData()
    {
        return new PlayerProfileData
        {
            saveVersion = Mathf.Max(1, saveVersion),
            playerName = string.IsNullOrWhiteSpace(defaultPlayerName) ? "Player" : defaultPlayerName.Trim(),
            avatarIndex = 0,
            diamonds = Mathf.Clamp(startingDiamonds, 0, Mathf.Max(0, maxDiamonds)),
            totalPlaySeconds = 0d,
            totalEnemiesKilled = 0L
        };
    }

    private void EnsureData()
    {
        if (data != null)
            return;

        data = CreateFreshData();
        data.diamonds = currentDiamonds;
        data.totalPlaySeconds = totalPlaySeconds;
        data.totalEnemiesKilled = totalEnemiesKilled;
    }

    private void SyncDiamondData()
    {
        EnsureData();
        data.diamonds = currentDiamonds;
    }

    private void SyncLifetimeData()
    {
        EnsureData();
        data.totalPlaySeconds = Math.Max(0d, totalPlaySeconds);
        data.totalEnemiesKilled = Math.Max(0L, totalEnemiesKilled);
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (!loaded || !trackLifetimeKills || enemy == null || !ShouldTrackGameplayNow())
            return;

        totalEnemiesKilled++;
        SyncLifetimeData();
        OnProfileStatsChanged?.Invoke();
    }

    private static bool ShouldTrackGameplayNow()
    {
        MainMenuController menu = MainMenuController.Instance;
        if (menu == null)
            return true;

        return menu.GameplayStarted && !menu.IsAnyMenuVisible;
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
