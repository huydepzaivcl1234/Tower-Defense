#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaveManager))]
[CanEditMultipleObjects]
public class WaveManagerInspector : Editor
{
    private SerializedProperty pathProperty;
    private SerializedProperty levelOneWavesProperty;
    private SerializedProperty additionalLevelsProperty;

    private void OnEnable()
    {
        pathProperty = serializedObject.FindProperty("path");
        levelOneWavesProperty = serializedObject.FindProperty("waves");
        additionalLevelsProperty = serializedObject.FindProperty("additionalStoryLevels");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((WaveManager)target), typeof(WaveManager), false);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(pathProperty);

        EditorGUILayout.Space(10f);
        DrawStoryLevels();

        EditorGUILayout.Space(10f);
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "path",
            "waves",
            "additionalStoryLevels");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawStoryLevels()
    {
        EditorGUILayout.LabelField("Story Levels", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "All Story levels are edited here. Level 1 keeps the existing Waves data; levels added below keep the existing Additional Story Levels data.",
            MessageType.Info);

        if (serializedObject.isEditingMultipleObjects &&
            (levelOneWavesProperty.hasMultipleDifferentValues || additionalLevelsProperty.hasMultipleDifferentValues))
        {
            EditorGUILayout.HelpBox("Select one WaveManager to edit Story level lists.", MessageType.Warning);
            return;
        }

        DrawLevelOne();

        for (int i = 0; i < additionalLevelsProperty.arraySize; i++)
        {
            if (DrawAdditionalLevel(i))
                return;
        }

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("+ Add Story Level", GUILayout.Height(28f)))
            AddStoryLevel();
    }

    private void DrawLevelOne()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            levelOneWavesProperty.isExpanded = EditorGUILayout.Foldout(
                levelOneWavesProperty.isExpanded,
                $"LEVEL 1  •  STORY LEVEL 1  •  {levelOneWavesProperty.arraySize} WAVES",
                true,
                EditorStyles.foldoutHeader);

            if (!levelOneWavesProperty.isExpanded)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(levelOneWavesProperty, new GUIContent("Waves"), true);
            EditorGUI.indentLevel--;
        }
    }

    private bool DrawAdditionalLevel(int index)
    {
        SerializedProperty levelProperty = additionalLevelsProperty.GetArrayElementAtIndex(index);
        SerializedProperty nameProperty = levelProperty.FindPropertyRelative("levelName");
        SerializedProperty wavesProperty = levelProperty.FindPropertyRelative("waves");
        int displayLevel = index + 2;
        string configuredName = string.IsNullOrWhiteSpace(nameProperty.stringValue)
            ? $"STORY LEVEL {displayLevel}"
            : nameProperty.stringValue;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                levelProperty.isExpanded = EditorGUILayout.Foldout(
                    levelProperty.isExpanded,
                    $"LEVEL {displayLevel}  •  {configuredName}  •  {wavesProperty.arraySize} WAVES",
                    true,
                    EditorStyles.foldoutHeader);

                using (new EditorGUI.DisabledScope(index <= 0))
                {
                    if (GUILayout.Button("▲", GUILayout.Width(28f)))
                    {
                        additionalLevelsProperty.MoveArrayElement(index, index - 1);
                        return true;
                    }
                }

                using (new EditorGUI.DisabledScope(index >= additionalLevelsProperty.arraySize - 1))
                {
                    if (GUILayout.Button("▼", GUILayout.Width(28f)))
                    {
                        additionalLevelsProperty.MoveArrayElement(index, index + 1);
                        return true;
                    }
                }

                if (GUILayout.Button("Remove", GUILayout.Width(64f)) &&
                    EditorUtility.DisplayDialog(
                        "Remove Story Level",
                        $"Remove Level {displayLevel} and all of its configured waves?",
                        "Remove",
                        "Cancel"))
                {
                    additionalLevelsProperty.DeleteArrayElementAtIndex(index);
                    return true;
                }
            }

            if (!levelProperty.isExpanded)
                return false;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(nameProperty, new GUIContent("Level Name"));
            EditorGUILayout.PropertyField(wavesProperty, new GUIContent("Waves"), true);
            EditorGUI.indentLevel--;
        }

        return false;
    }

    private void AddStoryLevel()
    {
        int newIndex = additionalLevelsProperty.arraySize;
        additionalLevelsProperty.arraySize++;

        SerializedProperty levelProperty = additionalLevelsProperty.GetArrayElementAtIndex(newIndex);
        levelProperty.FindPropertyRelative("levelName").stringValue = $"Story Level {newIndex + 2}";
        levelProperty.FindPropertyRelative("waves").arraySize = 0;
        levelProperty.isExpanded = true;
    }
}
#endif
