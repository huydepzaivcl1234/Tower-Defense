#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class QuestDialogueSetupTool
{
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
        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(manager);

        Selection.activeGameObject = selected;
        EditorGUIUtility.PingObject(selected);

        EditorUtility.DisplayDialog(
            "Quest Dialogue Ready",
            "QuestManager is ready and QuestDialogueBridge is attached to the selected NPC.\n\nIn DialogueEditor, wire Easy/Medium/Hard Option Events to AcceptEasyQuest / AcceptMediumQuest / AcceptHardQuest.",
            "OK");
    }
}
#endif
