using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>One group of enemies within a wave, e.g. "6x Grunt, 0.8s apart".</summary>
[System.Serializable]
public class WaveEntry
{
    public EnemyData enemyData;
    [Min(1)] public int count = 5;
    [Tooltip("Seconds between each spawn within this entry")]
    public float spawnInterval = 0.8f;
}

/// <summary>A full wave, made of one or more WaveEntries so you can mix enemy types.</summary>
[System.Serializable]
public class Wave
{
    public string waveName = "Wave";
    public List<WaveEntry> entries = new List<WaveEntry>();
    [Tooltip("Delay after the wave is triggered before spawning starts")]
    public float startDelay = 1f;
}

public enum TowerDefenseGameMode
{
    Story,
    Endless
}

/// <summary>One extra Story level. The existing Waves list remains Story Level 1.</summary>
[System.Serializable]
public class StoryLevel
{
    public string levelName = "Story Level";
    public List<Wave> waves = new List<Wave>();
}

/// <summary>Configures how one enemy type scales and unlocks in Endless mode.</summary>
[System.Serializable]
public class EndlessEnemyRule
{
    public EnemyData enemyData;
    [Min(1)] public int unlockAtWave = 1;
    [Min(0)] public int startingCount = 3;
    [Min(0)] public int additionalCountPerWave = 1;
    [Min(1)] public int maximumCount = 100;
    [Min(0f)] public float spawnInterval = 0.8f;
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    public static event System.Action OnWaveCleared;

    [Header("Setup")]
    public WaypointPath path;
    public List<Wave> waves = new List<Wave>();

    [Header("Story Mode")]
    [Tooltip("The existing Waves list above is Story Level 1. Add further levels here.")]
    public List<StoryLevel> additionalStoryLevels = new List<StoryLevel>();

    [Header("Endless Mode")]
    [Tooltip("Enemy types, unlock waves and count scaling used only by Endless mode.")]
    public List<EndlessEnemyRule> endlessEnemyRules = new List<EndlessEnemyRule>();
    [Tooltip("When no Endless rules are configured, reuse enemy types from Story Level 1 with safe count-only scaling.")]
    public bool autoSeedEndlessRulesFromStory = true;
    [Min(1)] public int endlessMaximumEnemiesPerWave = 300;
    [Min(0f)] public float endlessWaveStartDelay = 1f;
    [Tooltip("Beta victory milestone. Set 0 later for true Endless with no Win.")]
    [Min(0)] public int endlessVictoryWave = 50;

    [Header("Spawn Portal Animation")]
    [Tooltip("Root Transform of your existing Portal VFX. Leave empty to auto-find a GameObject named 'Portal'.")]
    public Transform spawnPortal;
    [Min(0.05f)] public float portalOpenDuration = 0.65f;
    [Min(0.05f)] public float portalCloseDuration = 0.55f;

    [SerializeField] private int currentWaveIndex = -1;
    private int aliveEnemies = 0;
    private bool waveInProgress = false;

    private Vector3 portalBaseScale = Vector3.one;
    private ParticleSystem[] portalParticles;
    private bool portalReady;
    private int lastClearedWaveIndex = -1;

    private TowerDefenseGameMode activeGameMode = TowerDefenseGameMode.Story;
    private int activeStoryLevelIndex;

