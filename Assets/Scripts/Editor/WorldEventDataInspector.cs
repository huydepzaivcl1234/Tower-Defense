#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldEventData))]
public class WorldEventDataInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader("Identity");
        Draw("eventName");
        Draw("description");
        Draw("rarity");
        Draw("eventType");
        Draw("selectionWeight");
        Draw("durationRounds");

        EditorGUILayout.Space(8f);
        DrawHeader("Announcement");
        Draw("icon");
        Draw("accentColor");
        Draw("announcementSfx");

        SerializedProperty typeProperty = serializedObject.FindProperty("eventType");
        WorldEventType type = (WorldEventType)typeProperty.enumValueIndex;

        EditorGUILayout.Space(10f);
        switch (type)
        {
            case WorldEventType.DogCatRain:
                DrawDogCat();
                break;
            case WorldEventType.MeteorShower:
                DrawMeteor();
                break;
            case WorldEventType.HolyLight:
                DrawHoly();
                break;
        }

        serializedObject.ApplyModifiedProperties();
        DrawSummary((WorldEventData)target);
    }

    private void DrawDogCat()
    {
        DrawHeader("Dog & Cat Rain");
        Draw("goldPerDrop");
        Draw("goldDropInterval");
        Draw("enemyMaxHpBonusPercent");
        Draw("goldDropPrefab");
        Draw("goldDropAreaSize");
        Draw("goldDropHeight");
        Draw("goldDropFallDuration");
    }

    private void DrawMeteor()
    {
        DrawHeader("Meteor Shower");
        Draw("meteorChancePerTick");
        Draw("meteorTickInterval");
        Draw("meteorTargetEnemyChance");
        Draw("meteorTargetScatterRadius");
        Draw("meteorEnemyMaxHpDamagePercent");
        Draw("meteorTowerAttackSpeedPenaltyPercent");
        Draw("meteorTowerDebuffDuration");
        Draw("meteorPrefab");
        Draw("meteorImpactVfxPrefab");
        Draw("meteorAreaSize");
        Draw("meteorSpawnHeight");
        Draw("meteorFallDuration");
        Draw("meteorHitRadius");
    }

    private void DrawHoly()
    {
        DrawHeader("Holy Light — Blessing");
        Draw("holyTowerAttackSpeedBonusPercent");
        Draw("holyTowerDamageBonusPercent");
        Draw("holyProjectileSpeedBonusPercent");
        Draw("holyCollapseChancePerRound");
        Draw("holyLightVisualPrefab");

        EditorGUILayout.Space(8f);
        DrawHeader("Holy Light — Collapse Penalty");
        Draw("collapsePenaltyRounds");
        Draw("collapseEnemyMaxHpBonusPercent");
        Draw("collapseEnemyCCResistanceBonusPercent");
        Draw("collapseEnemyShieldPercentOfMaxHp");
        Draw("holyCollapseSfx");
        Draw("holyCollapseVfxPrefab");
    }

    private void DrawSummary(WorldEventData data)
    {
        if (data == null)
            return;

        EditorGUILayout.Space(12f);
        string summary;

        switch (data.eventType)
        {
            case WorldEventType.DogCatRain:
                summary = $"{data.durationRounds} round(s) • +{data.goldPerDrop} Gold/drop • " +
                          $"Enemy Max HP +{Percent(data.enemyMaxHpBonusPercent)}";
                break;

            case WorldEventType.MeteorShower:
                summary = $"{data.durationRounds} round(s) • Meteor damage {Percent(data.meteorEnemyMaxHpDamagePercent)} Max HP • " +
                          $"Tower AS -{Percent(data.meteorTowerAttackSpeedPenaltyPercent)} for {data.meteorTowerDebuffDuration:0.##}s";
                break;

            case WorldEventType.HolyLight:
                summary = $"{data.durationRounds} round(s) • Tower AS +{Percent(data.holyTowerAttackSpeedBonusPercent)} • " +
                          $"Damage +{Percent(data.holyTowerDamageBonusPercent)} • Projectile +{Percent(data.holyProjectileSpeedBonusPercent)} • " +
                          $"Collapse {Percent(data.holyCollapseChancePerRound)}/round";
                break;

            default:
                summary = string.Empty;
                break;
        }

        EditorGUILayout.HelpBox(summary, MessageType.Info);

        if (data.eventType == WorldEventType.HolyLight && data.rarity != WorldEventRarity.Rare)
            EditorGUILayout.HelpBox("Holy Light is designed as a Rare event.", MessageType.Warning);
        if (data.eventType != WorldEventType.HolyLight && data.rarity != WorldEventRarity.Common)
            EditorGUILayout.HelpBox("Dog & Cat Rain and Meteor Shower are designed as Common events.", MessageType.Warning);
    }

    private static string Percent(float value)
    {
        return Mathf.RoundToInt(value * 100f) + "%";
    }

    private void Draw(string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, true);
    }

    private static void DrawHeader(string text)
    {
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }
}
#endif
