using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight unscaled-time pulse for decorative Aurora UI graphics.
/// Visual-only: no gameplay state or input ownership.
/// </summary>
[DisallowMultipleComponent]
public class AuroraUIEffects : MonoBehaviour
{
    [Header("Target")]
    public Graphic targetGraphic;

    [Header("Alpha Pulse")]
    public bool pulseAlpha = true;
    [Range(0f, 1f)] public float minAlpha = 0.32f;
    [Range(0f, 1f)] public float maxAlpha = 0.78f;
    [Min(0.05f)] public float pulseSpeed = 0.8f;

    [Header("Scale Pulse")]
    public bool pulseScale = false;
    [Range(1f, 1.12f)] public float maxScale = 1.025f;

    private Color baseColor;
    private Vector3 baseScale;

    private void Awake()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        baseScale = transform.localScale;
        if (targetGraphic != null)
            baseColor = targetGraphic.color;
    }

    private void OnEnable()
    {
        baseScale = transform.localScale;
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();
        if (targetGraphic != null)
            baseColor = targetGraphic.color;
    }

    private void Update()
    {
        float t = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);

        if (pulseAlpha && targetGraphic != null)
        {
            Color c = baseColor;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            targetGraphic.color = c;
        }

        if (pulseScale)
            transform.localScale = baseScale * Mathf.Lerp(1f, maxScale, t);
    }

    private void OnDisable()
    {
        transform.localScale = baseScale;
        if (targetGraphic != null)
            targetGraphic.color = baseColor;
    }
}
