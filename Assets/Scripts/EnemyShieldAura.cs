using UnityEngine;

/// <summary>
/// Optional add-on for "support" enemy types: every Interval seconds, grants a shield
/// to every other living enemy within Radius - or to itself if it's alone (no one else
/// nearby to shield). Add this alongside Enemy on any enemy prefab you want to act as a
/// shield-granter (e.g. a "Shaman"/"Priest" type).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyShieldAura : MonoBehaviour
{
    [Tooltip("How often (seconds) this grants shields")]
    public float interval = 5f;
    [Tooltip("Shield amount granted per activation")]
    public float shieldAmount = 20f;
    [Tooltip("How many seconds the granted shield lasts")]
    public float shieldDuration = 4f;
    [Tooltip("Range to look for other enemies to shield")]
    public float radius = 5f;
    [Tooltip("Layer(s) other enemies are on. If left as 'Nothing' it falls back to scanning everything.")]
    public LayerMask enemyLayerMask;

    private float timer;
    private Enemy self;
    private static readonly Collider[] overlapBuffer = new Collider[64]; // shared, reused each activation

    private void Awake()
    {
        self = GetComponent<Enemy>();
        timer = interval; // wait one full interval before the first activation
    }

    private void Update()
    {
        if (self == null || !self.IsAlive) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Activate();
            timer = interval;
        }
    }

    private void Activate()
    {
        int mask = enemyLayerMask.value != 0 ? enemyLayerMask.value : ~0;
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer, mask);

        bool grantedToOther = false;
        for (int i = 0; i < count; i++)
        {
            Enemy other = overlapBuffer[i].GetComponent<Enemy>();
            if (other == null || other == self || !other.IsAlive) continue;
            other.GrantShield(shieldAmount, shieldDuration);
            grantedToOther = true;
        }

        if (!grantedToOther) self.GrantShield(shieldAmount, shieldDuration); // alone - shield itself instead
    }
}