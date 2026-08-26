using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core tower behaviour: targeting, aiming, firing and upgrades.
/// Relic/world-event modifiers are read at runtime and never mutate TowerData assets.
/// </summary>
public class Tower : MonoBehaviour
{
    public static event System.Action<Tower> OnAnyTowerClicked;

    private static readonly List<Tower> activeTowers = new List<Tower>();
    public static IReadOnlyList<Tower> ActiveTowers => activeTowers;

    [Header("Configuration")]
    public TowerData data;
    public Transform turretHead;
    public float turretForwardOffset = 0f;
    public Transform firePoint;
    public LayerMask enemyLayerMask;

    [Header("Visual Upgrade Phases (optional)")]
    public GameObject[] visualPhases;

    [Header("Audio (optional)")]
    public AudioClip fireSound;

    [Header("Gold Popup")]
    public GameObject goldPopupPrefab;
    public Vector3 goldPopupOffset = new Vector3(0f, 2f, 0f);
    public Color goldPopupColor = new Color(1f, 0.84f, 0f, 1f);

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentLevelIndex = 0;
    [SerializeField] private float temporaryAttackSpeedPenaltyPercent;
    [SerializeField] private float temporaryAttackSpeedPenaltyTime;

    [HideInInspector] public TowerPlacementSpot occupiedSpot;

    private float fireCooldown;
    private float targetSearchCooldown;
    private Enemy currentTarget;
    private AudioSource audioSource;
    private TowerFireAnimator fireAnimator;

    private static readonly Collider[] overlapBuffer = new Collider[64];

    public int CurrentLevelIndex => currentLevelIndex;
    public int CurrentLevelNumber => currentLevelIndex + 1;
    public int MaxLevelNumber => data != null && data.levels != null ? data.levels.Length : 1;
    public bool IsMaxLevel => data == null || data.levels == null || currentLevelIndex >= data.levels.Length - 1;
    public TowerLevelStats CurrentStats => data.levels[Mathf.Clamp(currentLevelIndex, 0, data.levels.Length - 1)];

    public float CurrentDamage => GetEffectiveDamageForLevel(currentLevelIndex);
    public float CurrentAttackSpeed => GetEffectiveAttackSpeedForLevel(currentLevelIndex);
    public float CurrentRange => GetEffectiveRangeForLevel(currentLevelIndex);

    public float GetEffectiveDamageForLevel(int levelIndex)
    {
        if (data == null || data.levels == null || data.levels.Length == 0) return 0f;
        int index = Mathf.Clamp(levelIndex, 0, data.levels.Length - 1);
        float result = data.levels[index].strength;
        if (RelicManager.Instance != null)
            result = RelicManager.Instance.ApplyDamageForLevel(data, index + 1, result);
        if (WorldEventManager.Instance != null)
            result = WorldEventManager.Instance.ApplyTowerDamage(result);
        return result;
    }

    public float GetEffectiveAttackSpeedForLevel(int levelIndex)
    {
        if (data == null || data.levels == null || data.levels.Length == 0) return 0f;
        int index = Mathf.Clamp(levelIndex, 0, data.levels.Length - 1);
        float result = data.levels[index].attackSpeed;
        if (RelicManager.Instance != null)
            result = RelicManager.Instance.ApplyAttackSpeed(result);
        if (WorldEventManager.Instance != null)
            result = WorldEventManager.Instance.ApplyTowerAttackSpeed(result);
        if (temporaryAttackSpeedPenaltyTime > 0f)
            result *= 1f - Mathf.Clamp01(temporaryAttackSpeedPenaltyPercent);
        return Mathf.Max(0.01f, result);
    }

    public float GetEffectiveRangeForLevel(int levelIndex)
    {
        if (data == null || data.levels == null || data.levels.Length == 0) return 0f;
        int index = Mathf.Clamp(levelIndex, 0, data.levels.Length - 1);
        float baseRange = data.levels[index].range;
        return RelicManager.Instance != null ? RelicManager.Instance.ApplyRange(data, baseRange) : baseRange;
    }

