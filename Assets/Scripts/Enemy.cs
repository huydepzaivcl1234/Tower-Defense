using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Walks along a WaypointPath, taking damage from towers, regenerating HP over time,
/// and either dying (rewarding gold) or reaching the end (costing the player lives).
/// Put this on the enemy prefab's root, on the "Enemy" layer, with a Collider (any type)
/// so Physics.OverlapSphere on towers can find it.
/// </summary>
public class Enemy : MonoBehaviour
{
    public static event System.Action<Enemy> OnAnyEnemyDied;
    public static event System.Action<Enemy> OnAnyEnemyReachedEnd;

    [Header("Config (assigned at spawn time)")]
    public EnemyData data;

    [Header("Optional")]
    [Tooltip("Child WorldHealthBar that shows HP above the enemy. Safe to leave empty.")]
    public WorldHealthBar healthBar;
    [Tooltip("Sound played once when this enemy dies. Safe to leave empty.")]
    public AudioClip deathSound;

    [Header("Animation (optional - safe to leave empty for a non-animated placeholder model)")]
    [Tooltip("The Animator on this enemy's model. Works with any imported model/rig as long as the " +
             "Animator Controller has a matching float parameter and trigger (see names below).")]
    public Animator animator;
    [Tooltip("Animator float parameter driven by current move speed, for a walk/idle blend tree. Leave blank to skip.")]
    public string speedParam = "Speed";
    [Tooltip("Animator trigger fired the instant this enemy dies.")]
    public string dieTrigger = "Die";
    [Tooltip("Seconds to keep the enemy alive after triggering Die, so its death animation has time to play, before removing it. Match this to your death clip's length.")]
    public float deathAnimDuration = 1f;

    [Header("Damage Popup (optional)")]
    [Tooltip("Prefab spawned above the enemy's head each time it takes damage, showing the amount. See DamagePopup.cs.")]
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

    private float regenRefreshTimer;

    public bool IsAlive => !isDead && currentHP > 0f;
    public float CurrentHP => currentHP;
    public float HPPercent => (data != null && data.maxHP > 0f) ? currentHP / data.maxHP : 0f;
    public bool IsBleeding => bleedTimeRemaining > 0f;
    public bool IsSlowed => slowTimeRemaining > 0f;
    public bool IsShielded => currentShield > 0f;
    public float CurrentShield => currentShield;

    /// <summary>Approximate progress along the path (waypoint index + fractional distance to next).
    /// Used by towers to prioritize the enemy furthest along the route.</summary>
    public float PathProgress { get; private set; }

    /// <summary>Called by WaveManager right after Instantiate or from object pool.</summary>
    public void Initialize(EnemyData enemyData, List<Transform> path)
    {
        // Cancel any pending delayed-release Invoke from a previous life (if reused from pool)
        CancelInvoke(nameof(ReleaseToPool));

        data = enemyData;
        waypoints = path;
        currentHP = data.maxHP;
        currentWaypointIndex = 0;
        isDead = false;
        PathProgress = 0f;
        bleedTimeRemaining = 0f;
        slowTimeRemaining = 0f;
        knockbackRemaining = 0f;
        currentShield = 0f;
        shieldTimeRemaining = 0f;
        hpThresholdShieldUsed = false;
        isShieldVisualActive = false;
        regenRefreshTimer = 0.5f;

        // Reset Animator state for reused pooled enemies
        if (animator != null)
            animator.Rebind(); // resets all parameters to default

        if (healthBar != null) healthBar.SetData(this);
        ApplyTintColor(data.tintColor);
    }

