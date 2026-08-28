using UnityEngine;
using DG.Tweening;

/// <summary>
/// World relic reward pickup. No click is required: moving the mouse over it collects it,
/// queues a relic reward, then removes the world object.
/// Spawn motion uses the shared WorldDropBounceAnimator so other drops can reuse the same feel.
/// Custom prefab visuals are preserved; the procedural card is fallback-only.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class RelicDropPickup : MonoBehaviour
{
    [Header("Spawn Animation")]
    public bool useDropBounceAnimation = true;
    [Tooltip("Fallback distance from current spawn position down to the final resting position when no RelicManager is available.")]
    [Min(0f)] public float fallbackGroundDropDistance = 0.65f;
    [Tooltip("Automatically add WorldDropBounceAnimator when the prefab does not already have one.")]
    public bool autoAddBounceAnimator = true;

    [Header("Fallback Visual")]
    [Tooltip("For manually placed pickups only. Runtime RelicManager decides automatically: custom prefab = keep custom visual, no prefab = build fallback card.")]
    public bool buildProceduralCardWhenUnconfigured = true;

    [Header("Idle Motion After Landing")]
    public bool bobAfterLanding = true;
    [Min(0f)] public float bobHeight = 0.18f;
    [Min(0.05f)] public float bobDuration = 0.7f;
    public float rotateDegreesPerSecond = 70f;

    [Header("Pickup")]
    [Min(0.1f)] public float hoverColliderRadius = 0.85f;
    public bool allowPickupDuringSpawnAnimation = true;

    private RelicRarity minimumRarity = RelicRarity.Common;
    private bool bossReward;
    private bool collected;
    private Tween bobTween;
    private Vector3 basePosition;
    private bool visualConfigured;
    private bool useFallbackVisual;
    private WorldDropBounceAnimator dropAnimator;

    public void Configure(RelicRarity rarity, bool isBossReward)
    {
        Configure(rarity, isBossReward, buildProceduralCardWhenUnconfigured);
    }

    /// <summary>
    /// Runtime configuration. useProceduralFallbackVisual should be false when the manager instantiated
    /// a designer-authored prefab, so the pickup never hides/replaces that prefab's mesh or children.
    /// </summary>
    public void Configure(RelicRarity rarity, bool isBossReward, bool useProceduralFallbackVisual)
    {
        minimumRarity = rarity;
        bossReward = isBossReward;
        collected = false;
        visualConfigured = true;
        useFallbackVisual = useProceduralFallbackVisual;

        SphereCollider hoverCollider = GetComponent<SphereCollider>();
        if (hoverCollider != null)
        {
            hoverCollider.isTrigger = false;
            hoverCollider.radius = Mathf.Max(0.1f, hoverColliderRadius);
        }

        if (useFallbackVisual)
            BuildFallbackCardVisual();

        BeginSpawnPresentation();
    }

    private void Start()
    {
        // Manually placed pickups can still use the procedural fallback if requested.
        if (!visualConfigured)
        {
            useFallbackVisual = buildProceduralCardWhenUnconfigured;
            visualConfigured = true;
            if (useFallbackVisual)
                BuildFallbackCardVisual();
        }

        // Configure() is the normal runtime path. This fallback keeps manually placed pickups usable.
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

        // Only fallback-generated primitive visuals are suppressed here. Designer-authored prefab visuals
        // never enter this path, so they remain untouched.
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

        bobTween?.Kill();
        if (dropAnimator != null)
        {
            dropAnimator.OnSettled -= HandleDropSettled;
            dropAnimator.Kill(false);
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        bobTween?.Kill();
        if (dropAnimator != null)
            dropAnimator.OnSettled -= HandleDropSettled;
    }
}
