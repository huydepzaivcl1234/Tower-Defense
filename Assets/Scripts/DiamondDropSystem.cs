using UnityEngine;

/// <summary>
/// Listens to the existing Enemy death event and creates Diamond world drops from EnemyData.
/// </summary>
[DisallowMultipleComponent]
public class DiamondDropSystem : MonoBehaviour
{
    [Header("Fallback Drop Presentation")]
    public Vector3 fallbackScale = Vector3.one * 0.35f;
    public Color fallbackColor = new Color(0.25f, 0.9f, 1f, 1f);

    [Header("Fallback Bounce Defaults")]
    [Min(0f)] public float popHeight = 1.8f;
    [Min(0f)] public float scatterRadius = 0.8f;
    [Range(0, 6)] public int bounceCount = 2;
    [Min(0f)] public float firstBounceHeight = 0.38f;

    private void OnEnable() => Enemy.OnAnyEnemyDied += HandleEnemyDied;
    private void OnDisable() => Enemy.OnAnyEnemyDied -= HandleEnemyDied;

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy == null || enemy.data == null)
            return;

        EnemyData data = enemy.data;
        Vector3 deathPosition = enemy.transform.position;

        if (data.diamondDropChance > 0f && UnityEngine.Random.value < Mathf.Clamp01(data.diamondDropChance))
            SpawnDrop(data, deathPosition, RollAmount(data.diamondDropMin, data.diamondDropMax));

        if (data.isBoss && data.bossGuaranteedDiamonds)
            SpawnDrop(data, deathPosition + new Vector3(0.4f, 0f, 0.2f), RollAmount(data.bossDiamondMin, data.bossDiamondMax));
    }

    private static int RollAmount(int min, int max)
    {
        int safeMin = Mathf.Max(0, Mathf.Min(min, max));
        int safeMax = Mathf.Max(safeMin, Mathf.Max(min, max));
        return safeMax <= safeMin ? safeMin : UnityEngine.Random.Range(safeMin, safeMax + 1);
    }

    private void SpawnDrop(EnemyData data, Vector3 deathPosition, int amount)
    {
        if (amount <= 0)
            return;

        Vector3 groundPosition = deathPosition + Vector3.up * data.diamondGroundYOffset;
        GameObject go = data.diamondDropPrefab != null
            ? Instantiate(data.diamondDropPrefab, deathPosition, Quaternion.identity)
            : CreateFallback(deathPosition);

        DiamondDropPickup pickup = go.GetComponent<DiamondDropPickup>();
        if (pickup == null)
            pickup = go.AddComponent<DiamondDropPickup>();

        WorldDropBounceAnimator bounce = go.GetComponent<WorldDropBounceAnimator>();
        if (bounce == null)
        {
            bounce = go.AddComponent<WorldDropBounceAnimator>();
            bounce.popHeight = popHeight;
            bounce.horizontalScatterRadius = scatterRadius;
            bounce.bounceCount = bounceCount;
            bounce.firstBounceHeight = firstBounceHeight;
        }

        pickup.bounceAnimator = bounce;
        pickup.Configure(amount, groundPosition);
    }

    private GameObject CreateFallback(Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Octahedron);
        go.name = "DiamondDrop";
        go.transform.position = position;
        go.transform.localScale = fallbackScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = fallbackColor;
            renderer.sharedMaterial = material;
        }
        return go;
    }
}
