using UnityEngine;
using DG.Tweening;

/// <summary>
/// World relic reward pickup. No click is required: moving the mouse over it collects it,
/// queues a relic reward, then removes the world object.
/// The old sphere fallback is converted into a 3D card visual at runtime.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class RelicDropPickup : MonoBehaviour
{
    [Header("Visual Motion")]
    public float bobHeight = 0.18f;
    public float bobDuration = 0.7f;
    public float rotateDegreesPerSecond = 70f;

    private RelicRarity minimumRarity = RelicRarity.Common;
    private bool bossReward;
    private bool collected;
    private Tween bobTween;
    private Vector3 basePosition;
    private bool cardVisualBuilt;

    public void Configure(RelicRarity rarity, bool isBossReward)
    {
        minimumRarity = rarity;
        bossReward = isBossReward;
        collected = false;
        basePosition = transform.position;

        SphereCollider hoverCollider = GetComponent<SphereCollider>();
        if (hoverCollider != null)
        {
            hoverCollider.isTrigger = false;
            hoverCollider.radius = 0.85f;
        }

        BuildCardVisual();
        StartBob();
    }

    private void Start()
    {
        basePosition = transform.position;
        if (!cardVisualBuilt)
            BuildCardVisual();
        StartBob();
    }

    private void BuildCardVisual()
    {
        if (cardVisualBuilt) return;
        cardVisualBuilt = true;

        MeshRenderer oldRenderer = GetComponent<MeshRenderer>();
        if (oldRenderer != null) oldRenderer.enabled = false;

        MeshFilter oldFilter = GetComponent<MeshFilter>();
        if (oldFilter != null) oldFilter.mesh = null;

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

    private void StartBob()
    {
        bobTween?.Kill();
        transform.position = basePosition;
        bobTween = transform.DOMoveY(basePosition.y + bobHeight, Mathf.Max(0.1f, bobDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateDegreesPerSecond * Time.deltaTime, Space.World);
    }

    private void OnMouseEnter()
    {
        Collect();
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;

        if (RelicManager.Instance != null)
            RelicManager.Instance.QueueDroppedReward(minimumRarity, bossReward);

        bobTween?.Kill();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        bobTween?.Kill();
    }
}
