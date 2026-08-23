using UnityEngine;

/// <summary>
/// Returns a pooled one-shot VFX object after its particle systems have finished.
/// Added automatically by Projectile on first pooled use, so existing VFX prefabs need no manual changes.
/// </summary>
public class PooledVFXAutoRelease : MonoBehaviour
{
    [Min(0.05f)] public float fallbackLifetime = 1.5f;

    public void PlayAndSchedule()
    {
        CancelInvoke(nameof(Release));

        float lifetime = 0f;
        ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null) continue;

            ps.Clear(true);
            ps.Play(true);

            var main = ps.main;
            float startLifetime = main.startLifetime.constantMax;
            lifetime = Mathf.Max(lifetime, main.duration + startLifetime);
        }

        if (lifetime <= 0f)
            lifetime = fallbackLifetime;

        Invoke(nameof(Release), Mathf.Max(0.05f, lifetime));
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Release));
    }

    private void Release()
    {
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
}
