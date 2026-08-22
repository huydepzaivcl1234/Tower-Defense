using UnityEngine;
using Microlight.MicroBar;

/// <summary>
/// World-space HP/shield bars for enemies.
/// HP bar stays hidden at full health, appears only after real HP has been lost,
/// and hides itself again as soon as the enemy is fully healed.
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

        if (bar != null)
        {
            bar.Initialize(e.data.maxHP);
            // Freshly spawned enemies are full HP: don't show an unnecessary health bar.
            bar.gameObject.SetActive(false);
        }

        if (shieldBar != null)
        {
            shieldBar.Initialize(e.data.maxHP);
            shieldBar.UpdateBar(0f, false, UpdateAnim.Damage);
            shieldBar.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Updates HP and visibility. Damage/heal animation is selected by isHeal.
    /// HP bar is visible only while 0 < HP < Max HP.
    /// </summary>
    public void Refresh(bool isHeal = false)
    {
        if (bar == null || enemy == null) return;

        bool shouldShow = enemy.IsAlive && enemy.CurrentHP < enemy.data.maxHP - 0.001f;
        if (!shouldShow)
        {
            bar.UpdateBar(enemy.CurrentHP, false, isHeal ? UpdateAnim.Heal : UpdateAnim.Damage);
            bar.gameObject.SetActive(false);
            return;
        }

        if (!bar.gameObject.activeSelf)
            bar.gameObject.SetActive(true);

        bar.UpdateBar(enemy.CurrentHP, false, isHeal ? UpdateAnim.Heal : UpdateAnim.Damage);
    }

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
            pos.y = modelRenderer.bounds.max.y;
        else
            pos.y += fallbackOffsetY;

        transform.position = pos + Vector3.up * headroomPadding;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null) transform.rotation = mainCam.transform.rotation;
    }
}
