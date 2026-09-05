using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual ribbon for the existing WaypointPath. The enemy still follows WaypointPath directly;
/// this component only renders the same route as a flat road surface.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[DisallowMultipleComponent]
public class FlatMapPathSurface : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Existing gameplay path. The rendered road follows these exact waypoints.")]
    public WaypointPath path;

    [Header("Road Shape")]
    [Min(0.5f)]
    [Tooltip("Visible road width in world units.")]
    public float pathWidth = 5f;

    [Tooltip("Small vertical offset above the ground to prevent z-fighting.")]
    public float surfaceYOffset = 0.035f;

    [Header("Generated Mesh")]
    [Tooltip("Generated at edit time by Tower Defense/Map/Rebuild Flat Map Path Surface.")]
    public Mesh generatedMesh;

    public void Rebuild()
    {
        if (path == null)
            return;

        List<Transform> points = path.GetWaypoints();
        if (points == null || points.Count < 2)
            return;

        List<Vector3> valid = new List<Vector3>();
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null)
                valid.Add(transform.InverseTransformPoint(points[i].position));
        }

        if (valid.Count < 2)
            return;

        int count = valid.Count;
        Vector3[] vertices = new Vector3[count * 2];
        Vector2[] uvs = new Vector2[count * 2];
        int[] triangles = new int[(count - 1) * 6];

        float accumulated = 0f;
        for (int i = 0; i < count; i++)
        {
            Vector3 prev = valid[Mathf.Max(0, i - 1)];
            Vector3 current = valid[i];
            Vector3 next = valid[Mathf.Min(count - 1, i + 1)];

            Vector3 tangent;
            if (i == 0)
                tangent = (next - current);
            else if (i == count - 1)
                tangent = (current - prev);
            else
                tangent = (next - prev);

            tangent.y = 0f;
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = Vector3.forward;
            tangent.Normalize();

            Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized * (pathWidth * 0.5f);
            Vector3 center = current + Vector3.up * surfaceYOffset;

            vertices[i * 2] = center - side;
            vertices[i * 2 + 1] = center + side;

            if (i > 0)
                accumulated += Vector3.Distance(valid[i - 1], current);

            float v = accumulated / Mathf.Max(0.01f, pathWidth);
            uvs[i * 2] = new Vector2(0f, v);
            uvs[i * 2 + 1] = new Vector2(1f, v);

            if (i < count - 1)
            {
                int vi = i * 2;
                int ti = i * 6;
                triangles[ti] = vi;
                triangles[ti + 1] = vi + 2;
                triangles[ti + 2] = vi + 1;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + 2;
                triangles[ti + 5] = vi + 3;
            }
        }

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = "FlatMap_PathSurface_Generated"
            };
        }
        else
        {
            generatedMesh.Clear();
        }

        generatedMesh.vertices = vertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        MeshFilter filter = GetComponent<MeshFilter>();
        filter.sharedMesh = generatedMesh;
    }
}
