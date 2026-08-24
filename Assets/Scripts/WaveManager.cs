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
    [Min(0f)] public float portalOpenDuration = 0.35f;
    [Min(0f)] public float portalCloseDuration = 0.28f;
    [Tooltip("Open animation. Default has a small overshoot so the portal pops into existence.")]
    public AnimationCurve portalOpenCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.72f, 1.08f),
        new Keyframe(1f, 1f));
    [Tooltip("Close animation from full size back to zero.")]
    public AnimationCurve portalCloseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private int currentWaveIndex = -1;
    private int aliveEnemies = 0;
    private bool waveInProgress = false;

    private Vector3 portalBaseScale = Vector3.one;
    private ParticleSystem[] portalParticles;
    private bool portalReady;

    // Prevents the same wave from firing OnWaveCleared more than once.
    // Important when the last enemy dies at the same time the spawn coroutine finishes.
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
        StopPortalParticles();
        spawnPortal.gameObject.SetActive(false);
    }

    private IEnumerator OpenPortal()
    {
        if (!portalReady)
            CacheSpawnPortal();
        if (!portalReady)
            yield break;

        spawnPortal.gameObject.SetActive(true);
        spawnPortal.localScale = Vector3.zero;
        PlayPortalParticles();

        if (portalOpenDuration <= 0f)
        {
            spawnPortal.localScale = portalBaseScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < portalOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / portalOpenDuration);
            float scaleFactor = portalOpenCurve != null ? portalOpenCurve.Evaluate(t) : t;
            spawnPortal.localScale = Vector3.LerpUnclamped(Vector3.zero, portalBaseScale, scaleFactor);
            yield return null;
        }

        spawnPortal.localScale = portalBaseScale;
    }

    private IEnumerator ClosePortal()
    {
        if (!portalReady)
            yield break;

        if (portalCloseDuration <= 0f)
        {
            HidePortalInstant();
            yield break;
        }

        Vector3 startScale = spawnPortal.localScale;
        float elapsed = 0f;
        while (elapsed < portalCloseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / portalCloseDuration);
            float curveValue = portalCloseCurve != null ? portalCloseCurve.Evaluate(t) : t;
            spawnPortal.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, curveValue);
            yield return null;
        }

        spawnPortal.localScale = Vector3.zero;
        StopPortalParticles();
        spawnPortal.gameObject.SetActive(false);
    }

    private void PlayPortalParticles()
    {
        if (portalParticles == null)
            return;

        foreach (ParticleSystem ps in portalParticles)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    private void StopPortalParticles()
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

    /// <summary>
    /// Completes the current wave only after every configured spawn has finished and no enemy remains.
    /// Safe when the final enemy is killed immediately after spawning: HandleEnemyRemoved may run while
    /// waveInProgress is still true, then SpawnWave calls this again as soon as spawning finishes.
    /// </summary>
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

        // Portal appears immediately before enemies begin spawning.
        yield return OpenPortal();

        foreach (var entry in wave.entries)
        {
            if (entry == null) continue;

            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyData);

                // Do not add an unnecessary vulnerable delay after the very last spawn in this entry.
                // The next entry can still begin immediately, while its own interval controls its spawns.
                if (i < entry.count - 1 && entry.spawnInterval > 0f)
                    yield return new WaitForSeconds(entry.spawnInterval);
            }
        }

        // Every enemy configured for this wave has now appeared, so close and hide the portal.
        yield return ClosePortal();

        waveInProgress = false;

        // Critical race-condition fix: the last enemy may already have died while spawning was active.
        // Re-check completion now that no more enemies can spawn for this wave.
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
            // Count the enemy before initialization so even an enemy that can die immediately during
            // initialization/runtime callbacks can never make the alive count go negative or miss clear.
            aliveEnemies++;
            e.Initialize(data, path.GetWaypoints());
        }
    }
}
