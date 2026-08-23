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
            existing.ApplyGreenReferencePreset();
            existing.Rebuild();
            EditorUtility.SetDirty(existing);
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog(
                "Enemy Spawn Portal",
                "Existing EnemySpawnPortal found and upgraded to the procedural green liquid-swirl style. No duplicate was created.",
                "OK");
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
        portal.verticalScale = 1.28f;
        portal.swirlSpeed = 1.2f;
        portal.swirlStrength = 5.5f;
        portal.edgeWobble = 0.075f;
        portal.emissionStrength = 2.2f;
        portal.useReferenceGreenPreset = true;
        portal.lightIntensity = 4.5f;
        portal.lightRange = 8f;
        portal.particlesPerSecond = 20;
        portal.particleLifetime = 0.65f;
        portal.particleSize = 0.075f;
        portal.ApplyGreenReferencePreset();
        portal.Rebuild();

        BuildExclusionZone exclusion = Undo.AddComponent<BuildExclusionZone>(portalGO);
        exclusion.radius = 3.2f;
        exclusion.centerOffset = new Vector3(0f, -2.45f, 0f);

        EditorUtility.SetDirty(portal);
        EditorUtility.SetDirty(exclusion);
        Selection.activeGameObject = portalGO;

        EditorUtility.DisplayDialog(
            "Enemy Spawn Portal Ready",
            "Created a procedural green liquid-vortex portal at the first enemy waypoint.\n\n" +
            "No portal image/texture is used: the swirl, irregular rim, glow and specks are generated in Unity.\n\n" +
            "Enemy spawning logic is unchanged and the 3.2m no-build zone is preserved.",
            "OK");
    }
}
#endif
