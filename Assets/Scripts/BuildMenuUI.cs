using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Build menu binding layer. Gameplay behaviour is unchanged: selecting a card still delegates
/// placement to TowerPlacementManager. Extra visual references are optional so old scenes keep working.
/// </summary>
public class BuildMenuUI : MonoBehaviour
{
    [System.Serializable]
    public class TowerButtonBinding
    {
        public TowerData towerData;
        public Button button;
        [Tooltip("Legacy combined label. Kept for backward compatibility.")]
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

    private void Update()
    {
        RefreshVisualState();
    }

    private void SelectTower(TowerButtonBinding binding, TowerData data)
    {
        selectedBinding = binding;
        TowerPlacementManager.Instance?.SelectTowerToBuild(data);
        RefreshVisualState();
    }

    private void RefreshBinding(TowerButtonBinding binding)
    {
        if (binding == null || binding.towerData == null) return;
        TowerData data = binding.towerData;

        if (binding.label != null)
            binding.label.text = $"{data.towerName}\n{data.buildCost}g";
        if (binding.nameText != null)
            binding.nameText.text = data.towerName;
        if (binding.costText != null)
            binding.costText.text = $"{data.buildCost}";
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
                binding.button.interactable = gold >= binding.towerData.buildCost;
            if (binding.selectedFrame != null)
                binding.selectedFrame.SetActive(placementActive && binding == selectedBinding);
        }

        if (!placementActive)
            selectedBinding = null;
    }
}
