using UnityEngine;
using DG.Tweening;

/// <summary>
/// World relic reward pickup. No click is required: moving the mouse over it collects it,
/// queues a relic reward, then removes the world object.
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
            hoverCollider.radius = Mathf.Max(0.65f, hoverCollider.radius);
        }

        StartBob();
    }

    private void Start()
    {
        basePosition = transform.position;
        StartBob();
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
