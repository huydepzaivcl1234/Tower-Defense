using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldEventManager : MonoBehaviour
{
    public static WorldEventManager Instance { get; private set; }

    [Header("Event Pool")]
    public List<WorldEventData> eventPool = new List<WorldEventData>();
    [Range(0f, 1f)] public float eventChancePerOpportunity = 0.35f;
    [Range(0f, 1f)] public float rareChanceWhenEventRolls = 0.10f;
    [Min(0)] public int firstEligibleWave = 2;
    [Min(0)] public int cooldownWavesAfterEvent = 1;

    [Header("Announcement UI")]
    public CanvasGroup announcementRoot;
    public TMP_Text rarityText;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Image iconImage;
    [Min(0f)] public float hudSlideDuration = 0.35f;
    [Min(0f)] public float announcementEnterDuration = 0.35f;
    [Min(0f)] public float announcementHoldDuration = 2.2f;
    [Min(0f)] public float announcementExitDuration = 0.35f;
    public Vector2 announcementEnterOffset = new Vector2(0f, 120f);
    public Ease announcementEnterEase = Ease.OutBack;
    public Ease announcementExitEase = Ease.InCubic;

    [System.Serializable]
    public class HUDTarget
    {
        public RectTransform target;
        public Vector2 hiddenOffset;
        [HideInInspector] public Vector2 shownPosition;
        [HideInInspector] public bool captured;
    }
    public List<HUDTarget> hudTargets = new List<HUDTarget>();

    [Header("World Placement")]
    public Transform worldCenter;
    public float worldGroundY = 0f;

    [Header("Holy Light Scene Lighting (optional)")]
    public Light holyDirectionalLight;
    public Color holyLightColor = new Color(1f, 0.96f, 0.72f, 1f);
    [Min(0f)] public float holyLightIntensity = 2.1f;
    [Min(0f)] public float holyLightFadeDuration = 0.7f;

    [Header("Runtime (read-only)")]
    [SerializeField] private WorldEventData activeEvent;
    [SerializeField] private int activeRoundsRemaining;
    [SerializeField] private int holyPenaltyRoundsRemaining;
    [SerializeField] private int cooldownRemaining;
    [SerializeField] private bool holyCollapsed;

    private Coroutine continuousEffectRoutine;
    private GameObject holyVisualInstance;
    private float normalLightIntensity;
    private Color normalLightColor;
    private bool lightStateCaptured;

    public WorldEventData ActiveEvent => activeEvent;
    public bool HasActiveEvent => activeEvent != null;
    public bool IsDogCatRainActive => activeEvent != null && activeEvent.eventType == WorldEventType.DogCatRain;
    public bool IsMeteorShowerActive => activeEvent != null && activeEvent.eventType == WorldEventType.MeteorShower;
    public bool IsHolyLightActive => activeEvent != null && activeEvent.eventType == WorldEventType.HolyLight && !holyCollapsed;
    public bool IsHolyPenaltyActive => holyPenaltyRoundsRemaining > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CaptureHUDPositions();
        if (announcementRoot != null)
        {
            announcementRoot.alpha = 0f;
            announcementRoot.gameObject.SetActive(false);
        }
        CaptureLightState();
    }

    private void OnEnable() => WaveManager.OnWaveCleared += HandleWaveCleared;
    private void OnDisable()
    {
        WaveManager.OnWaveCleared -= HandleWaveCleared;
        StopContinuousEffect();
    }

    public IEnumerator PrepareForWave(int upcomingWaveNumber)
    {
        if (activeEvent == null && holyPenaltyRoundsRemaining <= 0)
        {
            if (cooldownRemaining > 0)
            {
                cooldownRemaining--;
            }
            else if (upcomingWaveNumber >= Mathf.Max(1, firstEligibleWave))
            {
                WorldEventData rolled = RollEvent();
                if (rolled != null)
                {
                    ActivateEvent(rolled);
                    yield return PresentAnnouncement(rolled, false);
                }
            }
        }

        StartContinuousEffectForCurrentWave();
    }

    private WorldEventData RollEvent()
    {
        if (eventPool == null || eventPool.Count == 0) return null;
        if (Random.value > eventChancePerOpportunity) return null;

        WorldEventRarity wanted = Random.value < rareChanceWhenEventRolls ? WorldEventRarity.Rare : WorldEventRarity.Common;
        WorldEventData chosen = WeightedPick(wanted);
        if (chosen == null) chosen = WeightedPick(wanted == WorldEventRarity.Rare ? WorldEventRarity.Common : WorldEventRarity.Rare);
        return chosen;
    }

    private WorldEventData WeightedPick(WorldEventRarity rarity)
    {
        float total = 0f;
        for (int i = 0; i < eventPool.Count; i++)
        {
            WorldEventData e = eventPool[i];
            if (e == null || e.rarity != rarity || e.selectionWeight <= 0f) continue;
            total += e.selectionWeight;
        }
        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        float cursor = 0f;
        for (int i = 0; i < eventPool.Count; i++)
        {
            WorldEventData e = eventPool[i];
            if (e == null || e.rarity != rarity || e.selectionWeight <= 0f) continue;
            cursor += e.selectionWeight;
            if (roll <= cursor) return e;
        }
        return null;
    }

    private void ActivateEvent(WorldEventData data)
    {
        StopContinuousEffect();
        activeEvent = data;
        activeRoundsRemaining = Mathf.Max(1, data.durationRounds);
        holyCollapsed = false;

        if (data.eventType == WorldEventType.HolyLight)
            BeginHolyVisuals(data);
    }

    private void HandleWaveCleared()
    {
        StopContinuousEffect();

        if (holyPenaltyRoundsRemaining > 0)
        {
            holyPenaltyRoundsRemaining--;
            if (holyPenaltyRoundsRemaining <= 0)
                holyPenaltyRoundsRemaining = 0;
            return;
        }

        if (activeEvent == null) return;

        if (activeEvent.eventType == WorldEventType.HolyLight && !holyCollapsed &&
            activeRoundsRemaining > 1 && Random.value < activeEvent.holyCollapseChancePerRound)
        {
            TriggerHolyCollapse();
            return;
        }

        activeRoundsRemaining--;
        if (activeRoundsRemaining <= 0)
            EndActiveEvent();
    }

    private void TriggerHolyCollapse()
    {
        if (activeEvent == null || activeEvent.eventType != WorldEventType.HolyLight) return;
        WorldEventData holy = activeEvent;
        holyCollapsed = true;
        activeRoundsRemaining = 0;
        holyPenaltyRoundsRemaining = Mathf.Max(1, holy.collapsePenaltyRounds);
        EndHolyVisuals();
        activeEvent = null;
        cooldownRemaining = Mathf.Max(cooldownRemaining, cooldownWavesAfterEvent);
    }

    private void EndActiveEvent()
    {
        if (activeEvent != null && activeEvent.eventType == WorldEventType.HolyLight)
            EndHolyVisuals();
        activeEvent = null;
        activeRoundsRemaining = 0;
        holyCollapsed = false;
        cooldownRemaining = Mathf.Max(cooldownRemaining, cooldownWavesAfterEvent);
    }

    private void StartContinuousEffectForCurrentWave()
    {
        StopContinuousEffect();
        if (activeEvent == null) return;
        if (activeEvent.eventType == WorldEventType.DogCatRain)
            continuousEffectRoutine = StartCoroutine(DogCatRainRoutine(activeEvent));
        else if (activeEvent.eventType == WorldEventType.MeteorShower)
            continuousEffectRoutine = StartCoroutine(MeteorShowerRoutine(activeEvent));
    }

    private void StopContinuousEffect()
    {
        if (continuousEffectRoutine != null)
        {
            StopCoroutine(continuousEffectRoutine);
            continuousEffectRoutine = null;
        }
    }

    private IEnumerator DogCatRainRoutine(WorldEventData data)
    {
        yield return null;
        while (activeEvent == data && WaveManager.Instance != null && WaveManager.Instance.IsWaveInProgress)
        {
            if (HasLivingEnemies())
                SpawnGoldDrop(data);
            yield return new WaitForSeconds(Mathf.Max(0.05f, data.goldDropInterval));
        }
        continuousEffectRoutine = null;
    }

    private void SpawnGoldDrop(WorldEventData data)
    {
        Vector3 end = RandomGroundPoint(data.goldDropAreaSize);
        Vector3 start = end + Vector3.up * data.goldDropHeight;
        GameObject go = data.goldDropPrefab != null ? Instantiate(data.goldDropPrefab, start, Random.rotation) : null;
        float duration = Mathf.Max(0.05f, data.goldDropFallDuration);
        if (go != null)
        {
            go.transform.DOMove(end, duration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.AddGold(Mathf.Max(0, data.goldPerDrop));
                Destroy(go);
            });
            go.transform.DORotate(new Vector3(Random.Range(90f, 360f), Random.Range(90f, 360f), Random.Range(90f, 360f)), duration, RotateMode.LocalAxisAdd);
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(Mathf.Max(0, data.goldPerDrop));
        }
    }

    private IEnumerator MeteorShowerRoutine(WorldEventData data)
    {
        yield return null;
        while (activeEvent == data && WaveManager.Instance != null && WaveManager.Instance.IsWaveInProgress)
        {
            if (HasLivingEnemies() && Random.value < data.meteorChancePerTick)
                SpawnMeteor(data);
            yield return new WaitForSeconds(Mathf.Max(0.05f, data.meteorTickInterval));
        }
        continuousEffectRoutine = null;
    }

    private void SpawnMeteor(WorldEventData data)
    {
        Vector3 end = RandomGroundPoint(data.meteorAreaSize);
        Vector3 start = end + Vector3.up * data.meteorSpawnHeight + new Vector3(-data.meteorSpawnHeight * 0.35f, 0f, 0f);
        GameObject go = data.meteorPrefab != null ? Instantiate(data.meteorPrefab, start, Quaternion.identity) : null;
        float duration = Mathf.Max(0.05f, data.meteorFallDuration);
        if (go != null)
        {
            Vector3 dir = end - start;
            if (dir.sqrMagnitude > 0.001f) go.transform.rotation = Quaternion.LookRotation(dir.normalized);
            go.transform.DOMove(end, duration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                ResolveMeteorImpact(end, data);
                Destroy(go);
            });
        }
        else
        {
            ResolveMeteorImpact(end, data);
        }
    }

    private void ResolveMeteorImpact(Vector3 point, WorldEventData data)
    {
        Collider[] hits = Physics.OverlapSphere(point, Mathf.Max(0.1f, data.meteorHitRadius), ~0, QueryTriggerInteraction.Collide);
        HashSet<Enemy> enemies = new HashSet<Enemy>();
        HashSet<Tower> towers = new HashSet<Tower>();
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            Enemy enemy = hits[i].GetComponentInParent<Enemy>();
            if (enemy != null && enemy.IsAlive) enemies.Add(enemy);
            Tower tower = hits[i].GetComponentInParent<Tower>();
            if (tower != null) towers.Add(tower);
        }
        foreach (Enemy enemy in enemies)
            enemy.TakeDamage(enemy.MaxHP * Mathf.Clamp01(data.meteorEnemyMaxHpDamagePercent));
        foreach (Tower tower in towers)
            tower.ApplyTemporaryAttackSpeedPenalty(data.meteorTowerAttackSpeedPenaltyPercent, data.meteorTowerDebuffDuration);
    }

    private bool HasLivingEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
            if (enemies[i] != null && enemies[i].IsAlive) return true;
        return false;
    }

    private Vector3 RandomGroundPoint(Vector2 size)
    {
        Vector3 center = worldCenter != null ? worldCenter.position : Vector3.zero;
        center.y = worldGroundY;
        return center + new Vector3(Random.Range(-size.x * 0.5f, size.x * 0.5f), 0f, Random.Range(-size.y * 0.5f, size.y * 0.5f));
    }

    public float ApplyTowerDamage(float value)
    {
        if (IsHolyLightActive) value *= 1f + Mathf.Max(0f, activeEvent.holyTowerDamageBonusPercent);
        return value;
    }

    public float ApplyTowerAttackSpeed(float value)
    {
        if (IsHolyLightActive) value *= 1f + Mathf.Max(0f, activeEvent.holyTowerAttackSpeedBonusPercent);
        return value;
    }

    public float ApplyProjectileSpeed(float value)
    {
        if (IsHolyLightActive) value *= 1f + Mathf.Max(0f, activeEvent.holyProjectileSpeedBonusPercent);
        return value;
    }

    public float GetEnemyMaxHpMultiplier()
    {
        float multiplier = 1f;
        if (IsDogCatRainActive) multiplier *= 1f + Mathf.Max(0f, activeEvent.enemyMaxHpBonusPercent);
        if (IsHolyPenaltyActive)
        {
            WorldEventData holy = FindHolyEventDefinition();
            if (holy != null) multiplier *= 1f + Mathf.Max(0f, holy.collapseEnemyMaxHpBonusPercent);
        }
        return multiplier;
    }

    public float GetEnemyCCResistanceBonus()
    {
        if (!IsHolyPenaltyActive) return 0f;
        WorldEventData holy = FindHolyEventDefinition();
        return holy != null ? Mathf.Clamp01(holy.collapseEnemyCCResistanceBonusPercent) : 0f;
    }

    public float GetEnemySpawnShieldPercent()
    {
        if (!IsHolyPenaltyActive) return 0f;
        WorldEventData holy = FindHolyEventDefinition();
        return holy != null ? Mathf.Max(0f, holy.collapseEnemyShieldPercentOfMaxHp) : 0f;
    }

    private WorldEventData FindHolyEventDefinition()
    {
        for (int i = 0; i < eventPool.Count; i++)
            if (eventPool[i] != null && eventPool[i].eventType == WorldEventType.HolyLight) return eventPool[i];
        return null;
    }

    private IEnumerator PresentAnnouncement(WorldEventData data, bool collapsed)
    {
        if (data == null) yield break;
        CaptureHUDPositions();
        yield return AnimateHUD(false);

        if (announcementRoot != null)
        {
            announcementRoot.gameObject.SetActive(true);
            announcementRoot.alpha = 0f;
            RectTransform rect = announcementRoot.transform as RectTransform;
            Vector2 basePos = rect != null ? rect.anchoredPosition : Vector2.zero;
            if (rect != null) rect.anchoredPosition = basePos + announcementEnterOffset;

            if (rarityText != null) rarityText.text = collapsed ? "EVENT COLLAPSED" : data.rarity.ToString().ToUpperInvariant() + " EVENT";
            if (titleText != null) titleText.text = collapsed ? "HOLY LIGHT FADES" : data.eventName;
            if (descriptionText != null) descriptionText.text = collapsed ? "The blessing has vanished. Enemies are empowered." : data.description;
            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = data.icon != null;
            }
            if (data.announcementSfx != null) AudioSource.PlayClipAtPoint(data.announcementSfx, Camera.main != null ? Camera.main.transform.position : Vector3.zero);

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(announcementRoot.DOFade(1f, announcementEnterDuration));
            if (rect != null) seq.Join(rect.DOAnchorPos(basePos, announcementEnterDuration).SetEase(announcementEnterEase));
            yield return seq.WaitForCompletion();
            if (announcementHoldDuration > 0f) yield return new WaitForSecondsRealtime(announcementHoldDuration);
            yield return announcementRoot.DOFade(0f, announcementExitDuration).SetEase(announcementExitEase).SetUpdate(true).WaitForCompletion();
            announcementRoot.gameObject.SetActive(false);
            if (rect != null) rect.anchoredPosition = basePos;
        }
        else if (announcementHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(announcementHoldDuration);
        }

        yield return AnimateHUD(true);
    }

    private IEnumerator AnimateHUD(bool show)
    {
        float duration = Mathf.Max(0f, hudSlideDuration);
        CaptureHUDPositions();
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        bool any = false;
        for (int i = 0; i < hudTargets.Count; i++)
        {
            HUDTarget item = hudTargets[i];
            if (item == null || item.target == null || !item.captured) continue;
            Vector2 target = show ? item.shownPosition : item.shownPosition + item.hiddenOffset;
            seq.Join(item.target.DOAnchorPos(target, duration).SetEase(Ease.InOutCubic));
            any = true;
        }
        if (!any || duration <= 0f) yield break;
        yield return seq.WaitForCompletion();
    }

    private void CaptureHUDPositions()
    {
        for (int i = 0; i < hudTargets.Count; i++)
        {
            HUDTarget item = hudTargets[i];
            if (item == null || item.target == null || item.captured) continue;
            item.shownPosition = item.target.anchoredPosition;
            item.captured = true;
        }
    }

    private void CaptureLightState()
    {
        if (holyDirectionalLight == null || lightStateCaptured) return;
        normalLightIntensity = holyDirectionalLight.intensity;
        normalLightColor = holyDirectionalLight.color;
        lightStateCaptured = true;
    }

    private void BeginHolyVisuals(WorldEventData data)
    {
        CaptureLightState();
        if (holyDirectionalLight != null)
        {
            DOTween.To(() => holyDirectionalLight.intensity, x => holyDirectionalLight.intensity = x, holyLightIntensity, holyLightFadeDuration).SetUpdate(true);
            DOTween.To(() => holyDirectionalLight.color, x => holyDirectionalLight.color = x, holyLightColor, holyLightFadeDuration).SetUpdate(true);
        }
        if (data.holyLightVisualPrefab != null && holyVisualInstance == null)
            holyVisualInstance = Instantiate(data.holyLightVisualPrefab, worldCenter != null ? worldCenter.position : Vector3.zero, Quaternion.identity);
    }

    private void EndHolyVisuals()
    {
        if (holyDirectionalLight != null && lightStateCaptured)
        {
            DOTween.To(() => holyDirectionalLight.intensity, x => holyDirectionalLight.intensity = x, normalLightIntensity, holyLightFadeDuration).SetUpdate(true);
            DOTween.To(() => holyDirectionalLight.color, x => holyDirectionalLight.color = x, normalLightColor, holyLightFadeDuration).SetUpdate(true);
        }
        if (holyVisualInstance != null)
        {
            Destroy(holyVisualInstance);
            holyVisualInstance = null;
        }
    }
}
