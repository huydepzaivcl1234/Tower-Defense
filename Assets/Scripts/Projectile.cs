using UnityEngine;

/// <summary>
/// Simple, reliable projectile movement (no Rigidbody/physics collision needed).
/// Moves toward its target each frame and applies damage + all status effects on arrival.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("Units traveled per second")]
    public float speed = 20f;
    [Tooltip("If true, continuously tracks the target's current position. " +
             "If false, flies straight toward where the target was at launch (still guaranteed to hit - simplified TD behaviour).")]
    public bool homing = true;
    [Tooltip("Optional particle/VFX prefab spawned on impact")]
    public GameObject impactEffectPrefab;

    private Enemy target;
    private float damage;
    private float splashRadius;
    private float bleedDamagePerTick;
    private float bleedTickInterval;
    private float bleedDuration;
    private float slowPercent;
    private float slowDuration;
    private float knockbackDistance;
    private Vector3 destination;

    /// <summary>Launch carrying a full level's stats, so splash/bleed/slow/knockback all ride along
    /// automatically - add a new effect field to TowerLevelStats and it's available here with no
    /// signature changes needed.</summary>
    public void Launch(Enemy targetEnemy, TowerLevelStats stats)
    {
        target = targetEnemy;
        damage = stats.strength;
        splashRadius = stats.splashRadius;
        bleedDamagePerTick = stats.bleedDamagePerTick;
        bleedTickInterval = stats.bleedTickInterval;
        bleedDuration = stats.bleedDuration;
        slowPercent = stats.slowPercent;
        slowDuration = stats.slowDuration;
        knockbackDistance = stats.knockbackDistance;
        destination = target != null ? target.transform.position : transform.position;
    }

    private void Update()
    {
        if (homing && target != null && target.IsAlive)
            destination = target.transform.position;

        Vector3 dir = destination - transform.position;
        float step = speed * Time.deltaTime;

        if (dir.magnitude <= step)
        {
            HitTarget();
            return;
        }

        transform.position += dir.normalized * step;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void HitTarget()
    {
        if (target != null && target.IsAlive)
        {
            target.TakeDamage(damage);
            ApplyStatusEffects(target);
        }

        if (splashRadius > 0f)
            ApplySplashDamage();

        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

        if (ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
        else Destroy(gameObject);
    }

    private static readonly Collider[] overlapBuffer = new Collider[64]; // shared, reused each splash hit - avoids allocating a new array every explosion

    private void ApplySplashDamage()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, splashRadius, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Enemy e = overlapBuffer[i].GetComponent<Enemy>();
            if (e == null || !e.IsAlive || e == target) continue; // primary target already took its hit above
            e.TakeDamage(damage);
            ApplyStatusEffects(e);
        }
    }

    private void ApplyStatusEffects(Enemy e)
    {
        if (bleedDamagePerTick > 0f && bleedDuration > 0f) e.ApplyBleed(bleedDamagePerTick, bleedTickInterval, bleedDuration);
        if (slowPercent > 0f && slowDuration > 0f) e.ApplySlow(slowPercent, slowDuration);
        if (knockbackDistance > 0f) e.ApplyKnockback(knockbackDistance);
    }
}