    public void ApplyTemporaryAttackSpeedPenalty(float percent, float duration)
    {
        if (percent <= 0f || duration <= 0f) return;
        temporaryAttackSpeedPenaltyPercent = Mathf.Max(temporaryAttackSpeedPenaltyPercent, Mathf.Clamp01(percent));
        temporaryAttackSpeedPenaltyTime = Mathf.Max(temporaryAttackSpeedPenaltyTime, duration);
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        fireAnimator = GetComponent<TowerFireAnimator>();
        ApplyVisualPhase();
    }

    private void Start() => ApplyVisualPhase();

    private void OnEnable()
    {
        if (!activeTowers.Contains(this)) activeTowers.Add(this);
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
        int granted = GameManager.Instance != null ? GameManager.Instance.AddGold(amount) : amount;
        SpawnGoldPopup(granted);
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
        if (temporaryAttackSpeedPenaltyTime > 0f)
        {
            temporaryAttackSpeedPenaltyTime -= Time.deltaTime;
            if (temporaryAttackSpeedPenaltyTime <= 0f)
            {
                temporaryAttackSpeedPenaltyTime = 0f;
                temporaryAttackSpeedPenaltyPercent = 0f;
            }
        }

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
            fireCooldown = 1f / Mathf.Max(0.01f, CurrentAttackSpeed);
        }
    }

    private void AcquireTarget()
    {
        float range = CurrentRange;
        if (currentTarget != null && currentTarget.IsAlive &&
            Vector3.Distance(transform.position, currentTarget.transform.position) <= range)
            return;

        int mask = enemyLayerMask.value != 0 ? enemyLayerMask.value : ~0;
        int count = Physics.OverlapSphereNonAlloc(transform.position, range, overlapBuffer, mask);
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
        if (currentTarget == null || data.projectilePrefab == null) return;
        if (fireSound != null && audioSource != null) audioSource.PlayOneShot(fireSound);
        fireAnimator?.PlayFire();

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        GameObject projGO = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(data.projectilePrefab, spawnPos, Quaternion.identity)
            : Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
        Projectile proj = projGO.GetComponent<Projectile>();
        if (proj != null) proj.Launch(currentTarget, CurrentStats, CurrentDamage, data);
    }

    public bool CanUpgrade() => !IsMaxLevel;

    public int GetNextUpgradeCost()
    {
        if (IsMaxLevel) return -1;
        int baseCost = data.levels[currentLevelIndex + 1].upgradeCost;
        return RelicManager.Instance != null ? RelicManager.Instance.GetUpgradeCost(baseCost) : baseCost;
    }

    public void Upgrade()
    {
        if (IsMaxLevel) return;
        currentLevelIndex++;
        ApplyVisualPhase();
    }

    public void ApplyVisualPhase()
    {
        if (visualPhases == null || visualPhases.Length == 0)
        {
            fireAnimator?.Rebind();
            return;
        }

        int activeIndex = Mathf.Clamp(currentLevelIndex, 0, visualPhases.Length - 1);
        GameObject activePhase = null;
        for (int i = 0; i < visualPhases.Length; i++)
        {
            if (visualPhases[i] == null) continue;
            bool active = i == activeIndex;
            visualPhases[i].SetActive(active);
            if (active) activePhase = visualPhases[i];
        }

        if (activePhase == null) { fireAnimator?.Rebind(); return; }

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
        fireAnimator?.Rebind();
    }

    public int GetSellValue()
    {
        int build = RelicManager.Instance != null ? RelicManager.Instance.GetBuildCost(data.buildCost) : data.buildCost;
        int total = build;
        for (int i = 1; i <= currentLevelIndex; i++)
        {
            int baseUpgrade = data.levels[i].upgradeCost;
            total += RelicManager.Instance != null ? RelicManager.Instance.GetUpgradeCost(baseUpgrade) : baseUpgrade;
        }
        return Mathf.RoundToInt(total * 0.5f);
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        TowerPlacementManager.Instance?.CancelPlacement();
        OnAnyTowerClicked?.Invoke(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null || data.levels == null || data.levels.Length == 0) return;
        Gizmos.color = Color.cyan;
        float range = Application.isPlaying ? CurrentRange : CurrentStats.range;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
