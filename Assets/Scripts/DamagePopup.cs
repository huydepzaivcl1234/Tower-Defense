using UnityEngine;
using TMPro;

/// <summary>
/// Floating combat text used for damage, healing and gold.
/// Supports compact large-number formatting and a small bounce/punch animation.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    public TMP_Text label;
    public float floatSpeed = 1.2f;
    public float lifetime = 0.8f;
    [Tooltip("Extra scale punch at the start. Heal popups use a slightly stronger bounce.")]
    public float bounceStrength = 0.22f;

    [Header("Combat Text Colors")]
    public Color damageColor = new Color(1f, 0.18f, 0.14f, 1f);
    public Color healColor = new Color(0.30f, 1f, 0.36f, 1f);

    private float timer;
    private Color startColor = Color.white;
    private Camera mainCam;
    private Vector3 baseScale = Vector3.one;
    private float activeBounceStrength;

    private void Awake()
    {
        mainCam = Camera.main;
        baseScale = transform.localScale;
        if (label != null) startColor = label.color;
    }

    private void OnEnable()
    {
        timer = 0f;
        activeBounceStrength = bounceStrength;
        transform.localScale = baseScale;
        if (label != null) label.color = startColor;
    }

    public void SetDamage(float amount)
    {
        // Always force damage to red. This also prevents a pooled popup that was
        // previously green (heal) or gold from leaking its old color into damage text.
        startColor = damageColor;
        activeBounceStrength = bounceStrength;
        if (label != null)
        {
            label.text = CompactNumber.Format(amount);
            label.color = startColor;
        }
    }

    public void SetHealText(float amount)
    {
        startColor = healColor;
        activeBounceStrength = bounceStrength * 1.35f;
        if (label != null)
        {
            label.text = "+" + CompactNumber.Format(amount);
            label.color = startColor;
        }
    }

    public void SetGoldText(int amount, Color color)
    {
        startColor = color;
        activeBounceStrength = bounceStrength;
        if (label != null)
        {
            label.text = CompactNumber.Format(amount);
            label.color = color;
        }
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null) transform.rotation = mainCam.transform.rotation;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        // Quick squash/pop near the start, then settle to normal size.
        float bounceWindow = Mathf.Clamp01(1f - t * 4f);
        float pulse = Mathf.Sin(t * Mathf.PI * 4f) * activeBounceStrength * bounceWindow;
        transform.localScale = baseScale * (1f + pulse);

        if (label != null)
            label.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

        if (timer >= lifetime)
        {
            transform.localScale = baseScale;
            if (ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
            else Destroy(gameObject);
        }
    }
}
