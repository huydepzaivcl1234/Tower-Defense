using UnityEngine;

/// <summary>
/// Defines one enemy type (e.g. Grunt, Runner, Brute). Create instances via
/// Assets > Create > Tower Defense > Enemy Data, or use the
/// "Tower Defense > Create Default Tower & Enemy Data" editor menu for ready-made examples.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Tower Defense/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "New Enemy";
    [Tooltip("The enemy GameObject that gets instantiated when this type spawns")]
    public GameObject enemyPrefab;

    [Header("Stats (fully customizable)")]
    [Tooltip("Maximum / starting hit points")]
    public float maxHP = 100f;
    [Tooltip("Movement speed in world units per second")]
    public float moveSpeed = 3f;
    [Tooltip("HP regenerated per second while alive and not at full HP")]
    public float hpRegenPerSec = 0f;

    [Header("Rewards & Penalties")]
    [Tooltip("Gold granted to the player when this enemy is killed")]
    public int goldReward = 10;
    [Tooltip("Lives lost by the player if this enemy reaches the end of the path")]
    public int damageToPlayer = 1;

    [Header("Kháng khống chế (CC Resist)")]
    [Tooltip("Reduces incoming Slow duration and Knockback distance by this fraction (0-1). " +
             "1.0 = fully immune to both slow and knockback. Does NOT reduce bleed damage.")]
    [Range(0f, 1f)] public float ccResistPercent = 0f;

    [Header("Giáp ảo (shield) - ngưỡng máu tự kích hoạt")]
    [Tooltip("When HP drops to/below this fraction of max HP (0-1), automatically grants a shield once per life. 0 = disabled.")]
    [Range(0f, 1f)] public float shieldTriggerHPPercent = 0f;
    [Tooltip("Shield amount granted when the HP threshold triggers.")]
    public float shieldTriggerAmount = 0f;
    [Tooltip("How many seconds the HP-threshold shield lasts before expiring, if not fully depleted by damage first.")]
    public float shieldTriggerDuration = 5f;

    [Header("Appearance")]
    [Tooltip("Tints the model's material color - handy for telling enemy types apart at a glance. " +
             "Leave as white to keep the model's original texture colors untouched.")]
    public Color tintColor = Color.white;
    [Tooltip("Tint applied instead, while this enemy currently has an active shield - the main visual cue that it's shielded.")]
    public Color shieldTintColor = new Color(0.3f, 0.85f, 1f, 1f);
}