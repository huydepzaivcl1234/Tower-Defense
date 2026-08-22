using UnityEngine;

/// <summary>
/// Simple projectile movement. Carries a snapshot of tower stats at launch.
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public bool homing = true;
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

    public void Launch(Enemy targetEnemy, TowerLevelStats stats)
    {
        Launch(targetEnemy, stats, stats != null ? stats.strength : 0f);
    }

    public void Launch(Enemy targetEnemy, TowerLevelStats stats, float effectiveDamage)
    {
        target = targetEnemy;
        damage = effectiveDamage;
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

        if (splashRadius > 0f) ApplySplashDamage();
        if (impactEffectPrefab != null)
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

        if (ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
        else Destroy(gameObject);
    }

    private static readonly Collider[] overlapBuffer = new Collider[64];

    private void ApplySplashDamage()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, splashRadius, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Enemy e = overlapBuffer[i].GetComponent<Enemy>();
            if (e == null || !e.IsAlive || e == target) continue;
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
