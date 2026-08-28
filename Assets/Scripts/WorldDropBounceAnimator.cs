using DG.Tweening;
using UnityEngine;

/// <summary>
/// Reusable spawn animation for any world drop (relics, currencies, items, etc.).
/// Plays an authored pop-out arc, falls to a ground position, then performs configurable bounces.
/// No gameplay ownership: pickup/reward scripts decide what the object actually gives.
/// </summary>
[DisallowMultipleComponent]
public class WorldDropBounceAnimator : MonoBehaviour
{
    [Header("Spawn Pop")]
    public bool playOnEnable = false;
    [Min(0f)] public float popHeight = 1.8f;
    [Min(0f)] public float horizontalScatterRadius = 0.8f;
    [Min(0.01f)] public float popDuration = 0.28f;
    public Ease popEase = Ease.OutQuad;

    [Header("Fall")]
    [Min(0.01f)] public float fallDuration = 0.34f;
    public Ease fallEase = Ease.InQuad;

    [Header("Bounce")]
    [Range(0, 6)] public int bounceCount = 2;
    [Min(0f)] public float firstBounceHeight = 0.38f;
    [Range(0f, 1f)] public float bounceHeightMultiplier = 0.48f;
    [Min(0.01f)] public float firstBounceDuration = 0.20f;
    [Range(0.1f, 1f)] public float bounceDurationMultiplier = 0.75f;
    public Ease bounceUpEase = Ease.OutQuad;
    public Ease bounceDownEase = Ease.InQuad;

    [Header("Rotation")]
    public bool rotateDuringSpawn = true;
    public Vector3 rotationMin = new Vector3(-55f, 100f, -35f);
    public Vector3 rotationMax = new Vector3(55f, 260f, 35f);

    [Header("Landing")]
    [Tooltip("Optional local visual scale punch when the drop lands.")]
    public bool landingScalePunch = true;
    public Vector3 landingPunchScale = new Vector3(0.10f, -0.08f, 0.10f);
    [Min(0.01f)] public float landingPunchDuration = 0.18f;
    [Range(0, 20)] public int landingPunchVibrato = 5;
    [Range(0f, 1f)] public float landingPunchElasticity = 0.5f;

    [Header("Time")]
    public bool useUnscaledTime = false;

    private Sequence sequence;
    private Vector3 restingPosition;
    private bool hasRestingPosition;

    public bool IsAnimating => sequence != null && sequence.IsActive() && sequence.IsPlaying();
    public Vector3 RestingPosition => restingPosition;

    private void OnEnable()
    {
        if (playOnEnable)
            Play(transform.position);
    }

    /// <summary>Play using the supplied final ground/rest position.</summary>
    public void Play(Vector3 groundPosition)
    {
        Kill(false);

        restingPosition = groundPosition;
        hasRestingPosition = true;

        Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, horizontalScatterRadius);
        Vector3 launchTarget = groundPosition + new Vector3(scatter.x, Mathf.Max(0f, popHeight), scatter.y);

        Vector3 authoredScale = transform.localScale;
        sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);

        sequence.Append(transform.DOMove(launchTarget, Mathf.Max(0.01f, popDuration)).SetEase(popEase));

        if (rotateDuringSpawn)
        {
            Vector3 spin = new Vector3(
                Random.Range(rotationMin.x, rotationMax.x),
                Random.Range(rotationMin.y, rotationMax.y),
                Random.Range(rotationMin.z, rotationMax.z));
            transform.DORotate(spin, Mathf.Max(0.01f, popDuration + fallDuration), RotateMode.LocalAxisAdd)
                .SetEase(Ease.OutCubic)
                .SetUpdate(useUnscaledTime)
                .SetTarget(this);
        }

        sequence.Append(transform.DOMove(groundPosition, Mathf.Max(0.01f, fallDuration)).SetEase(fallEase));

        float height = Mathf.Max(0f, firstBounceHeight);
        float duration = Mathf.Max(0.01f, firstBounceDuration);
        int count = Mathf.Max(0, bounceCount);

        for (int i = 0; i < count; i++)
        {
            if (height <= 0.001f)
                break;

            Vector3 bounceTop = groundPosition + Vector3.up * height;
            float half = duration * 0.5f;
            sequence.Append(transform.DOMove(bounceTop, half).SetEase(bounceUpEase));
            sequence.Append(transform.DOMove(groundPosition, half).SetEase(bounceDownEase));

            height *= Mathf.Clamp01(bounceHeightMultiplier);
            duration *= Mathf.Clamp(bounceDurationMultiplier, 0.1f, 1f);
        }

        if (landingScalePunch)
        {
            sequence.Join(transform.DOPunchScale(
                    landingPunchScale,
                    Mathf.Max(0.01f, landingPunchDuration),
                    Mathf.Max(0, landingPunchVibrato),
                    Mathf.Clamp01(landingPunchElasticity))
                .SetUpdate(useUnscaledTime));
        }

        sequence.OnComplete(() =>
        {
            if (transform != null)
            {
                transform.position = restingPosition;
                transform.localScale = authoredScale;
            }
            sequence = null;
        });
    }

    public void SnapToRestingPosition()
    {
        Kill(false);
        if (hasRestingPosition)
            transform.position = restingPosition;
    }

    public void Kill(bool complete)
    {
        if (sequence != null)
        {
            sequence.Kill(complete);
            sequence = null;
        }
        DOTween.Kill(this, complete);
    }

    private void OnDisable() => Kill(false);
    private void OnDestroy() => Kill(false);
}
