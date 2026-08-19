using UnityEngine;
using Microlight.MicroBar;

/// <summary>
/// Wraps TWO MicroBar instances (Microlight Games' "MicroBar - Animated Health Bar
/// Framework") - one for HP, one for shield - so both animate with the exact same smooth
/// feel with zero extra animation code needed. Also keeps both correctly pinned above the
/// enemy's head using the model's actual rendered bounds, so positioning stays correct no
/// matter what scale the enemy prefab is set to.
///
/// Setup: as children of this object's World Space Canvas, drag in TWO instances of any
/// MicroBar prefab variant (Simple/Delayed/Disappear/Impact/Punch/Shake) - one assigned to
/// Bar (HP), one assigned to Shield Bar. Requires DOTween (MicroBar's own dependency).
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    public MicroBar bar;
    [Tooltip("A second MicroBar instance used to show shield amount, with the same animation as HP. Leave empty to skip.")]
    public MicroBar shieldBar;

    [Header("Positioning")]
    [Tooltip("Extra padding above the top of the model's actual rendered bounds, so the bar floats " +
             "just clear of the head - stays correct at any model scale, unlike a fixed offset.")]
    public float headroomPadding = 0.3f;
    [Tooltip("Fallback offset used only if no Renderer could be found on the enemy")]
    public float fallbackOffsetY = 2f;

    private Enemy enemy;
    private Camera mainCam;
    private Renderer modelRenderer;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    public void SetData(Enemy e)
    {
        // Enemy objects are pooled. Hide() disables this child when an enemy dies, so a reused
        // pooled enemy must explicitly reactivate the health bar on its next Initialize call.
        // Without this, later waves randomly contain enemies with no visible HP bar.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        enemy = e;
        modelRenderer = e.GetComponentInChildren<Renderer>(); // used every frame to find the model's real top, regardless of scale

        if (bar != null) bar.Initialize(e.data.maxHP);

        if (shieldBar != null)
        {
            shieldBar.Initialize(e.data.maxHP); // shield is shown on the same scale as max HP
            shieldBar.UpdateBar(0f, false, UpdateAnim.Damage); // force it empty at spawn regardless of Initialize's own default fill
            shieldBar.gameObject.SetActive(false);
        }
    }

    /// <summary>Call whenever HP changes. isHeal just picks which MicroBar animation/color plays (Damage vs Heal).</summary>
    public void Refresh(bool isHeal = false)
    {
        if (bar == null || enemy == null) return;
        bar.UpdateBar(enemy.CurrentHP, false, isHeal ? UpdateAnim.Heal : UpdateAnim.Damage);
    }

    /// <summary>Call whenever shield amount changes. isGain = true for GrantShield (bar fills), false for absorbing a hit or expiring (bar drains).</summary>
    public void RefreshShield(bool isGain = false)
    {
        if (shieldBar == null || enemy == null) return;

        bool shown = enemy.IsShielded;
        if (!shown)
        {
            shieldBar.gameObject.SetActive(false);
            return;
        }

        shieldBar.gameObject.SetActive(true);
        shieldBar.UpdateBar(enemy.CurrentShield, false, isGain ? UpdateAnim.Heal : UpdateAnim.Damage);
    }

    public void Hide() => gameObject.SetActive(false);

    private void LateUpdate()
    {
        if (enemy == null) return;

        Vector3 pos = enemy.transform.position;
        if (modelRenderer != null)
            pos.y = modelRenderer.bounds.max.y; // the model's actual current top in world space - correct at any scale, any pose
        else
            pos.y += fallbackOffsetY;

        transform.position = pos + Vector3.up * headroomPadding;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null) transform.rotation = mainCam.transform.rotation;
    }
}