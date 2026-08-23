using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Small HUD notification shown only when one or more collected relic rewards are waiting.
/// Clicking it opens the next queued relic choice. It never opens the choice panel automatically.
/// </summary>
public class RelicRewardNotificationUI : MonoBehaviour
{
    public GameObject root;
    public Button openButton;
    public TMP_Text label;

    [Header("Attention Pulse")]
    public float pulseScale = 1.06f;
    public float pulseDuration = 0.55f;

    private Tween pulseTween;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (root == null) root = gameObject;
        baseScale = root.transform.localScale;
    }

    private void Start()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenNext);
            openButton.onClick.AddListener(OpenNext);
        }
        SetPendingCount(RelicManager.Instance != null ? RelicManager.Instance.PendingRewardCount : 0);
    }

    public void SetPendingCount(int count)
    {
        bool visible = count > 0;
        if (label != null)
            label.text = count > 1 ? $"RELIC AVAILABLE  x{count}" : "RELIC AVAILABLE";

        if (root != null)
        {
            if (!visible)
            {
                pulseTween?.Kill();
                root.transform.localScale = baseScale;
                root.SetActive(false);
            }
            else
            {
                if (!root.activeSelf) root.SetActive(true);
                StartPulse();
            }
        }
    }

    private void StartPulse()
    {
        if (root == null) return;
        pulseTween?.Kill();
        root.transform.localScale = baseScale;
        pulseTween = root.transform.DOScale(baseScale * pulseScale, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void OpenNext()
    {
        RelicManager.Instance?.OpenNextQueuedReward();
    }

    private void OnDestroy()
    {
        pulseTween?.Kill();
    }
}
