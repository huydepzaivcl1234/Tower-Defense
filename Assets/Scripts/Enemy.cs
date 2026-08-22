using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Walks along a WaypointPath, takes damage/status effects from towers, regenerates HP in ticks,
/// and either dies (rewarding gold) or reaches the end (costing player lives).
/// </summary>
public class Enemy : MonoBehaviour
{
    public static event System.Action<Enemy> OnAnyEnemyDied;
    public static event System.Action<Enemy> OnAnyEnemyReachedEnd;

    [Header("Config (assigned at spawn time)")]
    public EnemyData data;

    [Header("Optional")]
    public WorldHealthBar healthBar;
    public AudioClip deathSound;

    [Header("Animation (optional - safe to leave empty for a non-animated placeholder model)")]
    public Animator animator;
    public string speedParam = "Speed";
    public string dieTrigger = "Die";
    public float deathAnimDuration = 1f;

    [Header("Damage / Heal Popup (optional)")]
    [Tooltip("Same floating popup prefab is reused for damage and healing. Healing is shown green with a + prefix.")]
    public GameObject damagePopupPrefab;
    public Vector3 damagePopupOffset = new Vector3(0f, 1.6f, 0f);

    private float currentHP;
    private List<Transform> waypoints;
    private int currentWaypointIndex;
    private bool isDead;

    private float bleedDamagePerTick;
    private float bleedTickInterval;
    private float bleedTimeRemaining;
    private float bleedTickTimer;
    private float slowPercent;
    private float slowTimeRemaining;

    private float knockbackRemaining;
    [Tooltip("Units/second the knockback resolves at")]
    public float knockbackSpeed = 15f;

    private float currentShield;
    private float shieldTimeRemaining;
    private bool hpThresholdShieldUsed;
    private bool isShieldVisualActive;

    private float regenTickTimer;

    public bool IsAlive => !isDead && currentHP > 0f;
    public float CurrentHP => currentHP;
    public float HPPercent => (data != null && data.maxHP > 0f) ? currentHP / data.maxHP : 0f;
    public bool IsBleeding => bleedTimeRemaining > 0f;
    public bool IsSlowed => slowTimeRemaining > 0f;
    public bool IsShielded => currentShield > 0f;
    public float CurrentShield => currentShield;

    public float PathProgress { get; private set; }

    /// <summary>Called by WaveManager right after Instantiate or reuse from object pool.</summary>
    public void Initialize(EnemyData enemyData, List<Transform> path)
    {
        CancelInvoke(nameof(ReleaseToPool));

        data = enemyData;
        waypoints = path;
        currentHP = data.maxHP;
        currentWaypointIndex = 0;
        isDead = false;
        PathProgress = 0f;

        // Every pooled spawn is a fresh enemy life. No old status may leak into the new spawn.
        bleedDamagePerTick = 0f;
        bleedTickInterval = 0f;
        bleedTimeRemaining = 0f;
        bleedTickTimer = 0f;
        slowPercent = 0f;
        slowTimeRemaining = 0f;
        knockbackRemaining = 0f;

        currentShield = 0f;
        shieldTimeRemaining = 0f;
        hpThresholdShieldUsed = false;
        isShieldVisualActive = false;
        regenTickTimer = GetRegenInterval();

        if (animator != null)
        {
            animator.Rebind();
            if (!string.IsNullOrEmpty(speedParam))
                animator.SetFloat(speedParam, data.moveSpeed);
        }

        if (healthBar != null) healthBar.SetData(this);
        ApplyTintColor(data.tintColor);
    }

    private float GetRegenInterval()
    {
        if (data == null) return 1f;
        return data.hpRegenTickInterval > 0f ? data.hpRegenTickInterval : 1f;
    }

