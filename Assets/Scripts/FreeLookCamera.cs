using UnityEngine;

/// <summary>
/// Free-fly camera with full 360-degree mouse look. Press Shift to lock the cursor and enter
/// fly mode (mouse controls look direction, WASD + Space/Ctrl fly through the map in 3D);
/// press Shift again to unlock the cursor and go back to normal clicking (build towers, use UI).
///
/// Cursor locking is only allowed during active gameplay. Any menu/pause/end-game state
/// automatically releases the cursor so UI can always be interacted with safely.
///
/// Attach to Main Camera INSTEAD OF RTSCameraController - if both are enabled at once their
/// WASD handling will fight each other. Disable/remove RTSCameraController first.
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

    private float yaw;
    private float pitch;
    private bool isLocked;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = NormalizeAngle(angles.x);
        SetLocked(false); // start unlocked so build-menu / tower clicking works immediately
    }

    private void Update()
    {
        bool gameplayInputAllowed = IsGameplayInputAllowed();

        // If any menu opens while the cursor is locked, release it immediately.
        if (!gameplayInputAllowed)
        {
            if (isLocked || Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                SetLocked(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            SetLocked(!isLocked);

        if (!isLocked) return; // cursor is free for clicking right now - don't fly/look

        HandleLook();
        HandleMove();
    }

    private bool IsGameplayInputAllowed()
    {
        MainMenuController mainMenu = MainMenuController.Instance;

        if (requireGameplayStarted)
        {
            // If the menu controller exists, gameplay must explicitly be in its started state.
            if (mainMenu != null && !mainMenu.GameplayStarted)
                return false;
        }

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

        if (blockWhilePaused && Time.timeScale <= 0f)
            return false;

        return true;
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
        // always release the cursor if this gets turned off mid-game, so the player isn't stuck
        SetLocked(false);
    }
}
