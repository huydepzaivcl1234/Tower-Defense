using UnityEngine;
using TMPro;

/// <summary>
/// A small floating damage number that rises and fades out, then returns to the pool.
/// Set this up as a prefab: an empty GameObject with a child World Space Canvas
/// (scaled down, e.g. 0.01) containing a TMP_Text - same pattern as WorldHealthBar.
/// Assign the prefab to Enemy's "Damage Popup Prefab" field; Enemy spawns one on every hit.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    public TMP_Text label;
    public float floatSpeed = 1.2f;
    public float lifetime = 0.8f;

    private float timer;
    private Color startColor = Color.white;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        if (label != null) startColor = label.color;
    }

    // Fires every time this becomes active - both the first time and every time it's reused from the pool -
    // so leftover state from a previous use never carries over.
    private void OnEnable()
    {
        timer = 0f;
        if (label != null) label.color = startColor;
    }

    public void SetDamage(float amount)
    {
        if (label != null) label.text = Mathf.RoundToInt(amount).ToString();
    }

    public void SetGoldText(int amount, Color color)
    {
        if (label != null)
        {
            label.text = amount.ToString();
            startColor = color;
            label.color = color;
        }
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
        if (mainCam != null) transform.rotation = mainCam.transform.rotation; // billboard toward camera, same trick as WorldHealthBar

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);
        if (label != null)
            label.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);

        if (timer >= lifetime)
        {
            if (ObjectPool.Instance != null) ObjectPool.Instance.Release(gameObject);
            else Destroy(gameObject);
        }
    }
}