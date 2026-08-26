#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WorldEventGeneratedAssetsMenu
{
    private const string EventFolder = "Assets/WorldEvents";

    [MenuItem("Tower Defense/Event/Generate Event Icons, SFX & Models")]
    public static void Generate()
    {
        WorldEventData dogCat = AssetDatabase.LoadAssetAtPath<WorldEventData>(EventFolder + "/DogCatRain.asset");
        WorldEventData meteor = AssetDatabase.LoadAssetAtPath<WorldEventData>(EventFolder + "/MeteorShower.asset");
        WorldEventData holy = AssetDatabase.LoadAssetAtPath<WorldEventData>(EventFolder + "/HolyLight.asset");

        if (dogCat == null || meteor == null || holy == null)
        {
            EditorUtility.DisplayDialog(
                "World Event Data Missing",
                "Run Tower Defense > Event > Setup World Event System first so DogCatRain, MeteorShower and HolyLight data assets exist.",
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
            "Generated missing fallback assets without overwriting your custom assignments:\n\n" +
            "• 3 event icons\n" +
            "• Dog & Cat Rain announcement SFX + gold paw 3D prefab\n" +
            "• Meteor Shower announcement SFX + molten meteor 3D prefab\n" +
            "• Holy Light blessing SFX + collapse SFX + holy beam/halo 3D prefab\n\n" +
            "Assets are stored under Assets/WorldEvents/Generated.",
            "OK");
    }
}
#endif