    private void ApplyTintColor(Color color)
    {
        foreach (var rend in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in rend.materials)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.color = color;
            }
        }
    }

    private void Update()
    {
        if (isDead || data == null || waypoints == null || waypoints.Count == 0) return;

        UpdateRegeneration();
        UpdateStatusEffects();
        if (isDead) return;

        if (knockbackRemaining > 0f)
            ApplyKnockbackMovement();
        else
            MoveAlongPath();
    }

    /// <summary>
    /// Regeneration works like a DoT/bleed timer, but in reverse:
    /// hpRegenPerSec is converted into a heal amount per tick using hpRegenTickInterval.
    /// Example: 10 HP/s at 0.5s interval => +5 every 0.5 seconds.
    /// </summary>
    private void UpdateRegeneration()
    {
        if (data.hpRegenPerSec <= 0f || currentHP >= data.maxHP)
        {
            // Being full should not bank timer progress. After the next hit, the first heal waits a full interval.
            regenTickTimer = GetRegenInterval();
            return;
        }

        float interval = GetRegenInterval();
        regenTickTimer -= Time.deltaTime;
        if (regenTickTimer > 0f) return;

        regenTickTimer += interval;
        Heal(data.hpRegenPerSec * interval, true);
    }

    /// <summary>
    /// Shared healing entry point used by self regeneration and support/healer enemies.
    /// Returns the actual HP restored after clamping to Max HP.
    /// </summary>
    public float Heal(float amount, bool showPopup = true)
    {
        if (isDead || data == null || amount <= 0f || currentHP >= data.maxHP) return 0f;

        float oldHP = currentHP;
        currentHP = Mathf.Min(data.maxHP, currentHP + amount);
        float actualHeal = currentHP - oldHP;
        if (actualHeal <= 0f) return 0f;

        if (healthBar != null) healthBar.Refresh(true);
        if (showPopup) SpawnHealPopup(actualHeal);
        return actualHeal;
    }

    private void UpdateStatusEffects()
    {
        if (bleedTimeRemaining > 0f)
        {
            bleedTickTimer -= Time.deltaTime;
            if (bleedTickTimer <= 0f)
            {
                TakeDamage(bleedDamagePerTick);
                bleedTickTimer += bleedTickInterval;
            }
            bleedTimeRemaining -= Time.deltaTime;
        }

        if (slowTimeRemaining > 0f)
        {
            slowTimeRemaining -= Time.deltaTime;
            if (slowTimeRemaining <= 0f)
            {
                slowTimeRemaining = 0f;
                slowPercent = 0f;
            }
        }
        else if (slowPercent != 0f)
        {
            slowPercent = 0f;
        }

        if (shieldTimeRemaining > 0f)
        {
            shieldTimeRemaining -= Time.deltaTime;
            if (shieldTimeRemaining <= 0f)
            {
                currentShield = 0f;
                UpdateShieldVisual();
            }
        }
    }

    public void ApplyBleed(float damagePerTick, float tickInterval, float duration)
    {
        if (isDead || damagePerTick <= 0f || tickInterval <= 0f || duration <= 0f) return;
        bleedDamagePerTick = damagePerTick;
        bleedTickInterval = tickInterval;
        bleedTimeRemaining = duration;
        bleedTickTimer = tickInterval;
    }

    public void ApplySlow(float percent, float duration)
    {
        if (isDead || data == null || percent <= 0f || duration <= 0f) return;
        float effectiveDuration = duration * (1f - data.ccResistPercent);
        if (effectiveDuration <= 0f) return;
        slowPercent = Mathf.Clamp01(percent);
        slowTimeRemaining = effectiveDuration;
    }

    public void ApplyKnockback(float distance)
    {
        if (isDead || distance <= 0f) return;
        float effectiveDistance = distance * (1f - data.ccResistPercent);
        if (effectiveDistance <= 0f) return;
        knockbackRemaining += effectiveDistance;
    }

    private void ApplyKnockbackMovement()
    {
        float step = Mathf.Min(knockbackRemaining, knockbackSpeed * Time.deltaTime);
        transform.position -= transform.forward * step;
        knockbackRemaining -= step;
    }

    public void GrantShield(float amount, float duration)
    {
        if (isDead || amount <= 0f || duration <= 0f) return;
        currentShield += amount;
        shieldTimeRemaining = Mathf.Max(shieldTimeRemaining, duration);
        UpdateShieldVisual();
    }

    private void UpdateShieldVisual()
    {
        if (healthBar != null) healthBar.RefreshShield();

        bool shouldShow = currentShield > 0f;
        if (shouldShow == isShieldVisualActive) return;
        isShieldVisualActive = shouldShow;
        ApplyTintColor(shouldShow ? data.shieldTintColor : data.tintColor);
    }

    private void MoveAlongPath()
    {
        if (currentWaypointIndex >= waypoints.Count)
        {
            ReachEnd();
            return;
        }

        Transform target = waypoints[currentWaypointIndex];
        if (target == null) { currentWaypointIndex++; return; }

        Vector3 toTarget = target.position - transform.position;
        float activeSlow = slowTimeRemaining > 0f ? slowPercent : 0f;
        float effectiveSpeed = data.moveSpeed * (1f - activeSlow);
        float step = effectiveSpeed * Time.deltaTime;

        if (animator != null && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, effectiveSpeed);

        if (toTarget.magnitude <= step)
        {
            transform.position = target.position;
            currentWaypointIndex++;
            PathProgress = currentWaypointIndex;
            if (currentWaypointIndex >= waypoints.Count)
                ReachEnd();
        }
        else
        {
            Vector3 moveDir = toTarget.normalized;
            transform.position += moveDir * step;
            if (moveDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(moveDir);

            Vector3 prevPos = currentWaypointIndex > 0 ? waypoints[currentWaypointIndex - 1].position : transform.position;
            float segLength = Vector3.Distance(prevPos, target.position);
            float frac = segLength > 0f ? 1f - (toTarget.magnitude / segLength) : 0f;
            PathProgress = currentWaypointIndex + Mathf.Clamp01(frac);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f) return;

        SpawnDamagePopup(amount);

        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, amount);
            currentShield -= absorbed;
            amount -= absorbed;
            UpdateShieldVisual();
        }

        if (amount > 0f) currentHP -= amount;

        if (!hpThresholdShieldUsed && data.shieldTriggerHPPercent > 0f && currentHP <= data.maxHP * data.shieldTriggerHPPercent)
        {
            hpThresholdShieldUsed = true;
            GrantShield(data.shieldTriggerAmount, data.shieldTriggerDuration);
        }

        if (healthBar != null) healthBar.Refresh(false);
        if (currentHP <= 0f) Die();
    }

    private GameObject SpawnPopupObject()
    {
        if (damagePopupPrefab == null) return null;
        Vector3 jitter = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
        Vector3 pos = transform.position + damagePopupOffset + jitter;

        return ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(damagePopupPrefab, pos, Quaternion.identity)
            : Instantiate(damagePopupPrefab, pos, Quaternion.identity);
    }

    private void SpawnDamagePopup(float amount)
    {
        GameObject go = SpawnPopupObject();
        if (go == null) return;
        DamagePopup popup = go.GetComponent<DamagePopup>();
        if (popup != null) popup.SetDamage(amount);
    }

    private void SpawnHealPopup(float amount)
    {
        GameObject go = SpawnPopupObject();
        if (go == null) return;
        DamagePopup popup = go.GetComponent<DamagePopup>();
        if (popup != null) popup.SetHealText(amount);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (GameManager.Instance != null) GameManager.Instance.AddGold(data.goldReward);
        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);
        if (healthBar != null) healthBar.Hide();
        OnAnyEnemyDied?.Invoke(this);

        if (animator != null && !string.IsNullOrEmpty(dieTrigger))
        {
            animator.SetTrigger(dieTrigger);
            Invoke(nameof(ReleaseToPool), deathAnimDuration);
        }
        else
        {
            ReleaseToPool();
        }
    }

    private void ReleaseToPool()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    private void ReachEnd()
    {
        if (isDead) return;
        isDead = true;
        if (GameManager.Instance != null) GameManager.Instance.LoseLives(data.damageToPlayer);
        OnAnyEnemyReachedEnd?.Invoke(this);

        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
}
