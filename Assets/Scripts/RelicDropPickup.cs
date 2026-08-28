using UnityEngine;
using DG.Tweening;

/// <summary>
/// World relic reward pickup. Relic reward logic stays independent from Diamond drops.
/// Designer-authored prefab visuals are preserved; the procedural card is only a fallback when
/// RelicManager had no Relic Drop Prefab assigned.
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
    [Tooltip("Used for manually placed pickups. Runtime fallback objects named RelicDrop/BossRelicDrop automatically build the procedural card. Custom prefabs keep their own visuals.")]
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
    private WorldDropBounceAnimator dropAnimator;

    /// <summary>
    /// Canonical runtime path used by the existing RelicManager. It automatically distinguishes
    /// manager-created fallback objects from designer-authored prefabs without changing RelicManager architecture.
    /// </summary>
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

        BeginSpawnPresentation();
    }

    private bool IsManagerFallbackObject()
    {
        // These are the exact names assigned by RelicManager only when no custom relicDropPrefab exists.
        // Instantiate(customPrefab) produces the prefab name + "(Clone)", so it never enters this fallback path.
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
