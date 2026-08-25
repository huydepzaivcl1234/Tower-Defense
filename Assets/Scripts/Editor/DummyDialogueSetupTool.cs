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
        EnsureConversationManager();

        EditorUtility.SetDirty(selected);
        EditorUtility.SetDirty(conversation);
        EditorUtility.SetDirty(interactable);

        Selection.activeGameObject = selected;
        EditorGUIUtility.PingObject(selected);

        EditorUtility.DisplayDialog(
            "NPC Dialogue Setup",
            "Setup complete for '" + selected.name + "'.\n\n" +
            "1. Open the NPCConversation component and edit its dialogue with Dialogue Editor.\n" +
            "2. Assign one or more short clips to NPCDialogueInteractable > Voice Clips.\n" +
            "3. Tune Volume / Pitch / Every N Characters in the Inspector.\n\n" +
            "Click the NPC in Play Mode to start talking.",
            "OK");
    }

    [MenuItem("Tower Defense/NPC/Setup Selected Dummy Dialogue", true)]
    private static bool ValidateSetupSelectedDummy()
    {
        return Selection.activeGameObject != null && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void EnsureConversationManager()
    {
        ConversationManager existing = Object.FindAnyObjectByType<ConversationManager>(FindObjectsInactive.Include);
        if (existing != null)
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ConversationManagerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[NPC Dialogue Setup] DialogueEditor ConversationManager prefab was not found at: " + ConversationManagerPrefabPath);
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return;

        Undo.RegisterCreatedObjectUndo(instance, "Create DialogueEditor ConversationManager");
        instance.name = "ConversationManager";
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
