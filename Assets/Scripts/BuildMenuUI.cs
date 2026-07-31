using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple always-visible build menu: one button per tower type. Clicking a button
/// "arms" that tower type so the next TowerPlacementSpot click builds it.
/// Set the Size of Tower Buttons to 3 in the Inspector (or however many tower types you have).
/// </summary>
public class BuildMenuUI : MonoBehaviour
{
    [System.Serializable]
    public class TowerButtonBinding
    {
        public TowerData towerData;
        public Button button;
        [Tooltip("Optional label, auto-filled with name + cost on Start")]
        public TMP_Text label;
    }

    public TowerButtonBinding[] towerButtons;

    private void Start()
    {
        foreach (var binding in towerButtons)
        {
            if (binding.button == null || binding.towerData == null) continue;
            TowerData data = binding.towerData; // local copy so the closure captures the right value
            binding.button.onClick.AddListener(() => TowerPlacementManager.Instance?.SelectTowerToBuild(data));
            if (binding.label != null)
                binding.label.text = $"{data.towerName}\n{data.buildCost}g";
        }
    }
}
