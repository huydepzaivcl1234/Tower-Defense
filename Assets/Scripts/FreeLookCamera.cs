using UnityEngine;

/// <summary>
/// Free-fly camera with full 360-degree mouse look. Press Shift to lock the cursor and enter
/// fly mode (mouse controls look direction, WASD + Space/Ctrl fly through the map in 3D);
/// press Shift again to unlock the cursor and go back to normal clicking (build towers, use UI).
///
/// Cursor locking is only allowed during active gameplay. Any menu/pause/end-game/relic-choice
/// or DialogueEditor conversation state automatically releases the cursor.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FreeLookCamera : MonoBehaviour
{
    [Header("Look")]
    public float lookSensitivity = 3f;
    [Tooltip("Clamp so you can't flip upside down")]
    public float minPitch = -89f;
    public float maxPitch = 89f;

    [Header("Move")]
    public float moveSpeed = 15f;
    public KeyCode upKey = KeyCode.Space;
    public KeyCode downKey = KeyCode.LeftControl;

    [Header("Gameplay Lock Rules")]
    [Tooltip("Only allow Shift cursor lock while gameplay has actually started.")]
    public bool requireGameplayStarted = true;
    [Tooltip("Block/unlock free-look whenever gameplay is paused (Time.timeScale <= 0).")]
    public bool blockWhilePaused = true;
    [Tooltip("Block/unlock free-look while main/settings menus are visible.")]
    public bool blockWhileMenuVisible = true;
    [Tooltip("Block/unlock free-look while the pause menu is active.")]
    public bool blockWhilePauseMenuVisible = true;
    [Tooltip("Block/unlock free-look while the Win/Lose screen is active.")]
    public bool blockWhileEndGameVisible = true;
    [Tooltip("Block/unlock free-look while the relic choice screen is active.")]
    public bool blockWhileRelicChoiceVisible = true;
    [Tooltip("Block/unlock free-look while a DialogueEditor conversation/UI is active.")]
    public bool blockWhileDialogueVisible = true;

    private float yaw;
    private float pitch;
    private bool isLocked;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = NormalizeAngle(angles.x);
        SetLocked(false);
    }

    private void Update()
    {
        bool gameplayInputAllowed = IsGameplayInputAllowed();

        if (!gameplayInputAllowed)
        {
            if (isLocked || Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                SetLocked(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            SetLocked(!isLocked);

        if (!isLocked) return;

        HandleLook();
        HandleMove();
    }

    private bool IsGameplayInputAllowed()
    {
        MainMenuController mainMenu = MainMenuController.Instance;

        if (requireGameplayStarted && mainMenu != null && !mainMenu.GameplayStarted)
            return false;

        if (blockWhileMenuVisible && mainMenu != null && mainMenu.IsAnyMenuVisible)
            return false;

        if (blockWhilePauseMenuVisible && PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
            return false;

        if (blockWhileEndGameVisible && EndGameUIController.Instance != null)
        {
            GameObject endGameRoot = EndGameUIController.Instance.rootPanel;
            if (endGameRoot != null && endGameRoot.activeInHierarchy)
                return false;
        }

        if (blockWhileRelicChoiceVisible && RelicChoiceUI.Instance != null && RelicChoiceUI.Instance.IsVisible)
            return false;

        if (blockWhileDialogueVisible && IsDialogueUIActive())
            return false;

        if (blockWhilePaused && Time.timeScale <= 0f)
            return false;

        return true;
    }

    private static bool IsDialogueUIActive()
    {
        DialogueEditor.ConversationManager manager = DialogueEditor.ConversationManager.Instance;
        if (manager == null)
            return false;

        if (manager.IsConversationActive)
            return true;

        if (manager.DialoguePanel != null && manager.DialoguePanel.gameObject.activeInHierarchy)
            return true;

        if (manager.OptionsPanel != null && manager.OptionsPanel.gameObject.activeInHierarchy)
            return true;

        return false;
    }

    private void SetLocked(bool value)
    {
        isLocked = value;
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    private void HandleLook()
    {
        yaw += Input.GetAxis("Mouse X") * lookSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMove()
    {
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(upKey)) move += Vector3.up;
        if (Input.GetKey(downKey)) move += Vector3.down;

        if (move.sqrMagnitude > 0.0001f)
            transform.position += move.normalized * moveSpeed * Time.deltaTime;
    }

    private float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;

    private void OnDisable()
    {
        SetLocked(false);
    }
}
