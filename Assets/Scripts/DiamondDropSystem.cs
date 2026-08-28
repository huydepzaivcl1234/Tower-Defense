using UnityEngine;

/// <summary>
/// Independent Diamond drop system.
/// EnemyData owns only chance/amount; this component owns global Diamond world-drop placement/prefab.
/// Model, pickup SFX/VFX and authored bounce animation belong on the Diamond drop prefab itself.
/// </summary>
[DisallowMultipleComponent]
public class DiamondDropSystem : MonoBehaviour
{
    [Header("Diamond Drop Prefab")]
    [Tooltip("The single default Diamond world-drop prefab. Put the 3D model, DiamondDropPickup, SFX/VFX and WorldDropBounceAnimator on this prefab.")]
    public GameObject diamondDropPrefab;

    [Header("World Placement")]
    [Tooltip("Final resting Y offset relative to the enemy death position.")]
    public float groundYOffset = 0.2f;
    [Tooltip("Additional spawn offset for the guaranteed Boss Diamond drop so it does not perfectly overlap the normal drop.")]
    public Vector3 bossDropSpawnOffset = new Vector3(0.4f, 0f, 0.2f);

    [Header("Fallback Visual - Only When No Prefab Is Assigned")]
    public Vector3 fallbackScale = Vector3.one * 0.35f;
    public Color fallbackColor = new Color(0.25f, 0.9f, 1f, 1f);

    [Header("Fallback Bounce - Only When Prefab Has No Animator")]
    [Min(0f)] public float fallbackPopHeight = 1.8f;
    [Min(0f)] public float fallbackScatterRadius = 0.8f;
    [Range(0, 6)] public int fallbackBounceCount = 2;
    [Min(0f)] public float fallbackFirstBounceHeight = 0.38f;

    private void OnEnable() => Enemy.OnAnyEnemyDied += HandleEnemyDied;
    private void OnDisable() => Enemy.OnAnyEnemyDied -= HandleEnemyDied;

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null || enemy.data == null)
            return;

        EnemyData data = enemy.data;
        Vector3 deathPosition = enemy.transform.position;

        if (data.diamondDropChance > 0f && UnityEngine.Random.value < Mathf.Clamp01(data.diamondDropChance))
            SpawnDrop(deathPosition, RollAmount(data.diamondDropMin, data.diamondDropMax));

        if (data.isBoss && data.bossGuaranteedDiamonds)
            SpawnDrop(deathPosition + bossDropSpawnOffset, RollAmount(data.bossDiamondMin, data.bossDiamondMax));
    }

    private static int RollAmount(int min, int max)
    {
        int safeMin = Mathf.Max(0, Mathf.Min(min, max));
        int safeMax = Mathf.Max(safeMin, Mathf.Max(min, max));
        return safeMax <= safeMin ? safeMin : UnityEngine.Random.Range(safeMin, safeMax + 1);
    }

    private void SpawnDrop(Vector3 deathPosition, int amount)
    {
        if (amount <= 0)
            return;

        Vector3 groundPosition = deathPosition + Vector3.up * groundYOffset;
        GameObject go = diamondDropPrefab != null
            ? Instantiate(diamondDropPrefab, deathPosition, Quaternion.identity)
            : CreateFallback(deathPosition);

        DiamondDropPickup pickup = go.GetComponent<DiamondDropPickup>();
        if (pickup == null)
            pickup = go.AddComponent<DiamondDropPickup>();

        WorldDropBounceAnimator bounce = go.GetComponent<WorldDropBounceAnimator>();
        if (bounce == null)
        {
            bounce = go.AddComponent<WorldDropBounceAnimator>();
            bounce.popHeight = fallbackPopHeight;
            bounce.horizontalScatterRadius = fallbackScatterRadius;
            bounce.bounceCount = fallbackBounceCount;
            bounce.firstBounceHeight = fallbackFirstBounceHeight;
        }

        // Existing prefab-authored pickup/SFX/VFX/bounce values are preserved.
        pickup.bounceAnimator = bounce;
        pickup.Configure(amount, groundPosition);
    }

    private GameObject CreateFallback(Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "DiamondDrop_Fallback";
        go.transform.position = position;
        go.transform.localScale = fallbackScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = fallbackColor;
                renderer.sharedMaterial = material;
            }
        }

        return go;
    }
}
