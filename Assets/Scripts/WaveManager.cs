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

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }
    public static event System.Action OnWaveCleared;

    [Header("Setup")]
    public WaypointPath path;
    public List<Wave> waves = new List<Wave>();

    [Header("Spawn Portal Animation")]
    [Tooltip("Root Transform of your existing Portal VFX. Leave empty to auto-find a GameObject named 'Portal'.")]
    public Transform spawnPortal;
    [Tooltip("Smooth zoom-in time before the first enemy spawns.")]
    [Min(0.05f)] public float portalOpenDuration = 0.65f;
    [Tooltip("Smooth zoom-out time after the final enemy has spawned.")]
    [Min(0.05f)] public float portalCloseDuration = 0.55f;

    [SerializeField] private int currentWaveIndex = -1;
    private int aliveEnemies = 0;
    private bool waveInProgress = false;

    private Vector3 portalBaseScale = Vector3.one;
    private ParticleSystem[] portalParticles;
    private bool portalReady;

    // Prevents the same wave from firing OnWaveCleared more than once.
    private int lastClearedWaveIndex = -1;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waves.Count;
    public bool IsWaveInProgress => waveInProgress;
    public bool AllWavesComplete => currentWaveIndex >= waves.Count - 1 && !waveInProgress && aliveEnemies == 0 && waves.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CacheSpawnPortal();
    }

    private void Start()
    {
        HidePortalInstant();
    }

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
            if (portalObject != null)
                spawnPortal = portalObject.transform;
        }

        if (spawnPortal == null)
        {
            portalReady = false;
            return;
        }

        portalBaseScale = spawnPortal.localScale;
        portalParticles = spawnPortal.GetComponentsInChildren<ParticleSystem>(true);
        portalReady = true;
    }

    private void HidePortalInstant()
    {
        if (!portalReady)
            CacheSpawnPortal();
        if (!portalReady)
            return;

        spawnPortal.localScale = Vector3.zero;
        ClearPortalParticles();
        spawnPortal.gameObject.SetActive(false);
    }

    /// <summary>
    /// Smooth 0..1 easing with zero velocity at both ends.
    /// This avoids the hard "pop" of a linear scale or overshoot curve.
    /// </summary>
    private static float SmootherStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    private IEnumerator OpenPortal()
    {
        if (!portalReady)
            CacheSpawnPortal();
        if (!portalReady)
            yield break;

        // Activate while still exactly scale-zero so there is no visible one-frame pop.
        spawnPortal.localScale = Vector3.zero;
        spawnPortal.gameObject.SetActive(true);

        // Give Unity one frame to initialize renderers/particles while the root is invisible.
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
        if (!portalReady)
            yield break;

        // Stop creating NEW particles, but do not clear existing ones yet.
        // Existing visual layers remain visible while the whole portal smoothly shrinks.
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

        // Only after the zoom-out visually reaches zero do we clear and disable it.
        spawnPortal.localScale = Vector3.zero;
        ClearPortalParticles();
        spawnPortal.gameObject.SetActive(false);
    }

    private void PlayPortalParticles()
    {
        if (portalParticles == null)
            return;

        foreach (ParticleSystem ps in portalParticles)
        {
            if (ps == null) continue;

            // Hierarchy scaling makes authored particle sizes follow the root portal zoom.
            ParticleSystem.MainModule main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    private void StopPortalEmission()
    {
        if (portalParticles == null)
            return;

        foreach (ParticleSystem ps in portalParticles)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void ClearPortalParticles()
    {
        if (portalParticles == null)
            return;

        foreach (ParticleSystem ps in portalParticles)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
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
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Count) return;
        if (lastClearedWaveIndex == currentWaveIndex) return;

        lastClearedWaveIndex = currentWaveIndex;
        OnWaveCleared?.Invoke();
    }

    public bool CanStartNextWave()
    {
        bool relicChoiceOpen = RelicManager.Instance != null && RelicManager.Instance.IsChoosing;
        return !waveInProgress && aliveEnemies == 0 && !relicChoiceOpen && currentWaveIndex < waves.Count - 1;
    }

    public void StartNextWave()
    {
        if (!CanStartNextWave()) return;
        currentWaveIndex++;
        StartCoroutine(SpawnWave(waves[currentWaveIndex]));
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        waveInProgress = true;
        yield return new WaitForSeconds(wave.startDelay);

        // Portal smoothly opens completely before the first enemy appears.
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

        // Final enemy is now out of the portal; smoothly close it.
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
