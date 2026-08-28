using UnityEngine;
using DG.Tweening;

/// <summary>
/// Presentation + collection behavior for the Relic world-drop prefab.
/// Relic reward logic remains independent from Diamond drops.
/// Model/SFX/VFX/drop motion are authored on this prefab; EnemyData only controls Relic drop chance/rarity.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class RelicDropPickup : MonoBehaviour
{
    [Header("Drop Motion - Prefab Specific")]
    public bool useDropBounceAnimation = true;
    [Tooltip("Fallback distance from current spawn position down to the final resting position when no RelicManager is available.")]
    [Min(0f)] public float fallbackGroundDropDistance = 0.65f;
    [Tooltip("Automatically add WorldDropBounceAnimator when the prefab does not already have one.")]
    public bool autoAddBounceAnimator = true;

    [Header("Spawn Presentation - Prefab Specific")]
    public AudioClip spawnSfx;
    [Range(0f, 1f)] public float spawnSfxVolume = 1f;
    public GameObject spawnVfxPrefab;
    [Min(0.1f)] public float spawnVfxLifetime = 2f;

    [Header("Pickup Presentation - Prefab Specific")]
    public AudioClip collectSfx;
    [Range(0f, 1f)] public float collectSfxVolume = 1f;
    public GameObject collectVfxPrefab;
    [Min(0.1f)] public float collectVfxLifetime = 2f;

    [Header("Fallback Visual - Only Without Custom Prefab")]
    [Tooltip("Used for manually placed pickups. Runtime fallback objects named RelicDrop/BossRelicDrop automatically build the procedural card. Custom prefabs keep their own model/visuals.")]
    public bool buildProceduralCardWhenUnconfigured = true;

    [Header("Idle Motion After Landing - Prefab Specific")]
    public bool bobAfterLanding = true;
    [Min(0f)] public float bobHeight = 0.18f;
    [Min(0.05f)] public float bobDuration = 0.7f;
    public float rotateDegreesPerSecond = 70f;

    [Header("Collection")]
    [Min(0.1f)] public float hoverColliderRadius = 0.85f;
    public bool allowPickupDuringSpawnAnimation = true;

    private RelicRarity minimumRarity = RelicRarity.Common;
    private bool bossReward;
    private bool collected;
    private Tween bobTween;
    private Vector3 basePosition;
    private bool visualConfigured;
    private WorldDropBounceAnimator dropAnimator;

    public void Configure(RelicRarity rarity, bool isBossReward)
    {
        bool fallbackObject = IsManagerFallbackObject();
        Configure(rarity, isBossReward, fallbackObject);
    }

    public void Configure(RelicRarity rarity, bool isBossReward, bool useProceduralFallbackVisual)
    {
        minimumRarity = rarity;
        bossReward = isBossReward;
        collected = false;
        visualConfigured = true;

        SphereCollider hoverCollider = GetComponent<SphereCollider>();
        if (hoverCollider != null)
        {
            hoverCollider.isTrigger = false;
            hoverCollider.radius = Mathf.Max(0.1f, hoverColliderRadius);
        }

        if (useProceduralFallbackVisual)
            BuildFallbackCardVisual();

        PlaySpawnPresentation();
        BeginSpawnPresentation();
    }

    private bool IsManagerFallbackObject()
    {
        return gameObject.name == "RelicDrop" || gameObject.name == "BossRelicDrop";
    }

    private void Start()
    {
        if (!visualConfigured)
        {
            visualConfigured = true;
            if (buildProceduralCardWhenUnconfigured)
                BuildFallbackCardVisual();
        }

        if (dropAnimator == null && bobTween == null)
            BeginSpawnPresentation();
    }

    private void BeginSpawnPresentation()
    {
        bobTween?.Kill();

        float groundDistance = fallbackGroundDropDistance;
        if (RelicManager.Instance != null)
            groundDistance = Mathf.Max(0f, RelicManager.Instance.relicDropHeight);

        basePosition = transform.position - Vector3.up * groundDistance;

        if (!useDropBounceAnimation)
        {
            transform.position = basePosition;
            StartIdleMotion();
            return;
        }

        dropAnimator = GetComponent<WorldDropBounceAnimator>();
        if (dropAnimator == null && autoAddBounceAnimator)
            dropAnimator = gameObject.AddComponent<WorldDropBounceAnimator>();

        if (dropAnimator == null)
        {
            transform.position = basePosition;
            StartIdleMotion();
            return;
        }

        dropAnimator.OnSettled -= HandleDropSettled;
        dropAnimator.OnSettled += HandleDropSettled;
        dropAnimator.Play(basePosition);
    }

    private void HandleDropSettled()
    {
        if (dropAnimator != null)
            dropAnimator.OnSettled -= HandleDropSettled;
        StartIdleMotion();
    }

    private void BuildFallbackCardVisual()
    {
        if (transform.Find("CardVisual") != null)
            return;

        MeshRenderer oldRenderer = GetComponent<MeshRenderer>();
        if (oldRenderer != null)
            oldRenderer.enabled = false;

        MeshFilter oldFilter = GetComponent<MeshFilter>();
        if (oldFilter != null)
            oldFilter.mesh = null;

        transform.localScale = Vector3.one;

        GameObject card = RelicWorldCardModel.Create(transform.position, minimumRarity, bossReward);
        card.name = "CardVisual";
        card.transform.SetParent(transform, true);
        card.transform.localPosition = Vector3.zero;
        card.transform.localRotation = Quaternion.Euler(10f, 18f, 0f);
        card.transform.localScale = Vector3.one * (bossReward ? 1.15f : 0.95f);

        Collider[] visualColliders = card.GetComponentsInChildren<Collider>(true);
        foreach (Collider visualCollider in visualColliders)
            visualCollider.enabled = false;
    }

    private void StartIdleMotion()
    {
        basePosition = transform.position;
        if (!bobAfterLanding || bobHeight <= 0f)
            return;

        bobTween?.Kill();
        bobTween = transform.DOMoveY(basePosition.y + bobHeight, Mathf.Max(0.05f, bobDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void Update()
    {
        if (dropAnimator != null && dropAnimator.IsAnimating)
            return;

        if (Mathf.Abs(rotateDegreesPerSecond) > 0.01f)
            transform.Rotate(Vector3.up, rotateDegreesPerSecond * Time.deltaTime, Space.World);
    }

    private void OnMouseEnter()
    {
        if (!allowPickupDuringSpawnAnimation && dropAnimator != null && dropAnimator.IsAnimating)
            return;
        Collect();
    }

    private void Collect()
    {
        if (collected)
            return;
        collected = true;

        if (RelicManager.Instance != null)
            RelicManager.Instance.QueueDroppedReward(minimumRarity, bossReward);

        PlayOneShot(collectSfx, collectSfxVolume, transform.position);
        SpawnTimedVfx(collectVfxPrefab, transform.position, collectVfxLifetime);

        bobTween?.Kill();
        if (dropAnimator != null)
        {
            dropAnimator.OnSettled -= HandleDropSettled;
            dropAnimator.Kill(false);
        }
        Destroy(gameObject);
    }

    private void PlaySpawnPresentation()
    {
        PlayOneShot(spawnSfx, spawnSfxVolume, transform.position);
        SpawnTimedVfx(spawnVfxPrefab, transform.position, spawnVfxLifetime);
    }

    private static void PlayOneShot(AudioClip clip, float volume, Vector3 position)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
    }

    private static void SpawnTimedVfx(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        Destroy(instance, Mathf.Max(0.1f, lifetime));
    }

    private void OnDestroy()
    {
        bobTween?.Kill();
        if (dropAnimator != null)
            dropAnimator.OnSettled -= HandleDropSettled;
    }
}
