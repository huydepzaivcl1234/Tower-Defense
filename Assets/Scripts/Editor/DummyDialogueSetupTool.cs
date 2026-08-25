#if UNITY_EDITOR
using DialogueEditor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click setup for the currently selected NPC/Dummy.
/// Keeps DialogueEditor's own conversation authoring workflow intact.
/// </summary>
public static class DummyDialogueSetupTool
{
    private const string ConversationManagerPrefabPath = "Assets/DialogueEditor/ConversationManager.prefab";

    [MenuItem("Tower Defense/NPC/Setup Selected Dummy Dialogue")]
    public static void SetupSelectedDummy()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("NPC Dialogue Setup", "Select your Dummy/NPC GameObject in the Hierarchy first.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(selected, "Setup NPC Dialogue");

        NPCConversation conversation = selected.GetComponent<NPCConversation>();
        if (conversation == null)
            conversation = Undo.AddComponent<NPCConversation>(selected);

        NPCDialogueInteractable interactable = selected.GetComponent<NPCDialogueInteractable>();
        if (interactable == null)
            interactable = Undo.AddComponent<NPCDialogueInteractable>(selected);

        interactable.conversation = conversation;

        EnsureRootCollider(selected);
        ConversationManager manager = EnsureConversationManager();
        if (manager != null)
        {
            Undo.RecordObject(manager, "Configure DialogueEditor Conversation Manager");
            manager.ScrollText = true;
            manager.AllowMouseInteraction = true;
            EditorUtility.SetDirty(manager);
        }

        EditorUtility.SetDirty(selected);
        EditorUtility.SetDirty(conversation);
        EditorUtility.SetDirty(interactable);

        Selection.activeGameObject = selected;
        EditorGUIUtility.PingObject(selected);

        EditorUtility.DisplayDialog(
            "NPC Dialogue Setup",
            "Setup complete for '" + selected.name + "'.\n\n" +
            "1. Use the NPCConversation component / Dialogue Editor to write your own dialogue.\n" +
            "2. Assign one or more SHORT blip clips to NPCDialogueInteractable > Voice Clips.\n" +
            "3. Tune Voice Volume, Pitch Min/Max and Play Every N Characters.\n" +
            "4. DialogueEditor Scroll Text + mouse interaction were enabled automatically.\n\n" +
            "Click the NPC in Play Mode to start talking.",
            "OK");
    }

    [MenuItem("Tower Defense/NPC/Setup Selected Dummy Dialogue", true)]
    private static bool ValidateSetupSelectedDummy()
    {
        return Selection.activeGameObject != null && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static ConversationManager EnsureConversationManager()
    {
        ConversationManager existing = Object.FindAnyObjectByType<ConversationManager>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConversationManagerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[NPC Dialogue Setup] DialogueEditor ConversationManager prefab was not found at: " + ConversationManagerPrefabPath);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return null;

        Undo.RegisterCreatedObjectUndo(instance, "Create DialogueEditor ConversationManager");
        instance.name = "ConversationManager";
        return instance.GetComponent<ConversationManager>();
    }

    private static void EnsureRootCollider(GameObject npc)
    {
        if (npc.GetComponent<Collider>() != null)
            return;

        BoxCollider collider = Undo.AddComponent<BoxCollider>(npc);

        Renderer[] renderers = npc.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = npc.transform.InverseTransformPoint(worldBounds.center);
        Vector3 localSize = npc.transform.InverseTransformVector(worldBounds.size);

        collider.center = localCenter;
        collider.size = new Vector3(
            Mathf.Abs(localSize.x),
            Mathf.Abs(localSize.y),
            Mathf.Abs(localSize.z));
    }
}
#endif
