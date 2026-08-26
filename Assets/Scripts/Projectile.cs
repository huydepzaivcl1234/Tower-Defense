using UnityEngine;

/// <summary>
/// Simple projectile movement. Carries a snapshot of tower stats at launch.
/// Advanced relics and world events can modify projectile speed and damage.
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
    private float baseSpeed;
    private float effectiveSpeed;
    private float distanceTravelled;
    private TowerData sourceTowerData;

    private void Awake()
    {
        baseSpeed = speed;
        effectiveSpeed = baseSpeed;
    }

    public void Launch(Enemy targetEnemy, TowerLevelStats stats)
    {
        Launch(targetEnemy, stats, stats != null ? stats.strength : 0f, null);
    }

    public void Launch(Enemy targetEnemy, TowerLevelStats stats, float effectiveDamage)
    {
        Launch(targetEnemy, stats, effectiveDamage, null);
    }

    public void Launch(Enemy targetEnemy, TowerLevelStats stats, float effectiveDamage, TowerData sourceTower)
    {
        if (stats == null) return;
        target = targetEnemy;
        damage = effectiveDamage;
        sourceTowerData = sourceTower;
        distanceTravelled = 0f;
        effectiveSpeed = baseSpeed;
        if (RelicManager.Instance != null)
            effectiveSpeed = RelicManager.Instance.ApplyProjectileSpeed(effectiveSpeed);
        if (WorldEventManager.Instance != null)
            effectiveSpeed = WorldEventManager.Instance.ApplyProjectileSpeed(effectiveSpeed);

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
        float distance = dir.magnitude;
        float step = effectiveSpeed * Time.deltaTime;
        if (distance <= step)
        {
            distanceTravelled += distance;
            transform.position = destination;
            HitTarget();
            return;
        }

        if (distance > 0.0001f)
        {
            Vector3 moveDir = dir / distance;
            transform.position += moveDir * step;
            distanceTravelled += step;
            transform.rotation = Quaternion.LookRotation(moveDir);
        }
    }

    private float GetHitDamage()
    {
        float result = damage;
        if (RelicManager.Instance != null)
        {
            result = RelicManager.Instance.ApplyProjectileTravelDamage(sourceTowerData, result, distanceTravelled);
            result = RelicManager.Instance.RollCriticalDamage(result);
        }
        return result;
    }

    private void HitTarget()
    {
        if (target != null && target.IsAlive)
        {
            target.TakeDamage(GetHitDamage());
            ApplyStatusEffects(target);
        }

        if (splashRadius > 0f) ApplySplashDamage();
        SpawnImpactEffect();

        if (ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
        else Destroy(gameObject);
    }

    private void SpawnImpactEffect()
    {
        if (impactEffectPrefab == null) return;
        if (ObjectPool.Instance == null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
            return;
        }
        GameObject fx = ObjectPool.Instance.Get(impactEffectPrefab, transform.position, Quaternion.identity);
        if (fx == null) return;
        PooledVFXAutoRelease autoRelease = fx.GetComponent<PooledVFXAutoRelease>();
        if (autoRelease == null) autoRelease = fx.AddComponent<PooledVFXAutoRelease>();
        autoRelease.PlayAndSchedule();
    }

    private static readonly Collider[] overlapBuffer = new Collider[64];

    private void ApplySplashDamage()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, splashRadius, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Enemy e = overlapBuffer[i].GetComponent<Enemy>();
            if (e == null || !e.IsAlive || e == target) continue;
            e.TakeDamage(GetHitDamage());
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
