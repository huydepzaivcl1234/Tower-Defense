using UnityEngine;

/// <summary>
/// Free-fly camera with full 360-degree mouse look. Press Shift to lock the cursor and enter
/// fly mode (mouse controls look direction, WASD + Space/Ctrl fly through the map in 3D);
/// press Shift again to unlock the cursor and go back to normal clicking (build towers, use UI).
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
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            SetLocked(!isLocked);

        if (!isLocked) return; // cursor is free for clicking right now - don't fly/look

        HandleLook();
        HandleMove();
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
