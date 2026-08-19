using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tower upgrade panel. Existing upgrade/sell/close behaviour is preserved.
/// Additional next-level fields are optional and are used by the clean right-side panel.
/// </summary>
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
    [Tooltip("Optional separate cost display, e.g. a small text next to a gold icon, independent from the button's own label")]
    public TMP_Text upgradeCostText;
    public Button sellButton;
    public TMP_Text sellButtonLabel;
    public Button closeButton;
    [Tooltip("Optional second Close button used by the clean UI layout.")]
    public Button secondaryCloseButton;

    [Header("Range Preview")]
    [Tooltip("A dedicated RangeIndicator instance used only for showing the selected tower's range - use a separate one from the placement ghost's")]
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

    private void Refresh()
    {
        if (selectedTower == null || selectedTower.data == null) return;
        TowerLevelStats stats = selectedTower.CurrentStats;

        if (towerNameText != null) towerNameText.text = selectedTower.data.towerName;
        if (levelText != null) levelText.text = $"Level {selectedTower.CurrentLevelNumber}";
        if (strengthText != null) strengthText.text = $"{stats.strength:0.#}";
        if (attackSpeedText != null) attackSpeedText.text = $"{stats.attackSpeed:0.##}/s";
        if (rangeText != null) rangeText.text = $"{stats.range:0.#}";

        if (selectedTower.CanUpgrade())
        {
            int nextIndex = selectedTower.CurrentLevelNumber;
            TowerLevelStats next = selectedTower.data.levels[nextIndex];
            int cost = selectedTower.GetNextUpgradeCost();

            if (nextLevelRoot != null) nextLevelRoot.SetActive(true);
            if (nextLevelTitleText != null) nextLevelTitleText.text = $"NEXT LEVEL ({selectedTower.CurrentLevelNumber + 1})";
            if (nextStrengthText != null) nextStrengthText.text = FormatNext(next.strength, next.strength - stats.strength);
            if (nextAttackSpeedText != null) nextAttackSpeedText.text = FormatNext(next.attackSpeed, next.attackSpeed - stats.attackSpeed, "/s");
            if (nextRangeText != null) nextRangeText.text = FormatNext(next.range, next.range - stats.range);

            if (upgradeButtonLabel != null) upgradeButtonLabel.text = "UPGRADE";
            if (upgradeCostText != null) upgradeCostText.text = cost.ToString();
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

        if (sellButtonLabel != null) sellButtonLabel.text = $"SELL  {selectedTower.GetSellValue()}";

        if (rangeIndicator != null)
            rangeIndicator.Show(selectedTower.transform.position, stats.range, rangeColor);
    }

    private static string FormatNext(float value, float delta, string suffix = "")
    {
        string sign = delta > 0.0001f ? "+" : string.Empty;
        return $"{value:0.##}{suffix}   <color=#55E86A>{sign}{delta:0.##}{suffix}</color>";
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

        GameManager.Instance?.AddGold(selectedTower.GetSellValue());
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
        if (panelRoot != null && panelRoot.activeSelf && selectedTower != null)
            Refresh();
    }
}
