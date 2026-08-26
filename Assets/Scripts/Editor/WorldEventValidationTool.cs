#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WorldEventValidationTool
{
    private const string EventFolder = "Assets/WorldEvents";

    [MenuItem("Tower Defense/Event/Validate World Event System")]
    public static void Validate()
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        WorldEventManager[] managers = Object.FindObjectsByType<WorldEventManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length == 0)
        {
            errors.Add("No WorldEventManager exists in the loaded scene.");
        }
        else if (managers.Length > 1)
        {
            errors.Add($"{managers.Length} WorldEventManager objects exist. Keep exactly one.");
        }

        WorldEventManager manager = managers.Length > 0 ? managers[0] : null;
        if (manager != null)
        {
            if (manager.eventPool == null || manager.eventPool.Count == 0)
                errors.Add("WorldEventManager Event Pool is empty.");
            if (manager.announcementRoot == null)
                errors.Add("Announcement Root is not assigned.");
            if (manager.rarityText == null || manager.titleText == null || manager.descriptionText == null)
                errors.Add("Announcement text references are incomplete.");
            if (manager.iconImage == null)
                warnings.Add("Announcement Icon Image is not assigned.");
            if (manager.eventAudioSource == null)
                warnings.Add("Event Audio Source is not assigned. Runtime will create one as a fallback.");
            if (manager.worldCenter == null)
                warnings.Add("World Center is not assigned. Event world effects will use world origin (0,0,0).");
            if (manager.hudTargets == null || manager.hudTargets.Count == 0)
                warnings.Add("HUD Targets is empty, so gameplay HUD will not slide away during announcements.");
            if (manager.holyDirectionalLight == null)
                warnings.Add("Holy Directional Light is empty. Holy Light gameplay buffs work, but map lighting will not brighten.");
            if (manager.eventChancePerOpportunity <= 0f)
                warnings.Add("Event Chance Per Opportunity is 0%; random events can never start.");
            if (manager.rareChanceWhenEventRolls <= 0f)
                warnings.Add("Rare Chance When Event Rolls is 0%; Holy Light cannot roll as Rare while Common events exist.");
        }

        WorldEventData dog = AssetDatabase.LoadAssetAtPath<WorldEventData>(EventFolder + "/DogCatRain.asset");
        WorldEventData meteor = AssetDatabase.LoadAssetAtPath<WorldEventData>(EventFolder + "/MeteorShower.asset");
        WorldEventData holy = AssetDatabase.LoadAssetAtPath<WorldEventData>(EventFolder + "/HolyLight.asset");

        ValidateData(dog, "DogCatRain.asset", WorldEventType.DogCatRain, WorldEventRarity.Common, errors, warnings);
        ValidateData(meteor, "MeteorShower.asset", WorldEventType.MeteorShower, WorldEventRarity.Common, errors, warnings);
        ValidateData(holy, "HolyLight.asset", WorldEventType.HolyLight, WorldEventRarity.Rare, errors, warnings);

        if (dog != null)
        {
            if (dog.goldDropPrefab == null)
                warnings.Add("Dog & Cat Rain has no Gold Drop Prefab. Gold still works but nothing 3D will fall.");
            if (dog.goldDropInterval <= 0f)
                errors.Add("Dog & Cat Rain Gold Drop Interval must be > 0.");
        }

        if (meteor != null)
        {
            if (meteor.meteorPrefab == null)
                warnings.Add("Meteor Shower has no Meteor Prefab. Gameplay impact still works but falling meteor is invisible.");
            if (meteor.meteorHitRadius <= 0f)
                errors.Add("Meteor Hit Radius must be > 0.");
            if (meteor.meteorTickInterval <= 0f)
                errors.Add("Meteor Tick Interval must be > 0.");
        }

        if (holy != null)
        {
            if (holy.holyLightVisualPrefab == null)
                warnings.Add("Holy Light has no Holy Light Visual Prefab. Buffs still work but world VFX is missing.");
            if (holy.collapsePenaltyRounds < 1)
                errors.Add("Holy Light Collapse Penalty Rounds must be at least 1.");
        }

        string report = BuildReport(errors, warnings);
        if (errors.Count > 0)
            Debug.LogError(report, manager);
        else if (warnings.Count > 0)
            Debug.LogWarning(report, manager);
        else
            Debug.Log(report, manager);

        EditorUtility.DisplayDialog(
            errors.Count == 0 ? "World Event Validation" : "World Event Validation Failed",
            report,
            "OK");
    }

    private static void ValidateData(
        WorldEventData data,
        string assetName,
        WorldEventType expectedType,
        WorldEventRarity expectedRarity,
        List<string> errors,
        List<string> warnings)
    {
        if (data == null)
        {
            errors.Add($"Missing Assets/WorldEvents/{assetName}.");
            return;
        }

        if (data.eventType != expectedType)
            errors.Add($"{assetName} has Event Type {data.eventType}, expected {expectedType}.");
        if (data.rarity != expectedRarity)
            errors.Add($"{assetName} has rarity {data.rarity}, expected {expectedRarity}.");
        if (data.durationRounds < 1)
            errors.Add($"{assetName} Duration Rounds must be at least 1.");
        if (data.selectionWeight <= 0f)
            warnings.Add($"{assetName} Selection Weight is 0, so it cannot be selected.");
        if (data.icon == null)
            warnings.Add($"{assetName} has no event icon.");
        if (data.announcementSfx == null)
            warnings.Add($"{assetName} has no announcement SFX.");
    }

    private static string BuildReport(List<string> errors, List<string> warnings)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine("WORLD EVENT SYSTEM VALIDATION");
        builder.AppendLine();

        if (errors.Count == 0 && warnings.Count == 0)
        {
            builder.AppendLine("PASS — no setup problems found.");
            return builder.ToString();
        }

        if (errors.Count > 0)
        {
            builder.AppendLine($"ERRORS ({errors.Count})");
            for (int i = 0; i < errors.Count; i++)
                builder.AppendLine("• " + errors[i]);
            builder.AppendLine();
        }

        if (warnings.Count > 0)
        {
            builder.AppendLine($"WARNINGS ({warnings.Count})");
            for (int i = 0; i < warnings.Count; i++)
                builder.AppendLine("• " + warnings[i]);
        }

        return builder.ToString();
    }
}
#endif
