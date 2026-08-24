#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BuildExclusionSetupTool
{
    [MenuItem("Tower Defense/Map/Setup Main Base No-Build Zone")]
    public static void SetupMainBaseZone()
    {
        GameObject mainBase = GameObject.Find("MainBase");
        if (mainBase == null)
        {
            EditorUtility.DisplayDialog("Main Base No-Build Zone", "Could not find an active GameObject named MainBase in the scene.", "OK");
            return;
        }

        BuildExclusionZone zone = mainBase.GetComponent<BuildExclusionZone>();
        if (zone == null)
            zone = Undo.AddComponent<BuildExclusionZone>(mainBase);

        Undo.RecordObject(zone, "Configure Main Base No-Build Zone");
        zone.radius = 7.5f;
        zone.centerOffset = Vector3.zero;
        EditorUtility.SetDirty(zone);
        Selection.activeGameObject = mainBase;

        EditorUtility.DisplayDialog(
            "Main Base No-Build Zone Ready",
            "Tower placement is now blocked within 7.5m of MainBase.\n\nSelect MainBase > BuildExclusionZone to change Radius whenever you want.",
            "OK");
    }
}
#endif