    private void ApplyTintColor(Color color)
    {
        foreach (var rend in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in rend.materials) // .materials (not sharedMaterials) instances per-enemy, doesn't touch the shared asset or other enemies
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); // URP Lit
                else if (mat.HasProperty("_Color")) mat.color = color;                 // Built-in Standard
            }
        }
    }

    private void Update()
    {
        if (isDead || data == null || waypoints == null || waypoints.Count == 0) return;

        if (data.hpRegenPerSec > 0f && currentHP < data.maxHP)
        {
            currentHP = Mathf.Min(data.maxHP, currentHP + data.hpRegenPerSec * Time.deltaTime);
            regenRefreshTimer -= Time.deltaTime;
            if (regenRefreshTimer <= 0f)
            {
                if (healthBar != null) healthBar.Refresh(true); // isHeal = true
                regenRefreshTimer = 0.5f; // throttle so an animated bar (e.g. MicroBar) isn't re-triggered 60x/sec
            }
        }

        UpdateStatusEffects();
        if (isDead) return; // a bleed tick may have just finished this enemy off

        if (knockbackRemaining > 0f)
            ApplyKnockbackMovement();
        else
            MoveAlongPath();
    }

    private void UpdateStatusEffects()
    {
        if (bleedTimeRemaining > 0f)
        {
            bleedTickTimer -= Time.deltaTime;
            if (bleedTickTimer <= 0f)
            {
                TakeDamage(bleedDamagePerTick);
                bleedTickTimer += bleedTickInterval; // += (not =) so leftover time isn't lost if a frame runs long
            }
            bleedTimeRemaining -= Time.deltaTime;
        }

        if (slowTimeRemaining > 0f)
        {
            slowTimeRemaining -= Time.deltaTime;
            if (slowTimeRemaining <= 0f) slowPercent = 0f;
        }

        if (shieldTimeRemaining > 0f)
        {
            shieldTimeRemaining -= Time.deltaTime;
            if (shieldTimeRemaining <= 0f)
            {
                currentShield = 0f;
                UpdateShieldVisual(); // only update visual when shield expires
            }
        }
    }

    /// <summary>"Chảy máu" - damage over time, ticking every Tick Interval seconds. A new application overwrites (refreshes) any current bleed.</summary>
    public void ApplyBleed(float damagePerTick, float tickInterval, float duration)
    {
        if (isDead || damagePerTick <= 0f || tickInterval <= 0f || duration <= 0f) return;
        bleedDamagePerTick = damagePerTick;
        bleedTickInterval = tickInterval;
        bleedTimeRemaining = duration;
        bleedTickTimer = tickInterval; // first tick fires after one interval, e.g. "10 dmg every 1s" ticks at t=1,2,3...
    }

    /// <summary>"Làm chậm" - temporary move-speed reduction. A new application overwrites (refreshes) any current slow.
    /// Duration is reduced by this enemy's CC Resist Percent (100% resist = fully immune).</summary>
    public void ApplySlow(float percent, float duration)
    {
        if (isDead || percent <= 0f || duration <= 0f) return;
        float effectiveDuration = duration * (1f - data.ccResistPercent);
        if (effectiveDuration <= 0f) return; // fully resisted
        slowPercent = Mathf.Clamp01(percent);
        slowTimeRemaining = effectiveDuration;
    }

    /// <summary>"Đẩy lùi" - pushes the enemy back along the path over the next few frames.
    /// Distance is reduced by this enemy's CC Resist Percent (100% resist = fully immune).
    /// Multiple hits in quick succession stack (add together).</summary>
    public void ApplyKnockback(float distance)
    {
        if (isDead || distance <= 0f) return;
        float effectiveDistance = distance * (1f - data.ccResistPercent);
        if (effectiveDistance <= 0f) return; // fully resisted
        knockbackRemaining += effectiveDistance;
    }

    private void ApplyKnockbackMovement()
    {
        float step = Mathf.Min(knockbackRemaining, knockbackSpeed * Time.deltaTime);
        transform.position -= transform.forward * step; // push back along whatever direction it's currently facing (away from the goal)
        knockbackRemaining -= step;
    }

    /// <summary>"Giáp ảo" - grants a shield that absorbs incoming damage before real HP is touched.
    /// Stacks additively with any existing shield; duration takes whichever is longer.
    /// Public so EnemyShieldAura (support-type enemies) can call it on other enemies too.</summary>
    public void GrantShield(float amount, float duration)
    {
        if (isDead || amount <= 0f || duration <= 0f) return;
        currentShield += amount;
        shieldTimeRemaining = Mathf.Max(shieldTimeRemaining, duration);
        UpdateShieldVisual();
    }

    private void UpdateShieldVisual()
    {
        if (healthBar != null) healthBar.RefreshShield(); // update the shield bar's fill amount every call, so it visibly shrinks as it absorbs hits

        bool shouldShow = currentShield > 0f;
        if (shouldShow == isShieldVisualActive) return; // tint only needs to change on an actual show/hide transition
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
        float effectiveSpeed = data.moveSpeed * (1f - slowPercent);
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

        SpawnDamagePopup(amount); // shows the full incoming hit, whether it lands on shield or real HP

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
            hpThresholdShieldUsed = true; // once per life, so it can't re-trigger every frame while low
            GrantShield(data.shieldTriggerAmount, data.shieldTriggerDuration);
        }

        if (healthBar != null) healthBar.Refresh();
        if (currentHP <= 0f) Die();
    }

    private void SpawnDamagePopup(float amount)
    {
        if (damagePopupPrefab == null) return;
        Vector3 jitter = new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f)); // so rapid hits don't perfectly overlap
        Vector3 pos = transform.position + damagePopupOffset + jitter;

        GameObject go = ObjectPool.Instance != null
            ? ObjectPool.Instance.Get(damagePopupPrefab, pos, Quaternion.identity)
            : Instantiate(damagePopupPrefab, pos, Quaternion.identity);

        DamagePopup popup = go.GetComponent<DamagePopup>();
        if (popup != null) popup.SetDamage(amount);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (GameManager.Instance != null) GameManager.Instance.AddGold(data.goldReward);
        if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position);
        if (healthBar != null) healthBar.Hide();
        OnAnyEnemyDied?.Invoke(this); // fires immediately - wave/gold logic shouldn't wait on a cosmetic animation

        if (animator != null && !string.IsNullOrEmpty(dieTrigger))
        {
            animator.SetTrigger(dieTrigger);
            // Release to pool after death animation completes (instead of Destroy)
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

        // Release to pool instead of destroying
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
}