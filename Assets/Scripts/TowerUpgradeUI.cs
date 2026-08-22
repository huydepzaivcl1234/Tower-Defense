using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerUpgradeUI : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("Info Text")]
    public TMP_Text towerNameText;
    public TMP_Text levelText;
    public TMP_Text strengthText;
    public TMP_Text attackSpeedText;
    public TMP_Text rangeText;

    [Header("Next Level (optional)")]
    public GameObject nextLevelRoot;
    public TMP_Text nextLevelTitleText;
    public TMP_Text nextStrengthText;
    public TMP_Text nextAttackSpeedText;
    public TMP_Text nextRangeText;

    [Header("Buttons")]
    public Button upgradeButton;
    public TMP_Text upgradeButtonLabel;
    public TMP_Text upgradeCostText;
    public Button sellButton;
    public TMP_Text sellButtonLabel;
    public Button closeButton;
    public Button secondaryCloseButton;

    [Header("Range Preview")]
    public RangeIndicator rangeIndicator;
    public Color rangeColor = new Color(0.3f, 0.75f, 1f, 1f);

    private Tower selectedTower;

    private void OnEnable() => Tower.OnAnyTowerClicked += HandleTowerClicked;
    private void OnDisable() => Tower.OnAnyTowerClicked -= HandleTowerClicked;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradePressed);
        if (sellButton != null) sellButton.onClick.AddListener(OnSellPressed);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (secondaryCloseButton != null) secondaryCloseButton.onClick.AddListener(ClosePanel);
    }

    private void HandleTowerClicked(Tower tower)
    {
        selectedTower = tower;
        Refresh();
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private float Damage(float value) => RelicManager.Instance != null ? RelicManager.Instance.ApplyDamage(value) : value;
    private float Speed(float value) => RelicManager.Instance != null ? RelicManager.Instance.ApplyAttackSpeed(value) : value;
    private float Range(float value) => RelicManager.Instance != null ? RelicManager.Instance.ApplyRange(value) : value;

    private void Refresh()
    {
        if (selectedTower == null || selectedTower.data == null) return;
        TowerLevelStats stats = selectedTower.CurrentStats;

        float curDamage = Damage(stats.strength);
        float curSpeed = Speed(stats.attackSpeed);
        float curRange = Range(stats.range);

        if (towerNameText != null) towerNameText.text = selectedTower.data.towerName;
        if (levelText != null) levelText.text = $"Level {selectedTower.CurrentLevelNumber}";
        if (strengthText != null) strengthText.text = CompactNumber.Format(curDamage);
        if (attackSpeedText != null) attackSpeedText.text = $"{CompactNumber.Format(curSpeed)}/s";
        if (rangeText != null) rangeText.text = CompactNumber.Format(curRange);

        if (selectedTower.CanUpgrade())
        {
            int nextIndex = selectedTower.CurrentLevelNumber;
            TowerLevelStats next = selectedTower.data.levels[nextIndex];
            int cost = selectedTower.GetNextUpgradeCost();

            float nextDamage = Damage(next.strength);
            float nextSpeed = Speed(next.attackSpeed);
            float nextRange = Range(next.range);

            if (nextLevelRoot != null) nextLevelRoot.SetActive(true);
            if (nextLevelTitleText != null) nextLevelTitleText.text = $"NEXT LEVEL ({selectedTower.CurrentLevelNumber + 1})";
            if (nextStrengthText != null) nextStrengthText.text = FormatNext(nextDamage, nextDamage - curDamage);
            if (nextAttackSpeedText != null) nextAttackSpeedText.text = FormatNext(nextSpeed, nextSpeed - curSpeed, "/s");
            if (nextRangeText != null) nextRangeText.text = FormatNext(nextRange, nextRange - curRange);

            if (upgradeButtonLabel != null) upgradeButtonLabel.text = "UPGRADE";
            if (upgradeCostText != null) upgradeCostText.text = CompactNumber.Format(cost);
            if (upgradeButton != null)
                upgradeButton.interactable = GameManager.Instance != null && GameManager.Instance.CurrentGold >= cost;
        }
        else
        {
            if (nextLevelRoot != null) nextLevelRoot.SetActive(false);
            if (upgradeButtonLabel != null) upgradeButtonLabel.text = "MAX LEVEL";
            if (upgradeCostText != null) upgradeCostText.text = "-";
            if (upgradeButton != null) upgradeButton.interactable = false;
        }

        if (sellButtonLabel != null) sellButtonLabel.text = $"SELL  {CompactNumber.Format(selectedTower.GetSellValue())}";
        if (rangeIndicator != null)
            rangeIndicator.Show(selectedTower.transform.position, curRange, rangeColor);
    }

    private static string FormatNext(float value, float delta, string suffix = "")
    {
        string sign = delta > 0.0001f ? "+" : string.Empty;
        return $"{CompactNumber.Format(value)}{suffix}   <color=#55E86A>{sign}{CompactNumber.Format(delta)}{suffix}</color>";
    }

    private void OnUpgradePressed()
    {
        if (selectedTower == null || !selectedTower.CanUpgrade()) return;
        int cost = selectedTower.GetNextUpgradeCost();
        if (GameManager.Instance == null || !GameManager.Instance.SpendGold(cost)) return;
        selectedTower.Upgrade();
        Refresh();
    }

    private void OnSellPressed()
    {
        if (selectedTower == null) return;
        GameManager.Instance?.AddGold(selectedTower.GetSellValue(), false);
        if (selectedTower.occupiedSpot != null) selectedTower.occupiedSpot.ClearSpot();
        Destroy(selectedTower.gameObject);
        ClosePanel();
    }

    private void ClosePanel()
    {
        selectedTower = null;
        if (panelRoot != null) panelRoot.SetActive(false);
        if (rangeIndicator != null) rangeIndicator.Hide();
    }

    private void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf && selectedTower != null) Refresh();
    }
}
