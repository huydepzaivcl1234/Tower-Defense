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

    [SerializeField] private int currentWaveIndex = -1;
    private int aliveEnemies = 0;
    private bool waveInProgress = false;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waves.Count;
    public bool IsWaveInProgress => waveInProgress;
    public bool AllWavesComplete => currentWaveIndex >= waves.Count - 1 && !waveInProgress && aliveEnemies == 0 && waves.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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

    private void HandleEnemyRemoved(Enemy e)
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        if (aliveEnemies == 0 && !waveInProgress)
            OnWaveCleared?.Invoke();
        CheckForWin();
    }

    public bool CanStartNextWave()
    {
        bool relicChoiceOpen = RelicManager.Instance != null && RelicManager.Instance.IsChoosing;
        return !waveInProgress && !relicChoiceOpen && currentWaveIndex < waves.Count - 1;
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

        foreach (var entry in wave.entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                SpawnEnemy(entry.enemyData);
                yield return new WaitForSeconds(entry.spawnInterval);
            }
        }

        waveInProgress = false;
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
            e.Initialize(data, path.GetWaypoints());
            aliveEnemies++;
        }
    }
}
