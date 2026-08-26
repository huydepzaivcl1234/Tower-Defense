#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WorldEventGeneratedAssetsMenu
{
    [MenuItem("Tower Defense/Event/Generate Event Icons, SFX & Models")]
    public static void Generate()
    {
        WorldEventManager manager = Object.FindAnyObjectByType<WorldEventManager>(FindObjectsInactive.Include);
        WorldEventData dogCat = WorldEventAssetResolver.Resolve(manager, WorldEventType.DogCatRain);
        WorldEventData meteor = WorldEventAssetResolver.Resolve(manager, WorldEventType.MeteorShower);
        WorldEventData holy = WorldEventAssetResolver.Resolve(manager, WorldEventType.HolyLight);

        if (dogCat == null || meteor == null || holy == null)
        {
            EditorUtility.DisplayDialog(
                "World Event Data Missing",
                "One or more event types are missing. Run Tower Defense > Event > Setup World Event System first.",
                "OK");
            return;
        }

        WorldEventGeneratedAssets.EnsureAndAssign(dogCat, meteor, holy);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = dogCat;
        EditorGUIUtility.PingObject(dogCat);

        EditorUtility.DisplayDialog(
            "Event Assets Ready",
            "Filled only missing icon/SFX/model fields on the event data actually used by WorldEventManager.\n\n" +
            "Dog: " + WorldEventAssetResolver.Describe(dogCat) + "\n" +
            "Meteor: " + WorldEventAssetResolver.Describe(meteor) + "\n" +
            "Holy: " + WorldEventAssetResolver.Describe(holy) + "\n\n" +
            "Your custom assignments are never overwritten.",
            "OK");
    }
}
#endif
