using UnityEngine;
using Microlight.MicroBar;

/// <summary>
/// World-space HP/shield bars for enemies.
/// HP bar stays hidden at full health, appears only after real HP has been lost,
/// and hides itself again as soon as the enemy is fully healed.
///
/// Important: MicroBar creates DOTween Sequences/Tweeners every time UpdateBar is called.
/// We cache the last submitted values so repeated refresh calls with no real value change
/// do not create duplicate tweens (especially during splash damage / shield-heavy waves).
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    public MicroBar bar;
    [Tooltip("A second MicroBar instance used to show shield amount, with the same animation as HP. Leave empty to skip.")]
    public MicroBar shieldBar;

    [Header("Positioning")]
    public float headroomPadding = 0.3f;
    public float fallbackOffsetY = 2f;

    private Enemy enemy;
    private Camera mainCam;
    private Renderer modelRenderer;

    private float lastHP = float.NaN;
    private float lastShield = float.NaN;
    private bool lastHPWasHeal;
    private bool lastShieldWasGain;

    private const float ValueEpsilon = 0.001f;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    public void SetData(Enemy e)
    {
        // Enemy objects are pooled. The root must be active again on reuse so Refresh can show HP later.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        enemy = e;
        modelRenderer = e.GetComponentInChildren<Renderer>();

        // Reset cached values for this new pooled life.
        lastHP = e != null ? e.CurrentHP : float.NaN;
        lastShield = 0f;
        lastHPWasHeal = false;
        lastShieldWasGain = false;

        if (bar != null)
        {
            bar.Initialize(e.data.maxHP);
            // Freshly spawned enemies are full HP: don't show an unnecessary health bar.
            bar.gameObject.SetActive(false);
        }

        if (shieldBar != null)
        {
            shieldBar.Initialize(e.data.maxHP);
            // We still set the logical bar to zero once for a freshly pooled life, then hide it.
            // This prevents an old shield fill from flashing when the bar becomes visible later.
            shieldBar.UpdateBar(0f, false, UpdateAnim.Damage);
            shieldBar.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Updates HP and visibility. Damage/heal animation is selected by isHeal.
    /// HP bar is visible only while 0 &lt; HP &lt; Max HP.
    /// Duplicate refreshes with the same HP are ignored to avoid needless DOTween allocations.
    /// </summary>
    public void Refresh(bool isHeal = false)
    {
        if (bar == null || enemy == null || enemy.data == null) return;

        float hp = enemy.CurrentHP;
        bool shouldShow = enemy.IsAlive && hp < enemy.data.maxHP - ValueEpsilon;

        // Full HP is hidden. There is no reason to build a tween that the player cannot see.
        if (!shouldShow)
        {
            lastHP = hp;
            lastHPWasHeal = isHeal;
            if (bar.gameObject.activeSelf)
                bar.gameObject.SetActive(false);
            return;
        }

        if (!bar.gameObject.activeSelf)
            bar.gameObject.SetActive(true);

        bool valueChanged = float.IsNaN(lastHP) || Mathf.Abs(hp - lastHP) > ValueEpsilon;
        bool animationDirectionChanged = isHeal != lastHPWasHeal;

        // Most callers only need an update when the numeric value actually changed.
        // The direction check is kept for the rare case where damage/heal animation mode changes
        // while the value is effectively identical due to clamping/rounding.
        if (!valueChanged && !animationDirectionChanged)
            return;

        bar.UpdateBar(hp, false, isHeal ? UpdateAnim.Heal : UpdateAnim.Damage);
        lastHP = hp;
        lastHPWasHeal = isHeal;
    }

    public void RefreshShield(bool isGain = false)
    {
        if (shieldBar == null || enemy == null) return;

        float shield = enemy.CurrentShield;
        bool shown = enemy.IsShielded;
        if (!shown)
        {
            lastShield = 0f;
            lastShieldWasGain = false;
            if (shieldBar.gameObject.activeSelf)
                shieldBar.gameObject.SetActive(false);
            return;
        }

        if (!shieldBar.gameObject.activeSelf)
            shieldBar.gameObject.SetActive(true);

        bool valueChanged = float.IsNaN(lastShield) || Mathf.Abs(shield - lastShield) > ValueEpsilon;
        bool animationDirectionChanged = isGain != lastShieldWasGain;
        if (!valueChanged && !animationDirectionChanged)
            return;

        shieldBar.UpdateBar(shield, false, isGain ? UpdateAnim.Heal : UpdateAnim.Damage);
        lastShield = shield;
        lastShieldWasGain = isGain;
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (enemy == null) return;

        Vector3 pos = enemy.transform.position;
        if (modelRenderer != null)
            pos.y = modelRenderer.bounds.max.y;
        else
            pos.y += fallbackOffsetY;

        transform.position = pos + Vector3.up * headroomPadding;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null) transform.rotation = mainCam.transform.rotation;
    }
}
