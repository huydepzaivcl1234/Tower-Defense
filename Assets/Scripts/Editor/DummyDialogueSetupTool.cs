#if UNITY_EDITOR
using System.Collections.Generic;
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

            SetupPresentation(manager);

            if (manager.DialoguePanel != null)
                manager.DialoguePanel.gameObject.SetActive(false);
            if (manager.OptionsPanel != null)
                manager.OptionsPanel.gameObject.SetActive(false);
        }

        EditorUtility.SetDirty(selected);
        EditorUtility.SetDirty(conversation);
        EditorUtility.SetDirty(interactable);

        Selection.activeGameObject = selected;
        EditorGUIUtility.PingObject(selected);

        EditorUtility.DisplayDialog(
            "NPC Dialogue Setup",
            "Setup complete for '" + selected.name + "'.\n\n" +
            "- Click NPC to start dialogue.\n" +
            "- Gameplay HUD smoothly slides off-screen while talking.\n" +
            "- ESC instantly cancels the dialogue.\n" +
            "- HUD smoothly returns after dialogue.\n" +
            "- Dialogue UI stays hidden while idle.\n\n" +
            "All slide offsets/timing are editable on ConversationManager > DialogueHUDPresentationController.",
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

    private static void SetupPresentation(ConversationManager manager)
    {
        DialogueHUDPresentationController controller = manager.GetComponent<DialogueHUDPresentationController>();
        if (controller == null)
            controller = Undo.AddComponent<DialogueHUDPresentationController>(manager.gameObject);

        Undo.RecordObject(controller, "Configure Dialogue HUD Presentation");
        controller.conversationManager = manager;
        controller.slideOutDuration = 0.35f;
        controller.slideInDuration = 0.35f;
        controller.allowEscapeCancel = true;
        controller.hideDialogueUIWhenIdle = true;

        controller.hudTargets = new List<DialogueHUDPresentationController.HUDSlideTarget>();
        AddTarget(controller, "ResourceHUD", new Vector2(-520f, 0f));
        AddTarget(controller, "WaveHUD", new Vector2(0f, 320f));
        AddTarget(controller, "BuildDock", new Vector2(0f, -320f));
        AddTarget(controller, "UpgradePanelClean", new Vector2(560f, 0f));
        AddTarget(controller, "QoLTopRight", new Vector2(520f, 0f));
        AddTarget(controller, "RelicOwnedHUD", new Vector2(-520f, 0f));

        EditorUtility.SetDirty(controller);
    }

    private static void AddTarget(DialogueHUDPresentationController controller, string objectName, Vector2 hiddenOffset)
    {
        RectTransform target = FindRectTransformByName(objectName);
        if (target == null) return;

        controller.hudTargets.Add(new DialogueHUDPresentationController.HUDSlideTarget
        {
            target = target,
            hiddenOffset = hiddenOffset
        });
    }

    private static RectTransform FindRectTransformByName(string objectName)
    {
        RectTransform[] all = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name == objectName)
                return all[i];
        }
        return null;
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
