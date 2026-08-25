#if UNITY_EDITOR
using System.Collections.Generic;
using DialogueEditor;
using UnityEditor;
using UnityEngine;

public static class QuestDialogueSetupTool
{
    private const string HasAnyQuest = "HasAnyQuest";
    private const string HasEasyQuest = "HasEasyQuest";
    private const string HasMediumQuest = "HasMediumQuest";
    private const string HasHardQuest = "HasHardQuest";

    [MenuItem("Tower Defense/NPC/Setup Selected NPC Quest Dialogue")]
    public static void SetupSelectedNpc()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Quest Dialogue", "Select the NPC GameObject in the Hierarchy first.", "OK");
            return;
        }

        QuestManager manager = Object.FindAnyObjectByType<QuestManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            GameObject go = new GameObject("QuestManager");
            Undo.RegisterCreatedObjectUndo(go, "Create QuestManager");
            manager = go.AddComponent<QuestManager>();
        }

        QuestDialogueBridge bridge = selected.GetComponent<QuestDialogueBridge>();
        if (bridge == null)
            bridge = Undo.AddComponent<QuestDialogueBridge>(selected);

        bridge.questManager = manager;
        bridge.hasAnyQuestParameter = HasAnyQuest;
        bridge.hasEasyQuestParameter = HasEasyQuest;
        bridge.hasMediumQuestParameter = HasMediumQuest;
        bridge.hasHardQuestParameter = HasHardQuest;
        bridge.autoSyncAvailability = true;

        NPCConversation conversation = selected.GetComponent<NPCConversation>();
        int addedParameters = 0;
        if (conversation != null)
            addedParameters = EnsureQuestAvailabilityParameters(conversation);

        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(manager);
        if (conversation != null)
            EditorUtility.SetDirty(conversation);

        Selection.activeGameObject = selected;
        EditorGUIUtility.PingObject(selected);

        string parameterMessage = conversation != null
            ? $"Quest availability parameters are ready ({addedParameters} newly added)."
            : "NPCConversation was not found on this NPC, so DialogueEditor parameters could not be created.";

        EditorUtility.DisplayDialog(
            "Quest Dialogue Ready",
            "QuestManager and QuestDialogueBridge are ready.\n\n" +
            parameterMessage + "\n\n" +
            "Use these DialogueEditor Bool Conditions:\n" +
            "HasAnyQuest == false  -> Out of quest speech\n" +
            "HasAnyQuest == true   -> Difficulty choices\n" +
            "HasEasyQuest == true  -> Easy option\n" +
            "HasMediumQuest == true -> Medium option\n" +
            "HasHardQuest == true  -> Hard option\n\n" +
            "The values refresh automatically whenever this NPC starts a conversation and after accepting a quest.",
            "OK");
    }

    private static int EnsureQuestAvailabilityParameters(NPCConversation conversation)
    {
        if (conversation == null) return 0;

        Undo.RecordObject(conversation, "Setup Quest Dialogue Parameters");

        // Deserialize first so ParameterList reflects the conversation JSON currently edited by the pack.
        EditableConversation editable = conversation.DeserializeForEditor();
        if (conversation.ParameterList == null)
            conversation.ParameterList = new List<EditableParameter>();

        int added = 0;
        added += EnsureBool(conversation.ParameterList, HasAnyQuest) ? 1 : 0;
        added += EnsureBool(conversation.ParameterList, HasEasyQuest) ? 1 : 0;
        added += EnsureBool(conversation.ParameterList, HasMediumQuest) ? 1 : 0;
        added += EnsureBool(conversation.ParameterList, HasHardQuest) ? 1 : 0;

        // Write the newly ensured parameters back into DialogueEditor's JSON data.
        conversation.Serialize(editable);
        return added;
    }

    private static bool EnsureBool(List<EditableParameter> parameters, string parameterName)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            EditableParameter existing = parameters[i];
            if (existing != null && existing.ParameterName == parameterName)
                return false;
        }

        parameters.Add(new EditableBoolParameter(parameterName) { BoolValue = false });
        return true;
    }
}
#endif
