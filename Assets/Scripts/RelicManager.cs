using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the current run's relic state. Relics are permanent until the scene/run is restarted.
/// Every N cleared waves it rolls 3 weighted, unique choices and asks RelicChoiceUI to display them.
/// </summary>
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    [Header("Timing")]
    [Min(1)] public int wavesPerChoice = 3;
    [Range(1, 5)] public int choicesPerRoll = 3;
    [Tooltip("Do not offer a relic after the final wave because the run is already over.")]
    public bool skipAfterFinalWave = true;

    [Header("Pool")]
    public List<RelicData> relicPool = new List<RelicData>();

    [Header("UI")]
    public RelicChoiceUI choiceUI;

    private readonly Dictionary<RelicData, int> stacks = new Dictionary<RelicData, int>();
    private bool isChoosing;

    private float towerDamagePercent;
    private float towerAttackSpeedPercent;
    private float towerRangePercent;
    private float goldGainPercent;
    private float buildCostDiscountPercent;
    private float upgradeCostDiscountPercent;

    public bool IsChoosing => isChoosing;
    public float TowerDamageMultiplier => Mathf.Max(0f, 1f + towerDamagePercent);
    public float TowerAttackSpeedMultiplier => Mathf.Max(0.01f, 1f + towerAttackSpeedPercent);
    public float TowerRangeMultiplier => Mathf.Max(0.01f, 1f + towerRangePercent);
    public float GoldGainMultiplier => Mathf.Max(0f, 1f + goldGainPercent);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        WaveManager.OnWaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveCleared -= HandleWaveCleared;
    }

    private void HandleWaveCleared()
    {
        if (isChoosing || WaveManager.Instance == null) return;

        int wave = WaveManager.Instance.CurrentWaveNumber;
        if (wave <= 0 || wave % Mathf.Max(1, wavesPerChoice) != 0) return;
        if (skipAfterFinalWave && wave >= WaveManager.Instance.TotalWaves) return;

        OpenChoice();
    }

    public void OpenChoice()
    {
        List<RelicData> rolled = RollChoices(choicesPerRoll);
        if (rolled.Count == 0) return;

        isChoosing = true;
        if (choiceUI != null)
            choiceUI.Show(rolled);
        else
            Debug.LogWarning("RelicManager rolled relics but no RelicChoiceUI is assigned.");
    }

    public void ChooseRelic(RelicData relic)
    {
        if (!isChoosing || relic == null) return;

        ApplyRelic(relic);
        isChoosing = false;
        if (choiceUI != null) choiceUI.Hide();
    }

    private void ApplyRelic(RelicData relic)
    {
        int currentStacks = GetStacks(relic);
        if (currentStacks >= Mathf.Max(1, relic.maxStacks)) return;
        stacks[relic] = currentStacks + 1;

        if (relic.modifiers == null) return;
        foreach (RelicModifier modifier in relic.modifiers)
        {
            if (modifier == null) continue;
            switch (modifier.effect)
            {
                case RelicEffectType.TowerDamagePercent:
                    towerDamagePercent += modifier.value;
                    break;
                case RelicEffectType.TowerAttackSpeedPercent:
                    towerAttackSpeedPercent += modifier.value;
                    break;
                case RelicEffectType.TowerRangePercent:
                    towerRangePercent += modifier.value;
                    break;
                case RelicEffectType.GoldGainPercent:
                    goldGainPercent += modifier.value;
                    break;
                case RelicEffectType.BuildCostDiscountPercent:
                    buildCostDiscountPercent += modifier.value;
                    break;
                case RelicEffectType.UpgradeCostDiscountPercent:
                    upgradeCostDiscountPercent += modifier.value;
                    break;
            }
        }
    }

    public int GetStacks(RelicData relic)
    {
        if (relic == null) return 0;
        return stacks.TryGetValue(relic, out int count) ? count : 0;
    }

    private List<RelicData> RollChoices(int count)
    {
        List<RelicData> candidates = new List<RelicData>();
        foreach (RelicData relic in relicPool)
        {
            if (relic == null) continue;
            if (GetStacks(relic) >= Mathf.Max(1, relic.maxStacks)) continue;
            candidates.Add(relic);
        }

        List<RelicData> result = new List<RelicData>();
        int targetCount = Mathf.Min(Mathf.Max(1, count), candidates.Count);
        while (result.Count < targetCount && candidates.Count > 0)
        {
            float totalWeight = 0f;
            foreach (RelicData candidate in candidates)
                totalWeight += Mathf.Max(0.01f, candidate.selectionWeight);

            float roll = Random.value * totalWeight;
            int selectedIndex = candidates.Count - 1;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= Mathf.Max(0.01f, candidates[i].selectionWeight);
                if (roll <= 0f) { selectedIndex = i; break; }
            }

            result.Add(candidates[selectedIndex]);
            candidates.RemoveAt(selectedIndex); // no duplicate card within the same 3-choice roll
        }

        return result;
    }

    public int GetBuildCost(int baseCost)
    {
        float discount = Mathf.Clamp(buildCostDiscountPercent, 0f, 0.90f);
        return Mathf.Max(0, Mathf.RoundToInt(baseCost * (1f - discount)));
    }

    public int GetUpgradeCost(int baseCost)
    {
        float discount = Mathf.Clamp(upgradeCostDiscountPercent, 0f, 0.90f);
        return Mathf.Max(0, Mathf.RoundToInt(baseCost * (1f - discount)));
    }

    public int ApplyGoldGain(int baseAmount)
    {
        if (baseAmount <= 0) return baseAmount;
        return Mathf.Max(0, Mathf.RoundToInt(baseAmount * GoldGainMultiplier));
    }

    public float ApplyDamage(float baseDamage) => baseDamage * TowerDamageMultiplier;
    public float ApplyAttackSpeed(float baseAttackSpeed) => baseAttackSpeed * TowerAttackSpeedMultiplier;
    public float ApplyRange(float baseRange) => baseRange * TowerRangeMultiplier;
}
