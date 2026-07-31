using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core tower behaviour: finds the enemy furthest along the path within range,
/// rotates an optional turret head toward it, fires at the level's attack speed,
/// and supports upgrading through the levels defined on its TowerData (default 3).
/// Requires ANY Collider on the prefab (e.g. Box/Capsule sized to the model) so
/// OnMouseDown can detect clicks for the Upgrade UI.
/// </summary>
public class Tower : MonoBehaviour
{
    /// <summary>Raised whenever any tower is clicked in the world. TowerUpgradeUI listens to this.</summary>
    public static event System.Action<Tower> OnAnyTowerClicked;

    /// <summary>Every currently-alive tower in the scene, used by TowerPlacementManager to block overlapping placement.</summary>
    private static readonly List<Tower> activeTowers = new List<Tower>();
    public static IReadOnlyList<Tower> ActiveTowers => activeTowers;

    [Header("Configuration")]
    public TowerData data;
    [Tooltip("Optional child transform that rotates to face the current target")]
    public Transform turretHead;
    [Tooltip("Extra Y-axis rotation added on top of the aim direction, to compensate for this model's " +
             "authored 'front' not matching Unity's +Z convention. If the barrel points away from " +
             "enemies instead of at them, try 180 first; for a 90°-off model try 90 or -90.")]
    public float turretForwardOffset = 0f;
    [Tooltip("Point projectiles spawn from. Defaults to tower position if empty")]
    public Transform firePoint;
    [Tooltip("Layer(s) enemies are on. If left as 'Nothing' the tower falls back to checking all layers.")]
    public LayerMask enemyLayerMask;

    [Header("Audio (optional)")]
    public AudioClip fireSound;

    [Header("Gold Popup (only used if this tower's data has Is Gold Generator checked)")]
    [Tooltip("Prefab shown above this tower's head each time it grants gold - reuse the same DamagePopup prefab used for enemies.")]
    public GameObject goldPopupPrefab;
    public Vector3 goldPopupOffset = new Vector3(0f, 2f, 0f);
    public Color goldPopupColor = new Color(1f, 0.84f, 0f, 1f);

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentLevelIndex = 0;

    [HideInInspector] public TowerPlacementSpot occupiedSpot;

    private float fireCooldown;
    private float targetSearchCooldown;
    private Enemy currentTarget;
    private AudioSource audioSource;

    private static readonly Collider[] overlapBuffer = new Collider[64]; // shared, reused each scan - avoids allocating a new array every 0.2s per tower

    public int CurrentLevelNumber => currentLevelIndex + 1;
    public int MaxLevelNumber => data != null ? data.levels.Length : 1;
    public bool IsMaxLevel => data == null || currentLevelIndex >= data.levels.Length - 1;
    public TowerLevelStats CurrentStats => data.levels[Mathf.Clamp(currentLevelIndex, 0, data.levels.Length - 1)];

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        activeTowers.Add(this);
        WaveManager.OnWaveCleared += HandleWaveCleared; // every tower subscribes; HandleWaveCleared itself checks Is Gold Generator once data is set
    }

    private void OnDisable()
    {
        activeTowers.Remove(this);
        WaveManager.OnWaveCleared -= HandleWaveCleared;
    }

    private void HandleWaveCleared()
    {
        if (data == null || !data.isGoldGenerator) return;
        int amount = Mathf.RoundToInt(CurrentStats.goldPerRound);
        if (amount <= 0) return;
        GameManager.Instance?.AddGold(amount);
        SpawnGoldPopup(amount);
    }

    private void SpawnGoldPopup(int amount)
    {
        if (goldPopupPrefab == null) return;
        Vector3 pos = transform.position + goldPopupOffset;

        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(goldPopupPrefab, pos, Quaternion.identity)
            : Instantiate(goldPopupPrefab, pos, Quaternion.identity);

        DamagePopup popup = go.GetComponent<DamagePopup>();
        if (popup != null) popup.SetGoldText(amount, goldPopupColor);
    }

    private void Update()
    {
        if (data == null || data.levels == null || data.levels.Length == 0) return;
        if (data.isGoldGenerator) return; // gold towers never target/fire - they only react to WaveManager.OnWaveCleared

        targetSearchCooldown -= Time.deltaTime;
        if (targetSearchCooldown <= 0f)
        {
            AcquireTarget();
            targetSearchCooldown = 0.2f; // re-scan 5x/sec instead of every frame, cheap on performance
        }

        if (currentTarget == null || !currentTarget.IsAlive)
        {
            currentTarget = null;
            return;
        }

        FaceTarget();

        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f)
        {
            Fire();
            fireCooldown = 1f / Mathf.Max(0.01f, CurrentStats.attackSpeed);
        }
    }

    private void AcquireTarget()
    {
        // Keep the current target if it's still alive and in range - avoids target-flicker between equally-ranked enemies.
        if (currentTarget != null && currentTarget.IsAlive &&
            Vector3.Distance(transform.position, currentTarget.transform.position) <= CurrentStats.range)
        {
            return;
        }

        int mask = enemyLayerMask.value != 0 ? enemyLayerMask.value : ~0; // fallback so an unset layer mask doesn't silently break targeting
        int count = Physics.OverlapSphereNonAlloc(transform.position, CurrentStats.range, overlapBuffer, mask);

        float bestProgress = -1f;
        Enemy best = null;
        for (int i = 0; i < count; i++)
        {
            Enemy e = overlapBuffer[i].GetComponent<Enemy>();
            if (e == null || !e.IsAlive) continue;
            if (e.PathProgress > bestProgress)
            {
                bestProgress = e.PathProgress;
                best = e;
            }
        }
        currentTarget = best;
    }

    private void FaceTarget()
    {
        if (turretHead == null || currentTarget == null) return;
        Vector3 dir = currentTarget.transform.position - turretHead.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, turretForwardOffset, 0f);
        turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRot, Time.deltaTime * 10f);
    }

    private void Fire()
    {
        if (currentTarget == null) return;
        if (fireSound != null && audioSource != null) audioSource.PlayOneShot(fireSound);

        if (data.projectilePrefab == null) return; // allow "hitscan-less" towers without a configured projectile to just sit idle rather than error
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        GameObject projGO = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(data.projectilePrefab, spawnPos, Quaternion.identity)
            : Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
        Projectile proj = projGO.GetComponent<Projectile>();
        if (proj != null) proj.Launch(currentTarget, CurrentStats);
    }

    public bool CanUpgrade() => !IsMaxLevel;

    public int GetNextUpgradeCost()
    {
        if (IsMaxLevel) return -1;
        return data.levels[currentLevelIndex + 1].upgradeCost;
    }

    public void Upgrade()
    {
        if (IsMaxLevel) return;
        currentLevelIndex++;
    }

    /// <summary>Refunds 50% of total gold spent (build cost + upgrade costs paid so far).</summary>
    public int GetSellValue()
    {
        int total = data.buildCost;
        for (int i = 1; i <= currentLevelIndex; i++)
            total += data.levels[i].upgradeCost;
        return Mathf.RoundToInt(total * 0.5f);
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return; // ignore clicks that are actually landing on UI
        }
        TowerPlacementManager.Instance?.CancelPlacement(); // clicking an existing tower means "select it", not "place here"
        OnAnyTowerClicked?.Invoke(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null || data.levels == null || data.levels.Length == 0) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, CurrentStats.range);
    }
}