#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EnemySpawnPortalSetupTool
{
    [MenuItem("Tower Defense/Art/Setup Enemy Spawn Portal")]
    public static void Setup()
    {
        EnemySpawnPortal existing = Object.FindFirstObjectByType<EnemySpawnPortal>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Enemy Spawn Portal", "An EnemySpawnPortal already exists. No duplicate was created.", "OK");
            return;
        }

        WaypointPath path = Object.FindFirstObjectByType<WaypointPath>(FindObjectsInactive.Include);
        if (path == null)
        {
            EditorUtility.DisplayDialog("Enemy Spawn Portal", "No WaypointPath was found in this scene.", "OK");
            return;
        }

        List<Transform> points = path.GetWaypoints();
        if (points == null || points.Count == 0 || points[0] == null)
        {
            EditorUtility.DisplayDialog("Enemy Spawn Portal", "WaypointPath does not have a valid first waypoint.", "OK");
            return;
        }

        GameObject portalGO = new GameObject("EnemySpawnPortal");
        Undo.RegisterCreatedObjectUndo(portalGO, "Create Enemy Spawn Portal");
        portalGO.transform.position = points[0].position + Vector3.up * 2.45f;

        if (points.Count >= 2 && points[1] != null)
        {
            Vector3 towardPath = points[1].position - points[0].position;
            towardPath.y = 0f;
            if (towardPath.sqrMagnitude > 0.001f)
                portalGO.transform.rotation = Quaternion.LookRotation(towardPath.normalized, Vector3.up);
        }

        EnemySpawnPortal portal = Undo.AddComponent<EnemySpawnPortal>(portalGO);
        portal.radius = 2.4f;
        portal.ringCount = 4;
        portal.segments = 64;
        portal.distortion = 0.12f;
        portal.ringWidth = 0.11f;
        portal.rotationSpeed = 35f;
        portal.pulseSpeed = 2.4f;
        portal.pulseAmount = 0.10f;
        portal.outerColor = new Color(5.5f, 0.05f, 0.02f, 1f);
        portal.innerColor = new Color(10f, 0.15f, 0.04f, 1f);
        portal.lightIntensity = 5f;
        portal.lightRange = 8f;
        portal.particlesPerSecond = 32;

        BuildExclusionZone exclusion = Undo.AddComponent<BuildExclusionZone>(portalGO);
        exclusion.radius = 3.2f;
        exclusion.centerOffset = new Vector3(0f, -2.45f, 0f);

        EditorUtility.SetDirty(portal);
        EditorUtility.SetDirty(exclusion);
        Selection.activeGameObject = portalGO;

        EditorUtility.DisplayDialog(
            "Enemy Spawn Portal Ready",
            "Created a red animated dimensional portal at the first enemy waypoint.\n\n" +
            "Enemy spawning logic is unchanged: enemies still spawn exactly at waypoint 1, while the portal is the visual source.\n\n" +
            "A 3.2m BuildExclusionZone was also added so towers cannot be placed through the portal.",
            "OK");
    }
}
#endif
