using UnityEngine;

/// <summary>
/// Presentation + collection behavior for the Diamond world-drop prefab.
/// EnemyData never owns model/SFX/VFX. Author those directly on this prefab/component.
/// </summary>
[DisallowMultipleComponent]
public class DiamondDropPickup : MonoBehaviour
{
    [Header("Runtime Reward (set by DiamondDropSystem)")]
    [Min(1)] public int amount = 1;

    [Header("Collection")]
    public bool collectOnMouseEnter = true;
    [Tooltip("If true, the pickup cannot be collected until the spawn/bounce animation finishes.")]
    public bool waitUntilSettled = true;
    [Tooltip("Fallback collider radius only used when the custom prefab has no Collider at all.")]
    [Min(0.05f)] public float fallbackColliderRadius = 0.75f;

    [Header("Spawn Presentation - Prefab Specific")]
    [Tooltip("Optional SFX played when this Diamond prefab spawns.")]
    public AudioClip spawnSfx;
    [Range(0f, 1f)] public float spawnSfxVolume = 1f;
    [Tooltip("Optional VFX prefab spawned at the Diamond's spawn position.")]
    public GameObject spawnVfxPrefab;
    [Min(0.1f)] public float spawnVfxLifetime = 2f;

    [Header("Pickup Presentation - Prefab Specific")]
    [Tooltip("Optional SFX played when this Diamond is collected.")]
    public AudioClip collectSfx;
    [Range(0f, 1f)] public float collectSfxVolume = 1f;
    [Tooltip("Optional VFX prefab spawned when this Diamond is collected.")]
    public GameObject collectVfxPrefab;
    [Min(0.1f)] public float collectVfxLifetime = 2f;

    [Header("Drop Motion - Prefab Specific")]
    [Tooltip("Put and tune WorldDropBounceAnimator on the prefab for full control. This reference is auto-filled when possible.")]
    public WorldDropBounceAnimator bounceAnimator;

    private bool settled;
    private bool collected;

    public void Configure(int diamondAmount, Vector3 groundPosition)
    {
        amount = Mathf.Max(1, diamondAmount);
        collected = false;
        settled = !waitUntilSettled;
        EnsureCollider();

        if (bounceAnimator == null)
            bounceAnimator = GetComponent<WorldDropBounceAnimator>();
        if (bounceAnimator == null)
            bounceAnimator = gameObject.AddComponent<WorldDropBounceAnimator>();

        PlaySpawnPresentation();

        bounceAnimator.OnSettled -= HandleSettled;
        bounceAnimator.OnSettled += HandleSettled;
        bounceAnimator.Play(groundPosition);
    }

    private void Awake()
    {
        EnsureCollider();
        if (bounceAnimator == null)
            bounceAnimator = GetComponent<WorldDropBounceAnimator>();
    }

    private void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>(true) != null)
            return;

        SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
        sphere.radius = Mathf.Max(0.05f, fallbackColliderRadius);
        sphere.isTrigger = false;
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

        PlayOneShot(collectSfx, collectSfxVolume, transform.position);
        SpawnTimedVfx(collectVfxPrefab, transform.position, collectVfxLifetime);
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
}
