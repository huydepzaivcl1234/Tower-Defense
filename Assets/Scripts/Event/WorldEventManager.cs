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
    public AudioSource eventAudioSource;
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
    [SerializeField] private bool currentWaveEffectsRunning;
    [SerializeField] private bool isPresentingAnnouncement;
    [SerializeField] private bool holyPenaltyStartsNextWave;
    [SerializeField] private bool holyCollapseRolledThisRound;

    private Coroutine continuousEffectRoutine;
    private GameObject holyVisualInstance;
    private float normalLightIntensity;
    private Color normalLightColor;
    private bool lightStateCaptured;

    private WorldEventData holyPenaltySource;
    private WorldEventData pendingHolyCollapseAnnouncement;

    public WorldEventData ActiveEvent => activeEvent;
    public bool HasActiveEvent => activeEvent != null;
    public bool IsDogCatRainActive => activeEvent != null && activeEvent.eventType == WorldEventType.DogCatRain;
    public bool IsMeteorShowerActive => activeEvent != null && activeEvent.eventType == WorldEventType.MeteorShower;
    public bool IsHolyLightActive => activeEvent != null && activeEvent.eventType == WorldEventType.HolyLight && !holyCollapsed;
    public bool IsHolyPenaltyActive => holyPenaltyRoundsRemaining > 0 && holyPenaltySource != null;
    public bool IsPresentingAnnouncement => isPresentingAnnouncement;
    public int ActiveRoundsRemaining => activeRoundsRemaining;
    public int HolyPenaltyRoundsRemaining => holyPenaltyRoundsRemaining;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CaptureHUDPositions();

        if (eventAudioSource == null)
        {
            eventAudioSource = GetComponent<AudioSource>();
            if (eventAudioSource == null)
                eventAudioSource = gameObject.AddComponent<AudioSource>();
            eventAudioSource.playOnAwake = false;
            eventAudioSource.loop = false;
            eventAudioSource.spatialBlend = 0f;
        }

        if (announcementRoot != null)
        {
            announcementRoot.alpha = 0f;
            announcementRoot.gameObject.SetActive(false);
        }

        CaptureLightState();
    }

    private void OnEnable()
    {
        WaveManager.OnWaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveCleared -= HandleWaveCleared;
        currentWaveEffectsRunning = false;
        StopContinuousEffect();
        RestoreHUDInstant();
        RestoreHolyLightInstant();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public IEnumerator PrepareForWave(int upcomingWaveNumber)
    {
        currentWaveEffectsRunning = true;

        if (pendingHolyCollapseAnnouncement != null)
        {
            WorldEventData collapsed = pendingHolyCollapseAnnouncement;
            pendingHolyCollapseAnnouncement = null;
            yield return PresentAnnouncement(collapsed, true);
        }

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
        if (eventPool == null || eventPool.Count == 0)
            return null;

        float eventChance = Mathf.Clamp01(eventChancePerOpportunity);
        if (eventChance <= 0f)
            return null;
        if (eventChance < 1f && Random.value >= eventChance)
            return null;

        float rareChance = Mathf.Clamp01(rareChanceWhenEventRolls);
        WorldEventRarity wanted = rareChance > 0f && Random.value < rareChance
            ? WorldEventRarity.Rare
            : WorldEventRarity.Common;

        return WeightedPick(wanted);
    }

    private WorldEventData WeightedPick(WorldEventRarity rarity)
    {
        float totalWeight = 0f;

        for (int i = 0; i < eventPool.Count; i++)
        {
            WorldEventData candidate = eventPool[i];
            if (candidate == null || candidate.rarity != rarity || candidate.selectionWeight <= 0f)
                continue;

            totalWeight += candidate.selectionWeight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);
        float cursor = 0f;

        for (int i = 0; i < eventPool.Count; i++)
        {
            WorldEventData candidate = eventPool[i];
            if (candidate == null || candidate.rarity != rarity || candidate.selectionWeight <= 0f)
                continue;

            cursor += candidate.selectionWeight;
            if (roll <= cursor)
                return candidate;
        }

        return null;
    }

    private void ActivateEvent(WorldEventData data)
    {
        if (data == null)
            return;

        StopContinuousEffect();
        activeEvent = data;
        activeRoundsRemaining = Mathf.Max(1, data.durationRounds);
        holyCollapsed = false;
        holyCollapseRolledThisRound = false;

        if (data.eventType == WorldEventType.HolyLight)
            BeginHolyVisuals(data);
    }

    private void HandleWaveCleared()
    {
        currentWaveEffectsRunning = false;

        // If Holy successfully rolled collapse this round but the wave ended before
        // its configured delay elapsed, collapse now so a 100% roll can never be lost.
        if (IsHolyLightActive && holyCollapseRolledThisRound)
        {
            StopContinuousEffect();
            TriggerHolyCollapse();
            holyPenaltyStartsNextWave = false;
            holyCollapseRolledThisRound = false;
            return;
        }

        StopContinuousEffect();
        holyCollapseRolledThisRound = false;

        if (holyPenaltyRoundsRemaining > 0)
        {
            // A mid-wave collapse starts its penalty on the NEXT complete wave.
            // Do not consume one penalty round when the collapse wave itself clears.
            if (holyPenaltyStartsNextWave)
            {
                holyPenaltyStartsNextWave = false;
                return;
            }

            holyPenaltyRoundsRemaining--;
            if (holyPenaltyRoundsRemaining <= 0)
            {
                holyPenaltyRoundsRemaining = 0;
                holyPenaltySource = null;
                holyCollapsed = false;
            }
            return;
        }

        if (activeEvent == null)
            return;

        activeRoundsRemaining--;
        if (activeRoundsRemaining <= 0)
            EndActiveEvent();
    }

    private void TriggerHolyCollapse()
    {
        if (activeEvent == null || activeEvent.eventType != WorldEventType.HolyLight)
            return;

        WorldEventData holy = activeEvent;
        holyCollapsed = true;
        activeRoundsRemaining = 0;
        holyPenaltySource = holy;
        holyPenaltyRoundsRemaining = Mathf.Max(1, holy.collapsePenaltyRounds);
        holyPenaltyStartsNextWave = currentWaveEffectsRunning;
        pendingHolyCollapseAnnouncement = holy;

        SpawnHolyCollapseVfx(holy);
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
        holyCollapseRolledThisRound = false;
        cooldownRemaining = Mathf.Max(cooldownRemaining, cooldownWavesAfterEvent);
    }

    private void StartContinuousEffectForCurrentWave()
    {
        StopContinuousEffect();
        holyCollapseRolledThisRound = false;

        if (!currentWaveEffectsRunning || activeEvent == null)
            return;

        if (activeEvent.eventType == WorldEventType.DogCatRain)
            continuousEffectRoutine = StartCoroutine(DogCatRainRoutine(activeEvent));
        else if (activeEvent.eventType == WorldEventType.MeteorShower)
            continuousEffectRoutine = StartCoroutine(MeteorShowerRoutine(activeEvent));
        else if (activeEvent.eventType == WorldEventType.HolyLight)
            continuousEffectRoutine = StartCoroutine(HolyLightCollapseRoutine(activeEvent));
    }

    private void StopContinuousEffect()
    {
        if (continuousEffectRoutine == null)
            return;

        StopCoroutine(continuousEffectRoutine);
        continuousEffectRoutine = null;
    }

    private IEnumerator HolyLightCollapseRoutine(WorldEventData data)
    {
        if (data == null)
        {
            continuousEffectRoutine = null;
            yield break;
        }

        // Collapse is a per-round event. Wait until combat really starts so a 100%
        // chance visibly collapses during the affected round, not during announcement/portal setup.
        while (currentWaveEffectsRunning && activeEvent == data && !HasLivingEnemies())
            yield return null;

        if (!currentWaveEffectsRunning || activeEvent != data || holyCollapsed)
        {
            continuousEffectRoutine = null;
            yield break;
        }

        float chance = Mathf.Clamp01(data.holyCollapseChancePerRound);
        holyCollapseRolledThisRound = chance >= 1f || (chance > 0f && Random.value < chance);

        if (!holyCollapseRolledThisRound)
        {
            continuousEffectRoutine = null;
            yield break;
        }

        float minDelay = Mathf.Max(0f, data.holyCollapseMinDelay);
        float maxDelay = Mathf.Max(minDelay, data.holyCollapseMaxDelay);
        float delay = Random.Range(minDelay, maxDelay);
        float elapsed = 0f;

        while (elapsed < delay && currentWaveEffectsRunning && activeEvent == data && !holyCollapsed)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentWaveEffectsRunning && activeEvent == data && !holyCollapsed)
            TriggerHolyCollapse();

        holyCollapseRolledThisRound = false;
        continuousEffectRoutine = null;
    }

    private IEnumerator DogCatRainRoutine(WorldEventData data)
    {
        yield return null;

        while (currentWaveEffectsRunning && activeEvent == data)
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
        int goldAmount = Mathf.Max(0, data.goldPerDrop);

        GameObject drop = data.goldDropPrefab != null
            ? Instantiate(data.goldDropPrefab, start, Random.rotation)
            : null;

        float duration = Mathf.Max(0.05f, data.goldDropFallDuration);

        if (drop == null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddGold(goldAmount);
            return;
        }

        Transform dropTransform = drop.transform;
        dropTransform.DOMove(end, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.AddGold(goldAmount);

                if (drop != null)
                    Destroy(drop);
            });

        dropTransform.DORotate(
            new Vector3(Random.Range(90f, 360f), Random.Range(90f, 360f), Random.Range(90f, 360f)),
            duration,
            RotateMode.LocalAxisAdd);
    }

    private IEnumerator MeteorShowerRoutine(WorldEventData data)
    {
        yield return null;

        while (currentWaveEffectsRunning && activeEvent == data)
        {
            if (HasLivingEnemies() && Random.value < data.meteorChancePerTick)
                SpawnMeteor(data);

            yield return new WaitForSeconds(Mathf.Max(0.05f, data.meteorTickInterval));
        }

        continuousEffectRoutine = null;
    }

    private void SpawnMeteor(WorldEventData data)
    {
        Tower exactTowerTarget = RollMeteorTowerTarget(data);
        Vector3 end = exactTowerTarget != null
            ? exactTowerTarget.transform.position
            : ChooseMeteorEnemyOrMapImpactPoint(data);

        Vector3 start = end + Vector3.up * data.meteorSpawnHeight + new Vector3(-data.meteorSpawnHeight * 0.35f, 0f, 0f);
        float duration = Mathf.Max(0.05f, data.meteorFallDuration);

        GameObject meteor = data.meteorPrefab != null
            ? Instantiate(data.meteorPrefab, start, Quaternion.identity)
            : null;

        if (meteor == null)
        {
            ResolveMeteorImpact(end, data, exactTowerTarget);
            return;
        }

        Vector3 direction = end - start;
        if (direction.sqrMagnitude > 0.001f)
            meteor.transform.rotation = Quaternion.LookRotation(direction.normalized);

        meteor.transform.DOMove(end, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                ResolveMeteorImpact(end, data, exactTowerTarget);
                if (meteor != null)
                    Destroy(meteor);
            });
    }

    private Tower RollMeteorTowerTarget(WorldEventData data)
    {
        if (data == null || data.meteorTargetTowerChance <= 0f)
            return null;
        if (Random.value >= Mathf.Clamp01(data.meteorTargetTowerChance))
            return null;

        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        Tower chosen = null;
        int validCount = 0;

        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null || !tower.isActiveAndEnabled || !tower.gameObject.activeInHierarchy)
                continue;

            validCount++;
            if (Random.Range(0, validCount) == 0)
                chosen = tower;
        }

        return chosen;
    }

    private Vector3 ChooseMeteorEnemyOrMapImpactPoint(WorldEventData data)
    {
        if (data != null && data.meteorTargetEnemyChance > 0f &&
            Random.value < Mathf.Clamp01(data.meteorTargetEnemyChance))
        {
            Enemy target = GetRandomLivingEnemy();
            if (target != null)
            {
                Vector2 scatter = Random.insideUnitCircle * Mathf.Max(0f, data.meteorTargetScatterRadius);
                Vector3 point = target.transform.position + new Vector3(scatter.x, 0f, scatter.y);
                point.y = worldGroundY;
                return point;
            }
        }

        return RandomGroundPoint(data != null ? data.meteorAreaSize : Vector2.zero);
    }

    private void ResolveMeteorImpact(Vector3 point, WorldEventData data, Tower exactTowerTarget)
    {
        float radius = Mathf.Max(0.1f, data.meteorHitRadius);
        float radiusSq = radius * radius;
        HashSet<Enemy> enemies = new HashSet<Enemy>();
        HashSet<Tower> towers = new HashSet<Tower>();

        if (exactTowerTarget != null && exactTowerTarget.isActiveAndEnabled && exactTowerTarget.gameObject.activeInHierarchy)
            towers.Add(exactTowerTarget);

        Collider[] hits = Physics.OverlapSphere(point, radius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null && enemy.IsAlive)
                enemies.Add(enemy);

            Tower tower = hit.GetComponentInParent<Tower>();
            if (tower != null)
                towers.Add(tower);
        }

        Enemy[] livingEnemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < livingEnemies.Length; i++)
        {
            Enemy enemy = livingEnemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            Vector3 delta = enemy.transform.position - point;
            delta.y = 0f;
            if (delta.sqrMagnitude <= radiusSq)
                enemies.Add(enemy);
        }

        IReadOnlyList<Tower> activeTowers = Tower.ActiveTowers;
        for (int i = 0; i < activeTowers.Count; i++)
        {
            Tower tower = activeTowers[i];
            if (tower == null)
                continue;

            Vector3 delta = tower.transform.position - point;
            delta.y = 0f;
            if (delta.sqrMagnitude <= radiusSq)
                towers.Add(tower);
        }

        foreach (Enemy enemy in enemies)
            enemy.TakeDamage(enemy.MaxHP * Mathf.Clamp01(data.meteorEnemyMaxHpDamagePercent));

        foreach (Tower tower in towers)
            tower.ApplyTemporaryAttackSpeedPenalty(
                data.meteorTowerAttackSpeedPenaltyPercent,
                data.meteorTowerDebuffDuration);

        SpawnMeteorImpactVfx(point, data);
    }

    private void SpawnMeteorImpactVfx(Vector3 point, WorldEventData data)
    {
        if (data == null || data.meteorImpactVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(data.meteorImpactVfxPrefab, point, Quaternion.identity);
        PooledVFXAutoRelease autoRelease = vfx.GetComponent<PooledVFXAutoRelease>();
        if (autoRelease != null)
        {
            autoRelease.PlayAndSchedule();
            return;
        }

        ParticleSystem particle = vfx.GetComponentInChildren<ParticleSystem>();
        float lifetime = 3f;
        if (particle != null)
            lifetime = Mathf.Max(0.5f, particle.main.duration + particle.main.startLifetime.constantMax);
        Destroy(vfx, lifetime);
    }

    private Enemy GetRandomLivingEnemy()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Enemy chosen = null;
        int validCount = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
                continue;

            validCount++;
            if (Random.Range(0, validCount) == 0)
                chosen = enemy;
        }

        return chosen;
    }

    private bool HasLivingEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].IsAlive)
                return true;
        }

        return false;
    }

    private Vector3 RandomGroundPoint(Vector2 size)
    {
        Vector3 center = worldCenter != null ? worldCenter.position : Vector3.zero;
        center.y = worldGroundY;

        return center + new Vector3(
            Random.Range(-size.x * 0.5f, size.x * 0.5f),
            0f,
            Random.Range(-size.y * 0.5f, size.y * 0.5f));
    }

    public float ApplyTowerDamage(float value)
    {
        if (IsHolyLightActive)
            value *= 1f + Mathf.Max(0f, activeEvent.holyTowerDamageBonusPercent);
        return value;
    }

    public float ApplyTowerAttackSpeed(float value)
    {
        if (IsHolyLightActive)
            value *= 1f + Mathf.Max(0f, activeEvent.holyTowerAttackSpeedBonusPercent);
        return value;
    }

    public float ApplyProjectileSpeed(float value)
    {
        if (IsHolyLightActive)
            value *= 1f + Mathf.Max(0f, activeEvent.holyProjectileSpeedBonusPercent);
        return value;
    }

    public float GetEnemyMaxHpMultiplier()
    {
        float multiplier = 1f;

        if (IsDogCatRainActive)
            multiplier *= 1f + Mathf.Max(0f, activeEvent.enemyMaxHpBonusPercent);

        if (IsHolyPenaltyActive)
            multiplier *= 1f + Mathf.Max(0f, holyPenaltySource.collapseEnemyMaxHpBonusPercent);

        return multiplier;
    }

    public float GetEnemyCCResistanceBonus()
    {
        if (!IsHolyPenaltyActive)
            return 0f;

        return Mathf.Clamp01(holyPenaltySource.collapseEnemyCCResistanceBonusPercent);
    }

    public float GetEnemySpawnShieldPercent()
    {
        if (!IsHolyPenaltyActive)
            return 0f;

        return Mathf.Max(0f, holyPenaltySource.collapseEnemyShieldPercentOfMaxHp);
    }

    private IEnumerator PresentAnnouncement(WorldEventData data, bool collapsed)
    {
        if (data == null)
            yield break;

        isPresentingAnnouncement = true;
        CaptureHUDPositions();
        yield return AnimateHUD(false);

        if (announcementRoot != null)
        {
            announcementRoot.gameObject.SetActive(true);
            announcementRoot.alpha = 0f;

            RectTransform rect = announcementRoot.transform as RectTransform;
            Vector2 basePos = rect != null ? rect.anchoredPosition : Vector2.zero;
            if (rect != null)
                rect.anchoredPosition = basePos + announcementEnterOffset;

            Color accent = collapsed
                ? new Color(1f, 0.42f, 0.18f, 1f)
                : data.accentColor;

            if (rarityText != null)
            {
                rarityText.text = collapsed
                    ? "EVENT COLLAPSED"
                    : data.rarity.ToString().ToUpperInvariant() + " EVENT";
                rarityText.color = accent;
            }

            if (titleText != null)
            {
                titleText.text = collapsed ? "HOLY LIGHT FADES" : data.eventName;
                titleText.color = accent;
            }

            if (descriptionText != null)
            {
                descriptionText.text = collapsed
                    ? BuildHolyCollapseDescription(data)
                    : data.description;
                descriptionText.color = Color.white;
            }

            if (iconImage != null)
            {
                iconImage.sprite = data.icon;
                iconImage.color = Color.white;
                iconImage.enabled = data.icon != null;
            }

            PlayAnnouncementSfx(data, collapsed);

            Sequence enter = DOTween.Sequence().SetUpdate(true);
            enter.Join(announcementRoot.DOFade(1f, announcementEnterDuration));
            if (rect != null)
                enter.Join(rect.DOAnchorPos(basePos, announcementEnterDuration).SetEase(announcementEnterEase));
            yield return enter.WaitForCompletion();

            if (announcementHoldDuration > 0f)
                yield return new WaitForSecondsRealtime(announcementHoldDuration);

            Tween exit = announcementRoot
                .DOFade(0f, announcementExitDuration)
                .SetEase(announcementExitEase)
                .SetUpdate(true);
            yield return exit.WaitForCompletion();

            announcementRoot.gameObject.SetActive(false);
            if (rect != null)
                rect.anchoredPosition = basePos;
        }
        else if (announcementHoldDuration > 0f)
        {
            PlayAnnouncementSfx(data, collapsed);
            yield return new WaitForSecondsRealtime(announcementHoldDuration);
        }

        yield return AnimateHUD(true);
        isPresentingAnnouncement = false;
    }

    private string BuildHolyCollapseDescription(WorldEventData data)
    {
        int rounds = Mathf.Max(1, data.collapsePenaltyRounds);
        int hp = Mathf.RoundToInt(Mathf.Max(0f, data.collapseEnemyMaxHpBonusPercent) * 100f);
        int resist = Mathf.RoundToInt(Mathf.Clamp01(data.collapseEnemyCCResistanceBonusPercent) * 100f);
        int shield = Mathf.RoundToInt(Mathf.Max(0f, data.collapseEnemyShieldPercentOfMaxHp) * 100f);

        return $"The blessing has vanished. For {rounds} round(s), enemies gain +{hp}% Max HP, +{resist}% effect resistance and a {shield}% Max HP shield.";
    }

    private void PlayAnnouncementSfx(WorldEventData data, bool collapsed)
    {
        if (data == null)
            return;

        AudioClip clip = collapsed && data.holyCollapseSfx != null
            ? data.holyCollapseSfx
            : data.announcementSfx;

        if (clip == null)
            return;

        if (eventAudioSource != null)
        {
            eventAudioSource.Stop();
            eventAudioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(
                clip,
                Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    private IEnumerator AnimateHUD(bool show)
    {
        CaptureHUDPositions();
        float duration = Mathf.Max(0f, hudSlideDuration);
        bool any = false;

        if (duration <= 0f)
        {
            for (int i = 0; i < hudTargets.Count; i++)
            {
                HUDTarget item = hudTargets[i];
                if (item == null || item.target == null || !item.captured)
                    continue;

                item.target.anchoredPosition = show
                    ? item.shownPosition
                    : item.shownPosition + item.hiddenOffset;
            }
            yield break;
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < hudTargets.Count; i++)
        {
            HUDTarget item = hudTargets[i];
            if (item == null || item.target == null || !item.captured)
                continue;

            item.target.DOKill();
            Vector2 target = show
                ? item.shownPosition
                : item.shownPosition + item.hiddenOffset;

            sequence.Join(item.target.DOAnchorPos(target, duration).SetEase(Ease.InOutCubic));
            any = true;
        }

        if (!any)
        {
            sequence.Kill();
            yield break;
        }

        yield return sequence.WaitForCompletion();
    }

    private void CaptureHUDPositions()
    {
        if (hudTargets == null)
            return;

        for (int i = 0; i < hudTargets.Count; i++)
        {
            HUDTarget item = hudTargets[i];
            if (item == null || item.target == null || item.captured)
                continue;

            item.shownPosition = item.target.anchoredPosition;
            item.captured = true;
        }
    }

    private void RestoreHUDInstant()
    {
        if (hudTargets == null)
            return;

        for (int i = 0; i < hudTargets.Count; i++)
        {
            HUDTarget item = hudTargets[i];
            if (item == null || item.target == null || !item.captured)
                continue;

            item.target.DOKill();
            item.target.anchoredPosition = item.shownPosition;
        }

        if (announcementRoot != null)
        {
            announcementRoot.DOKill();
            announcementRoot.alpha = 0f;
            announcementRoot.gameObject.SetActive(false);
        }

        isPresentingAnnouncement = false;
    }

    private void CaptureLightState()
    {
        if (holyDirectionalLight == null || lightStateCaptured)
            return;

        normalLightIntensity = holyDirectionalLight.intensity;
        normalLightColor = holyDirectionalLight.color;
        lightStateCaptured = true;
    }

    private void BeginHolyVisuals(WorldEventData data)
    {
        CaptureLightState();

        if (holyDirectionalLight != null)
        {
            DOTween.Kill(holyDirectionalLight);
            DOTween.To(
                    () => holyDirectionalLight.intensity,
                    value => holyDirectionalLight.intensity = value,
                    holyLightIntensity,
                    holyLightFadeDuration)
                .SetUpdate(true)
                .SetTarget(holyDirectionalLight);

            DOTween.To(
                    () => holyDirectionalLight.color,
                    value => holyDirectionalLight.color = value,
                    holyLightColor,
                    holyLightFadeDuration)
                .SetUpdate(true)
                .SetTarget(holyDirectionalLight);
        }

        if (data.holyLightVisualPrefab == null || holyVisualInstance != null)
            return;

        Vector3 position = (worldCenter != null ? worldCenter.position : Vector3.zero) + data.holyLightVisualOffset;
        holyVisualInstance = Instantiate(data.holyLightVisualPrefab, position, Quaternion.identity);
        EnsureHolyVisualMotion(holyVisualInstance);

        Vector3 authoredScale = holyVisualInstance.transform.localScale;
        holyVisualInstance.transform.localScale = Vector3.zero;
        holyVisualInstance.transform
            .DOScale(authoredScale, Mathf.Max(0.05f, holyLightFadeDuration))
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    private static void EnsureHolyVisualMotion(GameObject instance)
    {
        if (instance == null || instance.GetComponent<WorldEventVisualAnimator>() != null)
            return;

        Transform baseHalo = FindWorldEventChild(instance.transform, "BaseHalo");
        Transform haloA = FindWorldEventChild(instance.transform, "HaloVertical_A");
        Transform haloB = FindWorldEventChild(instance.transform, "HaloVertical_B");
        Transform crownHalo = FindWorldEventChild(instance.transform, "HaloCrown");
        Transform crystal = FindWorldEventChild(instance.transform, "HolyCrystal");

        if (baseHalo == null && haloA == null && haloB == null && crownHalo == null && crystal == null)
            return;

        WorldEventVisualAnimator animator = instance.AddComponent<WorldEventVisualAnimator>();
        animator.rotatingParts = new[]
        {
            new WorldEventVisualAnimator.RotatingPart { target = baseHalo, degreesPerSecond = new Vector3(0f, 22f, 0f) },
            new WorldEventVisualAnimator.RotatingPart { target = haloA, degreesPerSecond = new Vector3(28f, 0f, 0f) },
            new WorldEventVisualAnimator.RotatingPart { target = haloB, degreesPerSecond = new Vector3(0f, 0f, -34f) },
            new WorldEventVisualAnimator.RotatingPart { target = crownHalo, degreesPerSecond = new Vector3(0f, 42f, 0f) }
        };
        animator.pulseTarget = crystal;
        animator.pulseAmount = 0.055f;
        animator.pulseSpeed = 0.82f;
        animator.floatTarget = instance.transform;
        animator.floatHeight = 0.10f;
        animator.floatSpeed = 0.42f;
        animator.Recapture();
    }

    private static Transform FindWorldEventChild(Transform root, string childName)
    {
        if (root == null)
            return null;
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindWorldEventChild(root.GetChild(i), childName);
            if (found != null)
                return found;
        }
        return null;
    }

    private void EndHolyVisuals()
    {
        if (holyDirectionalLight != null && lightStateCaptured)
        {
            DOTween.Kill(holyDirectionalLight);
            DOTween.To(
                    () => holyDirectionalLight.intensity,
                    value => holyDirectionalLight.intensity = value,
                    normalLightIntensity,
                    holyLightFadeDuration)
                .SetUpdate(true)
                .SetTarget(holyDirectionalLight);

            DOTween.To(
                    () => holyDirectionalLight.color,
                    value => holyDirectionalLight.color = value,
                    normalLightColor,
                    holyLightFadeDuration)
                .SetUpdate(true)
                .SetTarget(holyDirectionalLight);
        }

        if (holyVisualInstance == null)
            return;

        GameObject instance = holyVisualInstance;
        holyVisualInstance = null;
        instance.transform.DOKill();
        instance.transform
            .DOScale(Vector3.zero, Mathf.Max(0.05f, holyLightFadeDuration))
            .SetEase(Ease.InCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (instance != null)
                    Destroy(instance);
            });
    }

    private void RestoreHolyLightInstant()
    {
        if (holyDirectionalLight != null && lightStateCaptured)
        {
            DOTween.Kill(holyDirectionalLight);
            holyDirectionalLight.intensity = normalLightIntensity;
            holyDirectionalLight.color = normalLightColor;
        }

        if (holyVisualInstance != null)
        {
            holyVisualInstance.transform.DOKill();
            Destroy(holyVisualInstance);
            holyVisualInstance = null;
        }
    }

    private void SpawnHolyCollapseVfx(WorldEventData data)
    {
        if (data == null || data.holyCollapseVfxPrefab == null)
            return;

        Vector3 position = (worldCenter != null ? worldCenter.position : Vector3.zero) + data.holyLightVisualOffset;
        GameObject vfx = Instantiate(data.holyCollapseVfxPrefab, position, Quaternion.identity);

        ParticleSystem particle = vfx.GetComponentInChildren<ParticleSystem>();
        float lifetime = 3f;
        if (particle != null)
            lifetime = Mathf.Max(0.5f, particle.main.duration + particle.main.startLifetime.constantMax);
        Destroy(vfx, lifetime);
    }
}
