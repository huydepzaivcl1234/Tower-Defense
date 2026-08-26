#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class NPCQuestLifecycleSetupTool
{
    [MenuItem("Tower Defense/NPC/Setup Selected NPC Idle + Quest Lifecycle")]
    public static void SetupSelectedNpc()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("NPC Quest Lifecycle", "Select the NPC GameObject in the Hierarchy first.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(selected, "Setup NPC Quest Lifecycle");

        NPCQuestLifecycle lifecycle = selected.GetComponent<NPCQuestLifecycle>();
        if (lifecycle == null)
            lifecycle = Undo.AddComponent<NPCQuestLifecycle>(selected);

        NPCDialogueInteractable interactable = selected.GetComponent<NPCDialogueInteractable>();
        if (interactable != null)
            lifecycle.dialogueInteractable = interactable;

        if (lifecycle.visualRoot == null)
        {
            Renderer[] renderers = selected.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0 && renderers[0] != null)
                lifecycle.visualRoot = renderers[0].transform;
            else
                lifecycle.visualRoot = selected.transform;
        }

        QuestDialogueBridge bridge = selected.GetComponent<QuestDialogueBridge>();
        if (bridge == null)
            bridge = Undo.AddComponent<QuestDialogueBridge>(selected);

        bridge.npcLifecycle = lifecycle;
        if (bridge.questManager == null)
            bridge.questManager = Object.FindAnyObjectByType<QuestManager>(FindObjectsInactive.Include);

        EditorUtility.SetDirty(lifecycle);
        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(selected);

        Selection.activeGameObject = selected;
        EditorGUIUtility.PingObject(selected);
        Debug.Log($"NPC idle + quest lifecycle configured on '{selected.name}'. Tune NPCQuestLifecycle in the Inspector.", selected);
    }
}
#endif
