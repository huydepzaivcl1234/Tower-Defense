using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core tower behaviour: finds the enemy furthest along the path within range,
/// rotates an optional turret head toward it, fires at the level's attack speed,
/// and supports upgrading through the levels defined on its TowerData.
/// Requires ANY Collider on the prefab so OnMouseDown can detect clicks.
/// </summary>
public class Tower : MonoBehaviour
{
    public static event System.Action<Tower> OnAnyTowerClicked;

    private static readonly List<Tower> activeTowers = new List<Tower>();
    public static IReadOnlyList<Tower> ActiveTowers => activeTowers;

    [Header("Configuration")]
    public TowerData data;
    [Tooltip("Optional child transform that rotates to face the current target")]
    public Transform turretHead;
    [Tooltip("Extra Y-axis rotation added on top of the aim direction.")]
    public float turretForwardOffset = 0f;
    [Tooltip("Point projectiles spawn from. Defaults to tower position if empty")]
    public Transform firePoint;
    [Tooltip("Layer(s) enemies are on. If left as 'Nothing' the tower falls back to checking all layers.")]
    public LayerMask enemyLayerMask;

    [Header("Visual Upgrade Phases (optional)")]
    [Tooltip("Visual roots for Phase 1, 2, 3, 4... Index follows the tower's current level. " +
             "Only the current phase is enabled. Each generated phase can contain its own TurretHead and FirePoint. " +
             "Safe to leave empty for old tower prefabs.")]
    public GameObject[] visualPhases;

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

    private static readonly Collider[] overlapBuffer = new Collider[64];

    public int CurrentLevelNumber => currentLevelIndex + 1;
    public int MaxLevelNumber => data != null && data.levels != null ? data.levels.Length : 1;
    public bool IsMaxLevel => data == null || data.levels == null || currentLevelIndex >= data.levels.Length - 1;
    public TowerLevelStats CurrentStats => data.levels[Mathf.Clamp(currentLevelIndex, 0, data.levels.Length - 1)];

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ApplyVisualPhase();
    }

    private void Start()
    {
        ApplyVisualPhase();
    }

    private void OnEnable()
    {
        activeTowers.Add(this);
        WaveManager.OnWaveCleared += HandleWaveCleared;
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
        if (data.isGoldGenerator) return;

        targetSearchCooldown -= Time.deltaTime;
        if (targetSearchCooldown <= 0f)
        {
            AcquireTarget();
            targetSearchCooldown = 0.2f;
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
        if (currentTarget != null && currentTarget.IsAlive &&
            Vector3.Distance(transform.position, currentTarget.transform.position) <= CurrentStats.range)
            return;

        int mask = enemyLayerMask.value != 0 ? enemyLayerMask.value : ~0;
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

        if (data.projectilePrefab == null) return;
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
        ApplyVisualPhase();
    }

    /// <summary>
    /// Enables only the visual matching the current level. If the active phase contains children named
    /// "TurretHead" and "FirePoint", references are automatically moved to that phase so aiming and
    /// projectile spawning keep working after the weapon changes shape.
    /// </summary>
    public void ApplyVisualPhase()
    {
        if (visualPhases == null || visualPhases.Length == 0) return;

        int activeIndex = Mathf.Clamp(currentLevelIndex, 0, visualPhases.Length - 1);
        GameObject activePhase = null;
        for (int i = 0; i < visualPhases.Length; i++)
        {
            if (visualPhases[i] == null) continue;
            bool active = i == activeIndex;
            visualPhases[i].SetActive(active);
            if (active) activePhase = visualPhases[i];
        }

        if (activePhase == null) return;
        Transform newHead = activePhase.transform.Find("TurretHead");
        if (newHead != null)
        {
            turretHead = newHead;
            Transform newFirePoint = newHead.Find("FirePoint");
            if (newFirePoint != null) firePoint = newFirePoint;
        }
        else
        {
            Transform newFirePoint = activePhase.transform.Find("FirePoint");
            if (newFirePoint != null) firePoint = newFirePoint;
        }
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
            return;

        TowerPlacementManager.Instance?.CancelPlacement();
        OnAnyTowerClicked?.Invoke(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null || data.levels == null || data.levels.Length == 0) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, CurrentStats.range);
    }
}
