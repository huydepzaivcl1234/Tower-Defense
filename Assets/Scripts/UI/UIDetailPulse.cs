using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight visual-only pulse for additive UI decoration.
/// Does not own input, layout, gameplay state or original UI graphics.
/// </summary>
[DisallowMultipleComponent]
public class UIDetailPulse : MonoBehaviour
{
    public Graphic target;
    [Range(0f, 1f)] public float minAlpha = 0.20f;
    [Range(0f, 1f)] public float maxAlpha = 0.65f;
    [Min(0.05f)] public float speed = 0.75f;

    private Color baseColor;

    private void Awake()
    {
        if (target == null) target = GetComponent<Graphic>();
        if (target != null) baseColor = target.color;
    }

    private void OnEnable()
    {
        if (target == null) target = GetComponent<Graphic>();
        if (target != null) baseColor = target.color;
    }

    private void Update()
    {
        if (target == null) return;
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f);
        Color c = baseColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        target.color = c;
    }

    private void OnDisable()
    {
        if (target != null) target.color = baseColor;
    }
}
