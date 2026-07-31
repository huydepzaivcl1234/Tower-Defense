using UnityEngine;

/// <summary>
/// Optional top-down/angled camera rig for viewing the map: WASD/arrows to pan,
/// scroll wheel to zoom, middle-mouse drag to rotate around the ground point.
/// Attach to the Main Camera. Uses the legacy Input Manager (Input.GetKey/GetAxis) -
/// if your project uses the new Input System package exclusively, see the README's
/// troubleshooting section.
/// </summary>
[RequireComponent(typeof(Camera))]
public class RTSCameraController : MonoBehaviour
{
    [Header("Pan")]
    public float panSpeed = 20f;

    [Header("Zoom")]
    public float zoomSpeed = 15f;

    [Header("Rotate")]
    public float rotateSpeed = 60f;

    [Header("Bounds (world space, X/Z)")]
    public Vector2 xBounds = new Vector2(-30f, 30f);
    public Vector2 zBounds = new Vector2(-30f, 30f);

    private void Update()
    {
        HandlePan();
        HandleZoom();
        HandleRotate();
    }

    private void HandlePan()
    {
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move -= transform.forward;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += transform.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= transform.right;
        move.y = 0f;

        if (move.sqrMagnitude > 0.0001f)
            transform.position += move.normalized * panSpeed * Time.deltaTime;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, xBounds.x, xBounds.y);
        pos.z = Mathf.Clamp(pos.z, zBounds.x, zBounds.y);
        transform.position = pos;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            transform.position += transform.forward * scroll * zoomSpeed;
    }

    private void HandleRotate()
    {
        if (!Input.GetMouseButton(2)) return; // middle-mouse drag
        float h = Input.GetAxis("Mouse X");
        transform.RotateAround(GetGroundPoint(), Vector3.up, h * rotateSpeed * Time.deltaTime);
    }

    private Vector3 GetGroundPoint()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        return ground.Raycast(ray, out float dist) ? ray.GetPoint(dist) : transform.position;
    }
}
