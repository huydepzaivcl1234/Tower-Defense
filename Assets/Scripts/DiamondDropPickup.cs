using UnityEngine;

/// <summary>
/// World Diamond pickup. Uses the shared WorldDropBounceAnimator for spawn/landing motion.
/// Collect by hovering the mouse over it after it settles.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DiamondDropPickup : MonoBehaviour
{
    [Header("Reward")]
    [Min(1)] public int amount = 1;

    [Header("Collection")]
    public bool collectOnMouseEnter = true;
    [Tooltip("If true, the pickup cannot be collected until the spawn/bounce animation finishes.")]
    public bool waitUntilSettled = true;

    [Header("Optional")]
    public WorldDropBounceAnimator bounceAnimator;
    public AudioClip collectSfx;
    [Range(0f, 1f)] public float collectSfxVolume = 1f;

    private bool settled;
    private bool collected;

    public void Configure(int diamondAmount, Vector3 groundPosition)
    {
        amount = Mathf.Max(1, diamondAmount);
        collected = false;
        settled = !waitUntilSettled;

        if (bounceAnimator == null)
            bounceAnimator = GetComponent<WorldDropBounceAnimator>();
        if (bounceAnimator == null)
            bounceAnimator = gameObject.AddComponent<WorldDropBounceAnimator>();

        bounceAnimator.OnSettled -= HandleSettled;
        bounceAnimator.OnSettled += HandleSettled;
        bounceAnimator.Play(groundPosition);
    }

    private void Awake()
    {
        if (bounceAnimator == null)
            bounceAnimator = GetComponent<WorldDropBounceAnimator>();
    }

    private void OnEnable()
    {
        if (bounceAnimator != null)
        {
            bounceAnimator.OnSettled -= HandleSettled;
            bounceAnimator.OnSettled += HandleSettled;
        }
    }

    private void OnDisable()
    {
        if (bounceAnimator != null)
            bounceAnimator.OnSettled -= HandleSettled;
    }

    private void HandleSettled() => settled = true;

    private void OnMouseEnter()
    {
        if (collectOnMouseEnter)
            Collect();
    }

    public void Collect()
    {
        if (collected || (waitUntilSettled && !settled))
            return;
        if (PlayerProfileManager.Instance == null)
            return;

        collected = true;
        int granted = PlayerProfileManager.Instance.AddDiamonds(amount, true, true);
        if (granted <= 0)
        {
            collected = false;
            return;
        }

        if (collectSfx != null)
            AudioSource.PlayClipAtPoint(collectSfx, transform.position, collectSfxVolume);

        Destroy(gameObject);
    }
}
