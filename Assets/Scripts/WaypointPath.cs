using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines the route enemies walk. Put this on an empty "Path" GameObject and either:
///  (a) drag ordered child waypoint transforms into the Waypoints list, or
///  (b) leave the list empty and just create child empty GameObjects in walk order -
///      they will be auto-collected in hierarchy order.
/// Draws a gizmo line in the Scene view so you can see the path while building the map.
/// </summary>
public class WaypointPath : MonoBehaviour
{
    [Tooltip("Ordered waypoints from spawn to goal. Leave empty to auto-use this object's children.")]
    public List<Transform> waypoints = new List<Transform>();

    [Header("Free Placement")]
    [Tooltip("Minimum distance a tower must be from the path (ground/XZ distance) to be placeable. " +
             "Tune this to roughly match how wide the path looks on your map.")]
    public float pathExclusionRadius = 2f;

    private void Awake()
    {
        if (waypoints == null || waypoints.Count == 0)
            waypoints = CollectChildren();
    }

    private List<Transform> CollectChildren()
    {
        List<Transform> list = new List<Transform>();
        foreach (Transform child in transform)
            list.Add(child);
        return list;
    }

    public List<Transform> GetWaypoints()
    {
        if (waypoints == null || waypoints.Count == 0)
            waypoints = CollectChildren();
        return waypoints;
    }

    public Vector3 GetSpawnPosition()
    {
        var pts = GetWaypoints();
        return pts.Count > 0 ? pts[0].position : transform.position;
    }

    /// <summary>Shortest ground-plane (XZ) distance from a point to any segment of the path.</summary>
    public float DistanceToPath(Vector3 point)
    {
        List<Transform> pts = GetWaypoints();
        float minDist = float.MaxValue;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            if (pts[i] == null || pts[i + 1] == null) continue;
            float d = DistancePointToSegmentXZ(point, pts[i].position, pts[i + 1].position);
            if (d < minDist) minDist = d;
        }
        return pts.Count >= 2 ? minDist : float.MaxValue;
    }

    /// <summary>True if a point is too close to the path to build on (inside Path Exclusion Radius).</summary>
    public bool IsTooCloseToPath(Vector3 point) => DistanceToPath(point) < pathExclusionRadius;

    private static float DistancePointToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
    {
        point.y = 0f; a.y = 0f; b.y = 0f; // compare on the ground plane only, ignore height differences
        Vector3 ab = b - a;
        float t = ab.sqrMagnitude > 0.0001f ? Vector3.Dot(point - a, ab) / ab.sqrMagnitude : 0f;
        t = Mathf.Clamp01(t);
        Vector3 closest = a + ab * t;
        return Vector3.Distance(point, closest);
    }

    private void OnDrawGizmos()
    {
        List<Transform> pts = (waypoints != null && waypoints.Count > 0) ? waypoints : CollectChildren();
        Gizmos.color = Color.yellow;
        for (int i = 0; i < pts.Count; i++)
        {
            if (pts[i] == null) continue;
            Gizmos.DrawSphere(pts[i].position, 0.35f);
            if (i < pts.Count - 1 && pts[i + 1] != null)
                Gizmos.DrawLine(pts[i].position, pts[i + 1].position);
        }

        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        for (int i = 0; i < pts.Count; i++)
        {
            if (pts[i] == null) continue;
            Gizmos.DrawWireSphere(pts[i].position, pathExclusionRadius); // no-build buffer, for tuning in Scene view
        }
    }
}