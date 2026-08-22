using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Build menu binding layer.</summary>
public class BuildMenuUI : MonoBehaviour
{
    [System.Serializable]
    public class TowerButtonBinding
    {
        public TowerData towerData;
        public Button button;
        public TMP_Text label;
        [Header("Clean UI (optional)")]
        public TMP_Text nameText;
        public TMP_Text costText;
        public GameObject selectedFrame;
    }

    public TowerButtonBinding[] towerButtons;
    private TowerButtonBinding selectedBinding;

    private void Start()
    {
        foreach (var binding in towerButtons)
        {
            if (binding.button == null || binding.towerData == null) continue;
            TowerButtonBinding capturedBinding = binding;
            TowerData data = binding.towerData;
            binding.button.onClick.AddListener(() => SelectTower(capturedBinding, data));
            RefreshBinding(binding);
        }
        RefreshVisualState();
    }

    private void Update() => RefreshVisualState();

    private void SelectTower(TowerButtonBinding binding, TowerData data)
    {
        selectedBinding = binding;
        TowerPlacementManager.Instance?.SelectTowerToBuild(data);
        RefreshVisualState();
    }

    private int GetBuildCost(TowerData data)
    {
        if (data == null) return 0;
        return RelicManager.Instance != null ? RelicManager.Instance.GetBuildCost(data.buildCost) : data.buildCost;
    }

    private void RefreshBinding(TowerButtonBinding binding)
    {
        if (binding == null || binding.towerData == null) return;
        TowerData data = binding.towerData;
        string cost = CompactNumber.Format(GetBuildCost(data));

        if (binding.label != null) binding.label.text = $"{data.towerName}\n{cost}g";
        if (binding.nameText != null) binding.nameText.text = data.towerName;
        if (binding.costText != null) binding.costText.text = cost;
    }

    private void RefreshVisualState()
    {
        int gold = GameManager.Instance != null ? GameManager.Instance.CurrentGold : 0;
        bool placementActive = TowerPlacementManager.Instance != null && TowerPlacementManager.Instance.IsPlacing;

        foreach (var binding in towerButtons)
        {
            if (binding == null || binding.towerData == null) continue;
            RefreshBinding(binding);
            if (binding.button != null)
                binding.button.interactable = gold >= GetBuildCost(binding.towerData);
            if (binding.selectedFrame != null)
                binding.selectedFrame.SetActive(placementActive && binding == selectedBinding);
        }

        if (!placementActive) selectedBinding = null;
    }
}