    private static TowerDefenseGameMode selectedGameMode = TowerDefenseGameMode.Story;
    private static int selectedStoryLevelIndex;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => activeGameMode == TowerDefenseGameMode.Endless
        ? (endlessVictoryWave > 0 ? endlessVictoryWave : int.MaxValue)
        : GetActiveStoryWaves().Count;
    public bool IsWaveInProgress => waveInProgress;
    public bool IsEndlessMode => activeGameMode == TowerDefenseGameMode.Endless;
    public TowerDefenseGameMode ActiveGameMode => activeGameMode;
    public int ActiveStoryLevelIndex => activeStoryLevelIndex;
    public int StoryLevelCount => 1 + (additionalStoryLevels != null ? additionalStoryLevels.Count : 0);
    public bool HasEndlessConfiguration => HasEndlessEnemyForWave(1);
    public bool AllWavesComplete
    {
        get
        {
            if (waveInProgress || aliveEnemies > 0)
                return false;

            if (IsEndlessMode)
                return endlessVictoryWave > 0 && currentWaveIndex >= endlessVictoryWave - 1;

            List<Wave> activeWaves = GetActiveStoryWaves();
            return activeWaves.Count > 0 && currentWaveIndex >= activeWaves.Count - 1;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SeedEndlessRulesFromStoryIfEmpty();
        ApplySelectedMode();
        CacheSpawnPortal();
    }

    private void Start() => HidePortalInstant();

    private void OnEnable()
    {
        Enemy.OnAnyEnemyDied += HandleEnemyRemoved;
        Enemy.OnAnyEnemyReachedEnd += HandleEnemyRemoved;
    }

    private void OnDisable()
    {
        Enemy.OnAnyEnemyDied -= HandleEnemyRemoved;
        Enemy.OnAnyEnemyReachedEnd -= HandleEnemyRemoved;
    }

    private void CacheSpawnPortal()
    {
        if (spawnPortal == null)
        {
            GameObject portalObject = GameObject.Find("Portal");
            if (portalObject != null) spawnPortal = portalObject.transform;
        }
        if (spawnPortal == null) { portalReady = false; return; }
        portalBaseScale = spawnPortal.localScale;
        portalParticles = spawnPortal.GetComponentsInChildren<ParticleSystem>(true);
        portalReady = true;
    }

    private void HidePortalInstant()
    {
        if (!portalReady) CacheSpawnPortal();
        if (!portalReady) return;
        spawnPortal.localScale = Vector3.zero;
        ClearPortalParticles();
        spawnPortal.gameObject.SetActive(false);
    }

    private static float SmootherStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private IEnumerator OpenPortal()
    {
        if (!portalReady) CacheSpawnPortal();
        if (!portalReady) yield break;
        spawnPortal.localScale = Vector3.zero;
        spawnPortal.gameObject.SetActive(true);
        yield return null;
        PlayPortalParticles();
        float duration = Mathf.Max(0.05f, portalOpenDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = SmootherStep01(elapsed / duration);
            spawnPortal.localScale = portalBaseScale * t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        spawnPortal.localScale = portalBaseScale;
    }

    private IEnumerator ClosePortal()
    {
        if (!portalReady) yield break;
        StopPortalEmission();
        float duration = Mathf.Max(0.05f, portalCloseDuration);
        Vector3 startScale = spawnPortal.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = SmootherStep01(elapsed / duration);
            spawnPortal.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        spawnPortal.localScale = Vector3.zero;
        ClearPortalParticles();
        spawnPortal.gameObject.SetActive(false);
    }

    private void PlayPortalParticles()
    {
        if (portalParticles == null) return;
        foreach (ParticleSystem ps in portalParticles)
        {
            if (ps == null) continue;
            ParticleSystem.MainModule main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    private void StopPortalEmission()
    {
        if (portalParticles == null) return;
        foreach (ParticleSystem ps in portalParticles)
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void ClearPortalParticles()
    {
        if (portalParticles == null) return;
        foreach (ParticleSystem ps in portalParticles)
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void HandleEnemyRemoved(Enemy e)
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        TryCompleteCurrentWave();
        CheckForWin();
    }

    private void TryCompleteCurrentWave()
    {
        if (waveInProgress) return;
        if (aliveEnemies > 0) return;
        if (currentWaveIndex < 0) return;
        if (!IsEndlessMode && currentWaveIndex >= GetActiveStoryWaves().Count) return;
        if (lastClearedWaveIndex == currentWaveIndex) return;
        lastClearedWaveIndex = currentWaveIndex;
        OnWaveCleared?.Invoke();
    }

    public bool CanStartNextWave()
    {
        bool relicChoiceOpen = RelicManager.Instance != null && RelicManager.Instance.IsChoosing;
        if (waveInProgress || aliveEnemies > 0 || relicChoiceOpen ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver))
            return false;

        if (IsEndlessMode)
        {
            if (endlessVictoryWave > 0 && currentWaveIndex >= endlessVictoryWave - 1)
                return false;
            return HasEndlessEnemyForWave(currentWaveIndex + 2);
        }

        return currentWaveIndex < GetActiveStoryWaves().Count - 1;
    }

    public void StartNextWave()
    {
        if (!CanStartNextWave()) return;
        currentWaveIndex++;
        Wave wave = IsEndlessMode
            ? BuildEndlessWave(currentWaveIndex + 1)
            : GetActiveStoryWaves()[currentWaveIndex];
        StartCoroutine(SpawnWave(wave));
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        waveInProgress = true;

        // World event roll and presentation always happens BEFORE the portal opens/enemies spawn.
        if (WorldEventManager.Instance != null)
            yield return WorldEventManager.Instance.PrepareForWave(currentWaveIndex + 1);

        yield return new WaitForSeconds(wave.startDelay);
        yield return OpenPortal();

        foreach (var entry in wave.entries)
        {
            if (entry == null) continue;
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyData);
                if (i < entry.count - 1 && entry.spawnInterval > 0f)
                    yield return new WaitForSeconds(entry.spawnInterval);
            }
        }

        yield return ClosePortal();
        waveInProgress = false;
        TryCompleteCurrentWave();
        CheckForWin();
    }

    private void CheckForWin()
    {
        if (AllWavesComplete && GameManager.Instance != null)
            GameManager.Instance.HandleAllWavesCleared();
    }

    public string GetStoryLevelName(int index)
    {
        if (index <= 0)
            return "STORY LEVEL 1";

        int additionalIndex = index - 1;
        if (additionalStoryLevels == null || additionalIndex >= additionalStoryLevels.Count)
            return $"STORY LEVEL {index + 1}";

        string configuredName = additionalStoryLevels[additionalIndex]?.levelName;
        return string.IsNullOrWhiteSpace(configuredName) ? $"STORY LEVEL {index + 1}" : configuredName;
    }

    public bool ConfigureStoryMode(int storyLevelIndex)
    {
        if (!CanConfigureMode())
            return false;

        int safeIndex = Mathf.Clamp(storyLevelIndex, 0, Mathf.Max(0, StoryLevelCount - 1));
        List<Wave> configuredWaves = GetStoryWaves(safeIndex);
        if (configuredWaves == null || configuredWaves.Count == 0)
            return false;

        activeGameMode = TowerDefenseGameMode.Story;
        activeStoryLevelIndex = safeIndex;
        selectedGameMode = activeGameMode;
        selectedStoryLevelIndex = safeIndex;
        return true;
    }

    public bool ConfigureEndlessMode()
    {
        if (!CanConfigureMode() || !HasEndlessEnemyForWave(1))
            return false;

        activeGameMode = TowerDefenseGameMode.Endless;
        selectedGameMode = activeGameMode;
        return true;
    }

    public static void ResetRunSelection()
    {
        selectedGameMode = TowerDefenseGameMode.Story;
        selectedStoryLevelIndex = 0;
    }

    private void ApplySelectedMode()
    {
        activeGameMode = selectedGameMode;
        activeStoryLevelIndex = Mathf.Clamp(selectedStoryLevelIndex, 0, Mathf.Max(0, StoryLevelCount - 1));

        if (activeGameMode == TowerDefenseGameMode.Endless && !HasEndlessEnemyForWave(1))
        {
            activeGameMode = TowerDefenseGameMode.Story;
            selectedGameMode = activeGameMode;
        }
    }

    private bool CanConfigureMode()
    {
        return currentWaveIndex < 0 && !waveInProgress && aliveEnemies == 0;
    }

    private List<Wave> GetActiveStoryWaves()
    {
        return GetStoryWaves(activeStoryLevelIndex) ?? (waves = new List<Wave>());
    }

    private List<Wave> GetStoryWaves(int storyLevelIndex)
    {
        if (storyLevelIndex <= 0)
            return waves;

        int additionalIndex = storyLevelIndex - 1;
        if (additionalStoryLevels == null || additionalIndex >= additionalStoryLevels.Count || additionalStoryLevels[additionalIndex] == null)
            return null;

        StoryLevel level = additionalStoryLevels[additionalIndex];
        return level.waves;
    }

    private bool HasEndlessEnemyForWave(int waveNumber)
    {
        if (endlessEnemyRules == null)
            return false;

        for (int i = 0; i < endlessEnemyRules.Count; i++)
        {
            EndlessEnemyRule rule = endlessEnemyRules[i];
            if (rule != null && rule.enemyData != null && rule.startingCount > 0 && waveNumber >= Mathf.Max(1, rule.unlockAtWave))
                return true;
        }
        return false;
    }

    private void SeedEndlessRulesFromStoryIfEmpty()
    {
        if (!autoSeedEndlessRulesFromStory || (endlessEnemyRules != null && endlessEnemyRules.Count > 0) || waves == null)
            return;

        endlessEnemyRules = new List<EndlessEnemyRule>();
        HashSet<EnemyData> addedEnemies = new HashSet<EnemyData>();
        int enemyTypeIndex = 0;

        for (int waveIndex = 0; waveIndex < waves.Count; waveIndex++)
        {
            Wave storyWave = waves[waveIndex];
            if (storyWave?.entries == null)
                continue;

            for (int entryIndex = 0; entryIndex < storyWave.entries.Count; entryIndex++)
            {
                WaveEntry entry = storyWave.entries[entryIndex];
                if (entry?.enemyData == null || !addedEnemies.Add(entry.enemyData))
                    continue;

                endlessEnemyRules.Add(new EndlessEnemyRule
                {
                    enemyData = entry.enemyData,
                    unlockAtWave = 1 + enemyTypeIndex * 3,
                    startingCount = enemyTypeIndex == 0 ? 3 : 1,
                    additionalCountPerWave = 1,
                    maximumCount = 100,
                    spawnInterval = Mathf.Max(0f, entry.spawnInterval)
                });
                enemyTypeIndex++;
            }
        }
    }

    private Wave BuildEndlessWave(int waveNumber)
    {
        Wave wave = new Wave
        {
            waveName = $"Endless {waveNumber}",
            startDelay = Mathf.Max(0f, endlessWaveStartDelay)
        };

        if (endlessEnemyRules == null)
            return wave;

        int remaining = Mathf.Max(1, endlessMaximumEnemiesPerWave);
        for (int i = 0; i < endlessEnemyRules.Count && remaining > 0; i++)
        {
            EndlessEnemyRule rule = endlessEnemyRules[i];
            if (rule == null || rule.enemyData == null || waveNumber < Mathf.Max(1, rule.unlockAtWave))
                continue;

            int wavesSinceUnlock = waveNumber - Mathf.Max(1, rule.unlockAtWave);
            long scaledCount = (long)Mathf.Max(0, rule.startingCount) +
                               (long)Mathf.Max(0, rule.additionalCountPerWave) * wavesSinceUnlock;
            int count = (int)System.Math.Min(scaledCount, Mathf.Max(1, rule.maximumCount));
            count = Mathf.Min(count, remaining);
            if (count <= 0)
                continue;

            wave.entries.Add(new WaveEntry
            {
                enemyData = rule.enemyData,
                count = count,
                spawnInterval = Mathf.Max(0f, rule.spawnInterval)
            });
            remaining -= count;
        }

        return wave;
    }

    private void SpawnEnemy(EnemyData data)
    {
        if (data == null || data.enemyPrefab == null || path == null) return;
        Vector3 spawnPos = path.GetSpawnPosition();
        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(data.enemyPrefab, spawnPos, Quaternion.identity)
            : Instantiate(data.enemyPrefab, spawnPos, Quaternion.identity);

        Enemy e = go.GetComponent<Enemy>();
        if (e != null)
        {
            aliveEnemies++;
            e.Initialize(data, path.GetWaypoints());
        }
    }
}
