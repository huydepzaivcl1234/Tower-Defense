using UnityEngine;

/// <summary>
/// Lightweight procedural motion for generated world-event visuals.
/// No Animator Controller is required; values are fully editable in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public class WorldEventVisualAnimator : MonoBehaviour
{
    [System.Serializable]
    public class RotatingPart
    {
        public Transform target;
        public Vector3 degreesPerSecond = new Vector3(0f, 30f, 0f);
    }

    [Header("Rotation")]
    public RotatingPart[] rotatingParts;

    [Header("Pulse / Float")]
    public Transform pulseTarget;
    [Min(0f)] public float pulseAmount = 0.06f;
    [Min(0.01f)] public float pulseSpeed = 1.4f;
    public Transform floatTarget;
    [Min(0f)] public float floatHeight = 0.12f;
    [Min(0.01f)] public float floatSpeed = 0.9f;
    public bool useUnscaledTime;

    private Vector3 pulseBaseScale;
    private Vector3 floatBaseLocalPosition;
    private bool captured;

    private void Awake() => Capture();
    private void OnEnable() => Capture();

    public void Recapture()
    {
        captured = false;
        Capture();
    }

    private void Capture()
    {
        if (pulseTarget != null) pulseBaseScale = pulseTarget.localScale;
        if (floatTarget != null) floatBaseLocalPosition = floatTarget.localPosition;
        captured = true;
    }

    private void Update()
    {
        if (!captured) Capture();
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        if (rotatingParts != null)
        {
            for (int i = 0; i < rotatingParts.Length; i++)
            {
                RotatingPart part = rotatingParts[i];
                if (part == null || part.target == null) continue;
                part.target.Rotate(part.degreesPerSecond * dt, Space.Self);
            }
        }

        if (pulseTarget != null && pulseAmount > 0f)
        {
            float scale = 1f + Mathf.Sin(t * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
            pulseTarget.localScale = pulseBaseScale * scale;
        }

        if (floatTarget != null && floatHeight > 0f)
        {
            float y = Mathf.Sin(t * floatSpeed * Mathf.PI * 2f) * floatHeight;
            floatTarget.localPosition = floatBaseLocalPosition + Vector3.up * y;
        }
    }
}
