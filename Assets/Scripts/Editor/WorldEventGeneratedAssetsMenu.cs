#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WorldEventGeneratedAssetsMenu
{
    private const string EventFolder = "Assets/WorldEvents";

    [MenuItem("Tower Defense/Event/Generate Event SFX & Models")]
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
            "Event SFX & Models Ready",
            "Generated and assigned:\n\n" +
            "• Dog & Cat Rain: bright coin/paw announcement SFX + stylized glowing gold paw drop prefab\n" +
            "• Meteor Shower: rumble/impact announcement SFX + molten meteor prefab with rock shell and fire trail\n" +
            "• Holy Light: layered blessing chord SFX + glowing beam/halo blessing prefab\n\n" +
            "Generated assets are stored under Assets/WorldEvents/Generated. Existing custom assignments are never overwritten.",
            "OK");
    }
}
#endif
