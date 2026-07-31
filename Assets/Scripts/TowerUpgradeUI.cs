using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The Upgrade UI. Opens automatically whenever a placed Tower is clicked (via
/// Tower.OnAnyTowerClicked), shows its current level and stats, and lets the
/// player spend gold to upgrade it (up to its max level) or sell it for a refund.
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

    [Header("Buttons")]
    public Button upgradeButton;
    public TMP_Text upgradeButtonLabel;
    [Tooltip("Optional separate cost display, e.g. a small text next to a gold icon, independent from the button's own label")]
    public TMP_Text upgradeCostText;
    public Button sellButton;
    public TMP_Text sellButtonLabel;
    public Button closeButton;

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
    }

    private void HandleTowerClicked(Tower tower)
    {
        selectedTower = tower;
        Refresh();
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void Refresh()
    {
        if (selectedTower == null) return;
        TowerLevelStats stats = selectedTower.CurrentStats;

        if (towerNameText != null) towerNameText.text = selectedTower.data.towerName;
        if (levelText != null) levelText.text = $"Level {selectedTower.CurrentLevelNumber}/{selectedTower.MaxLevelNumber}";
        if (strengthText != null) strengthText.text = $"Strength: {stats.strength:0.#}";
        if (attackSpeedText != null) attackSpeedText.text = $"Attack Speed: {stats.attackSpeed:0.##}/s";
        if (rangeText != null) rangeText.text = $"Range: {stats.range:0.#}";

        if (selectedTower.CanUpgrade())
        {
            int cost = selectedTower.GetNextUpgradeCost();
            if (upgradeButtonLabel != null) upgradeButtonLabel.text = $"Upgrade  ·  {cost}g";
            if (upgradeCostText != null) upgradeCostText.text = $"{cost}";
            if (upgradeButton != null)
                upgradeButton.interactable = GameManager.Instance != null && GameManager.Instance.CurrentGold >= cost;
        }
        else
        {
            if (upgradeButtonLabel != null) upgradeButtonLabel.text = "Max Level";
            if (upgradeCostText != null) upgradeCostText.text = "-";
            if (upgradeButton != null) upgradeButton.interactable = false;
        }

        if (sellButtonLabel != null) sellButtonLabel.text = $"Sell ({selectedTower.GetSellValue()}g)";

        if (rangeIndicator != null)
            rangeIndicator.Show(selectedTower.transform.position, stats.range, rangeColor);
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
        // Keep affordability / button state live while the panel is open (e.g. gold changes from other towers killing enemies)
        if (panelRoot != null && panelRoot.activeSelf && selectedTower != null)
            Refresh();
    }
}