using UnityEngine;

/// <summary>
/// Marks a location on the map where a tower can be built. Put this on a flat marker
/// object (e.g. a squashed cylinder) with a Collider so it can be clicked.
/// </summary>
public class TowerPlacementSpot : MonoBehaviour
{
    public static event System.Action<TowerPlacementSpot> OnSpotClicked;

    [Tooltip("Exact spawn point for the tower. Defaults to this transform if empty.")]
    public Transform buildPoint;

    public bool IsOccupied { get; private set; }

    public Vector3 BuildPosition => buildPoint != null ? buildPoint.position : transform.position;

    public void MarkOccupied()
    {
        IsOccupied = true;
        SetColliderEnabled(false); // stop this spot's collider from competing with the tower's own collider for clicks
    }

    public void ClearSpot()
    {
        IsOccupied = false;
        SetColliderEnabled(true);
    }

    private void SetColliderEnabled(bool value)
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = value;
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (IsOccupied) return;
        OnSpotClicked?.Invoke(this);
    }
}