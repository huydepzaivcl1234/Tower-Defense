using UnityEngine;

/// <summary>
/// Draws a circle on the ground to preview a tower's range. Reused for two things:
///  - TowerPlacementManager's ghost preview while placing a new tower
///  - TowerUpgradeUI showing the live range of whichever tower is currently selected
/// Use TWO separate instances of this (one per use above) so they don't fight over the same ring.
/// Just add this script to an empty GameObject - it adds its own LineRenderer and a safe
/// default material, and starts hidden.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RangeIndicator : MonoBehaviour
{
    [Tooltip("Number of segments used to approximate the circle - higher is smoother")]
    public int segments = 48;
    [Tooltip("Small vertical offset so the ring doesn't Z-fight with the ground")]
    public float yOffset = 0.05f;
    public float lineWidth = 0.1f;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = segments;
        line.widthMultiplier = lineWidth;

        if (line.sharedMaterial == null)
        {
            // Safe fallback so the ring is never invisible/magenta due to a missing material -
            // Sprites/Default ships with both Built-in and URP projects.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) line.material = new Material(shader);
        }

        Hide();
    }

    public void Show(Vector3 center, float radius, Color color)
    {
        gameObject.SetActive(true);
        line.startColor = color;
        line.endColor = color;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, yOffset, Mathf.Sin(angle) * radius);
            line.SetPosition(i, point);
        }
    }

    public void Hide() => gameObject.SetActive(false);
}
