using DG.Tweening;
using UnityEngine;

/// <summary>
/// Gives a quest-giver NPC a subtle wind/idle motion and handles its quest lifecycle:
/// disappear after giving a quest, then return to its original scene position when that quest completes.
/// The GameObject itself remains active while hidden so it can keep listening for quest events.
/// </summary>
[DisallowMultipleComponent]
public class NPCQuestLifecycle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional visual transform. If empty, the first child with a Renderer is used; otherwise this transform is used.")]
    public Transform visualRoot;
    public NPCDialogueInteractable dialogueInteractable;

    [Header("Idle / Wind")]
    public bool enableIdleWind = true;
    [Min(0f)] public float swayDegrees = 2.5f;
    [Min(0.01f)] public float swaySpeed = 0.85f;
    [Min(0f)] public float secondarySwayDegrees = 0.9f;
    [Min(0.01f)] public float secondarySwaySpeed = 1.35f;
    [Min(0f)] public float bobHeight = 0.025f;
    [Min(0.01f)] public float bobSpeed = 0.8f;
    [Range(0f, 1f)] public float randomMotionAmount = 0.25f;

    [Header("Disappear Animation")]
    [Min(0.01f)] public float disappearDuration = 0.45f;
    [Min(0f)] public float disappearLift = 0.35f;
    [Min(0f)] public float disappearSpinDegrees = 14f;
    public Ease disappearEase = Ease.InBack;

    [Header("Appear Animation")]
    [Min(0.01f)] public float appearDuration = 0.55f;
    [Min(0f)] public float appearDropHeight = 0.45f;
    [Min(0f)] public float appearSpinDegrees = 14f;
    public Ease appearEase = Ease.OutBack;

    [Header("Spawn Point")]
    [Tooltip("When enabled, the NPC returns to the exact world position/rotation it had when this component first Awake'd.")]
    public bool returnToOriginalSpawnPoint = true;

    private ActiveQuest trackedQuest;
    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;
    private Vector3 baseVisualLocalPosition;
    private Quaternion baseVisualLocalRotation;
    private Vector3 baseVisualLocalScale;
    private Vector3 originalWorldPosition;
    private Quaternion originalWorldRotation;
    private float idleSeed;
    private bool hidden;
    private bool transitioning;
    private Sequence transitionSequence;

    public bool IsHidden => hidden;
    public ActiveQuest TrackedQuest => trackedQuest;

    private void Awake()
    {
        originalWorldPosition = transform.position;
        originalWorldRotation = transform.rotation;

        ResolveReferences();
        CacheVisualState();
        idleSeed = Random.Range(0f, 1000f);
    }

    private void OnEnable()
    {
        QuestManager.OnQuestCompleted += HandleQuestCompleted;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestCompleted -= HandleQuestCompleted;
        transitionSequence?.Kill();
    }

    private void OnDestroy()
    {
        transitionSequence?.Kill();
    }

    private void Update()
    {
        if (!enableIdleWind || hidden || transitioning || visualRoot == null) return;
        ApplyIdleMotion();
    }

    public void TrackAcceptedQuest(QuestData questData)
    {
        if (questData == null || QuestManager.Instance == null) return;

        var active = QuestManager.Instance.ActiveQuests;
        ActiveQuest match = null;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            ActiveQuest candidate = active[i];
            if (candidate != null && candidate.data == questData && !candidate.completed)
            {
                match = candidate;
                break;
            }
        }

        if (match == null) return;
        trackedQuest = match;
        PlayDisappear();
    }

    private void HandleQuestCompleted(ActiveQuest quest)
    {
        if (quest == null || trackedQuest == null || quest != trackedQuest) return;

        if (returnToOriginalSpawnPoint)
        {
            transform.position = originalWorldPosition;
            transform.rotation = originalWorldRotation;
        }

        PlayAppear();
    }

    private void ResolveReferences()
    {
        if (dialogueInteractable == null)
            dialogueInteractable = GetComponent<NPCDialogueInteractable>();

        if (visualRoot == null)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                visualRoot = renderers[i].transform;
                if (visualRoot != transform) break;
            }
        }

        if (visualRoot == null) visualRoot = transform;

        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    private void CacheVisualState()
    {
        if (visualRoot == null) return;
        baseVisualLocalPosition = visualRoot.localPosition;
        baseVisualLocalRotation = visualRoot.localRotation;
        baseVisualLocalScale = visualRoot.localScale;
    }

    private void ApplyIdleMotion()
    {
        float t = Time.unscaledTime;
        float noise = Mathf.Lerp(1f, Mathf.PerlinNoise(idleSeed, t * 0.15f) * 1.2f, randomMotionAmount);
        float swayA = Mathf.Sin((t + idleSeed) * swaySpeed) * swayDegrees * noise;
        float swayB = Mathf.Sin((t * secondarySwaySpeed) + idleSeed * 0.37f) * secondarySwayDegrees;

        Quaternion idleRotation = Quaternion.Euler(swayB, 0f, swayA);
        visualRoot.localRotation = baseVisualLocalRotation * idleRotation;

        if (visualRoot != transform && bobHeight > 0f)
        {
            float bob = Mathf.Sin((t + idleSeed * 0.21f) * bobSpeed) * bobHeight;
            visualRoot.localPosition = baseVisualLocalPosition + Vector3.up * bob;
        }
    }

    private void PlayDisappear()
    {
        if (hidden || transitioning || visualRoot == null) return;
        transitioning = true;

        if (dialogueInteractable != null)
            dialogueInteractable.enabled = false;

        transitionSequence?.Kill();
        visualRoot.DOKill();

        Vector3 startPos = visualRoot.localPosition;
        Quaternion startRot = visualRoot.localRotation;
        Vector3 startScale = visualRoot.localScale;

        transitionSequence = DOTween.Sequence().SetUpdate(true);
        transitionSequence.Join(visualRoot.DOScale(Vector3.zero, disappearDuration).SetEase(disappearEase));
        transitionSequence.Join(visualRoot.DOLocalMove(startPos + Vector3.up * disappearLift, disappearDuration).SetEase(Ease.InCubic));
        transitionSequence.Join(visualRoot.DOLocalRotateQuaternion(startRot * Quaternion.Euler(0f, disappearSpinDegrees, disappearSpinDegrees * 0.25f), disappearDuration));
        transitionSequence.OnComplete(() =>
        {
            SetVisualAndColliders(false);
            visualRoot.localPosition = baseVisualLocalPosition;
            visualRoot.localRotation = baseVisualLocalRotation;
            visualRoot.localScale = startScale == Vector3.zero ? baseVisualLocalScale : startScale;
            hidden = true;
            transitioning = false;
        });
    }

    private void PlayAppear()
    {
        if (visualRoot == null) return;

        transitionSequence?.Kill();
        visualRoot.DOKill();

        transitioning = true;
        hidden = false;

        SetVisualAndColliders(true);
        if (dialogueInteractable != null)
            dialogueInteractable.enabled = false;

        visualRoot.localScale = Vector3.zero;
        visualRoot.localPosition = baseVisualLocalPosition + Vector3.up * appearDropHeight;
        visualRoot.localRotation = baseVisualLocalRotation * Quaternion.Euler(0f, -appearSpinDegrees, 0f);

        transitionSequence = DOTween.Sequence().SetUpdate(true);
        transitionSequence.Join(visualRoot.DOScale(baseVisualLocalScale, appearDuration).SetEase(appearEase));
        transitionSequence.Join(visualRoot.DOLocalMove(baseVisualLocalPosition, appearDuration).SetEase(Ease.OutCubic));
        transitionSequence.Join(visualRoot.DOLocalRotateQuaternion(baseVisualLocalRotation, appearDuration).SetEase(Ease.OutCubic));
        transitionSequence.OnComplete(() =>
        {
            visualRoot.localPosition = baseVisualLocalPosition;
            visualRoot.localRotation = baseVisualLocalRotation;
            visualRoot.localScale = baseVisualLocalScale;
            transitioning = false;
            trackedQuest = null;

            if (dialogueInteractable != null)
                dialogueInteractable.enabled = true;
        });
    }

    private void SetVisualAndColliders(bool visible)
    {
        if (cachedRenderers == null || cachedColliders == null)
            ResolveReferences();

        if (cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
                if (cachedRenderers[i] != null)
                    cachedRenderers[i].enabled = visible;
        }

        if (cachedColliders != null)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
                if (cachedColliders[i] != null)
                    cachedColliders[i].enabled = visible;
        }
    }
}
