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
            ApplyReferencePreset(existing);
            existing.Rebuild();
            EditorUtility.SetDirty(existing);
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog(
                "Enemy Spawn Portal",
                "Existing EnemySpawnPortal upgraded to the layered spiral-mesh URP portal. No duplicate was created.",
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
        ApplyReferencePreset(portal);
        portal.Rebuild();

        BuildExclusionZone exclusion = Undo.AddComponent<BuildExclusionZone>(portalGO);
        exclusion.radius = 3.2f;
        exclusion.centerOffset = new Vector3(0f, -2.45f, 0f);

        EditorUtility.SetDirty(portal);
        EditorUtility.SetDirty(exclusion);
        Selection.activeGameObject = portalGO;

        EditorUtility.DisplayDialog(
            "Enemy Spawn Portal Ready",
            "Created the layered green portal at waypoint 1:\n\n" +
            "Dark Background + Outer Ring + Green Spiral + Bright Spiral + Edge Wave + Sparks.\n\n" +
            "The spiral mesh and radial UV are generated in Unity; no portal image is used. Enemy spawn logic and the 3.2m no-build zone are unchanged.",
            "OK");
    }

    private static void ApplyReferencePreset(EnemySpawnPortal portal)
    {
        portal.radius = 2.4f;
        portal.verticalScale = 1.28f;
        portal.angularSegments = 72;
        portal.radialSegments = 14;
        portal.meshTwistTurns = 0.72f;
        portal.centerDepth = 0.28f;

        portal.swirlSpeed = 1.0f;
        portal.swirlStrength = 5.5f;
        portal.edgeWobble = 0.075f;

        portal.darkCoreEmission = 0.85f;
        portal.ringEmission = 2.2f;
        portal.greenSpiralEmission = 2.45f;
        portal.brightSpiralEmission = 4.8f;
        portal.edgeWaveEmission = 3.2f;

        portal.greenErosion = 1.7f;
        portal.brightErosion = 5.4f;
        portal.maskErosion = 1.25f;

        portal.useReferenceGreenPreset = true;
        portal.darkColor = new Color(0.002f, 0.12f, 0.006f, 1f);
        portal.greenColor = new Color(0.025f, 1.65f, 0.01f, 1f);
        portal.limeColor = new Color(0.34f, 4.8f, 0.015f, 1f);
        portal.highlightColor = new Color(2.8f, 7.0f, 0.65f, 1f);

        portal.lightIntensity = 4.5f;
        portal.lightRange = 8f;
        portal.particlesPerSecond = 20;
        portal.particleLifetime = 0.65f;
        portal.particleSize = 0.075f;
    }
}
#endif
