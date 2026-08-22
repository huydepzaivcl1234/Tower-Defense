using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional support ability for healer-type enemies.
/// Every interval, heals injured living allies inside radius and can also heal itself.
/// Uses Enemy.Heal(), so popup/health-bar behavior is identical to normal regeneration.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyHealAura : MonoBehaviour
{
    [Tooltip("How often (seconds) this healing pulse activates.")]
    [Min(0.05f)] public float interval = 3f;

    [Tooltip("HP restored to each injured ally per activation.")]
    [Min(0f)] public float healAmount = 20f;

    [Tooltip("Range to look for allies to heal.")]
    [Min(0f)] public float radius = 5f;

    [Tooltip("If enabled, the healer also receives healing from every pulse when injured.")]
    public bool healSelf = true;

    [Tooltip("Multiplier applied to self healing. 1 = same amount as allies.")]
    [Min(0f)] public float selfHealMultiplier = 1f;

    [Tooltip("Layer(s) enemies are on. If left as Nothing, scans all layers.")]
    public LayerMask enemyLayerMask;

    private float timer;
    private Enemy self;

    private static readonly Collider[] overlapBuffer = new Collider[64];
    private readonly HashSet<Enemy> healedThisPulse = new HashSet<Enemy>();

    private void Awake()
    {
        self = GetComponent<Enemy>();
        timer = Mathf.Max(0.05f, interval);
    }

    private void OnEnable()
    {
        // Pooled healer enemies always start a fresh cooldown on reuse.
        timer = Mathf.Max(0.05f, interval);
        healedThisPulse.Clear();
    }

    private void Update()
    {
        if (self == null || !self.IsAlive) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        Activate();
        timer += Mathf.Max(0.05f, interval);
    }

    private void Activate()
    {
        if (healAmount <= 0f) return;

        healedThisPulse.Clear();

        int mask = enemyLayerMask.value != 0 ? enemyLayerMask.value : ~0;
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer, mask);

        for (int i = 0; i < count; i++)
        {
            Enemy other = overlapBuffer[i].GetComponentInParent<Enemy>();
            if (other == null || other == self || !other.IsAlive) continue;

            // One enemy may have multiple colliders. Heal each enemy only once per pulse.
            if (!healedThisPulse.Add(other)) continue;

            other.Heal(healAmount, true);
        }

        // Unlike the shield aura's fallback behavior, a healer can support allies AND
        // restore its own HP on the same pulse, as requested.
        if (healSelf && self.IsAlive)
            self.Heal(healAmount * selfHealMultiplier, true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
