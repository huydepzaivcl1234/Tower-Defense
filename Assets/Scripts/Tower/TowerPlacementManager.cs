using UnityEngine;

public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

    [Header("Setup")]
    public Camera raycastCamera;
    public WaypointPath path;
    public LayerMask groundLayerMask;
    public RangeIndicator placementRangeIndicator;

    [Header("Ghost Colors")]
    public Color validColor = new Color(0.15f, 1f, 0.25f, 1f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Overlap Prevention")]
    public float minSpacingBetweenTowers = 1.5f;

    public bool IsPlacing => selectedTowerData != null;

    private TowerData selectedTowerData;
    private GameObject ghostInstance;
    private bool currentPositionValid;
    private Vector3 currentGhostPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (raycastCamera == null) raycastCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsPlacing) return;
        UpdateGhost();

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        if (Input.GetMouseButtonDown(0) && currentPositionValid && !IsPointerOverUI())
            ConfirmPlacement();
    }

    public void SelectTowerToBuild(TowerData data)
    {
        if (data == null || data.towerPrefab == null) return;
        if (RelicManager.Instance != null && !RelicManager.Instance.CanBuildTower(data))
        {
            CancelPlacement();
            return;
        }
        CancelPlacement();
        selectedTowerData = data;
        SpawnGhost();
    }

    public void CancelPlacement()
    {
        selectedTowerData = null;
        if (ghostInstance != null) Destroy(ghostInstance);
        ghostInstance = null;
        if (placementRangeIndicator != null) placementRangeIndicator.Hide();
    }

    private void SpawnGhost()
    {
        ghostInstance = Instantiate(selectedTowerData.towerPrefab);
        Tower ghostTowerScript = ghostInstance.GetComponent<Tower>();
        if (ghostTowerScript != null) ghostTowerScript.enabled = false;
        foreach (var col in ghostInstance.GetComponentsInChildren<Collider>()) col.enabled = false;
        ghostInstance.SetActive(false);
    }

    private void UpdateGhost()
    {
        if (ghostInstance == null || raycastCamera == null) return;
        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        int mask = groundLayerMask.value != 0 ? groundLayerMask.value : ~0;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, mask))
        {
            currentGhostPosition = hit.point + Vector3.up * selectedTowerData.placementYOffset;
            ghostInstance.transform.position = currentGhostPosition;
            ghostInstance.SetActive(true);

            bool relicAllowsBuild = RelicManager.Instance == null || RelicManager.Instance.CanBuildTower(selectedTowerData);
            bool clearOfPath = path == null || !path.IsTooCloseToPath(hit.point);
            bool clearOfTowers = !IsTooCloseToOtherTowers(hit.point);
            bool clearOfBlockedObjects = !IsInsideBuildExclusionZone(hit.point);

            currentPositionValid = relicAllowsBuild && clearOfPath && clearOfTowers && clearOfBlockedObjects;
            Color tint = currentPositionValid ? validColor : invalidColor;
            ApplyGhostTint(tint);

            if (placementRangeIndicator != null)
            {
                float range = (selectedTowerData.levels != null && selectedTowerData.levels.Length > 0) ? selectedTowerData.levels[0].range : 0f;
                if (RelicManager.Instance != null) range = RelicManager.Instance.ApplyRange(selectedTowerData, range);
                placementRangeIndicator.Show(currentGhostPosition, range, tint);
            }
        }
        else
        {
            ghostInstance.SetActive(false);
            currentPositionValid = false;
            if (placementRangeIndicator != null) placementRangeIndicator.Hide();
        }
    }

    private void ApplyGhostTint(Color color)
    {
        foreach (var renderer in ghostInstance.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.color = color;
            }
        }
    }

    private int GetSelectedBuildCost()
    {
        if (selectedTowerData == null) return 0;
        return RelicManager.Instance != null ? RelicManager.Instance.GetBuildCost(selectedTowerData.buildCost) : selectedTowerData.buildCost;
    }

    private void ConfirmPlacement()
    {
        if (selectedTowerData == null) return;
        if (RelicManager.Instance != null && !RelicManager.Instance.CanBuildTower(selectedTowerData))
        {
            CancelPlacement();
            return;
        }

        int cost = GetSelectedBuildCost();
        if (GameManager.Instance == null || !GameManager.Instance.SpendGold(cost)) return;

        GameObject go = Instantiate(selectedTowerData.towerPrefab, currentGhostPosition, Quaternion.identity);
        Tower tower = go.GetComponent<Tower>();
        if (tower != null) tower.data = selectedTowerData;
        RelicManager.Instance?.NotifyTowerBuilt(selectedTowerData);

        Destroy(ghostInstance);
        if (RelicManager.Instance == null || RelicManager.Instance.CanBuildTower(selectedTowerData))
            SpawnGhost();
        else
            CancelPlacement();
    }

    private bool IsTooCloseToOtherTowers(Vector3 point)
    {
        foreach (var t in Tower.ActiveTowers)
        {
            if (t == null) continue;
            Vector3 diff = t.transform.position - point;
            diff.y = 0f;
            if (diff.magnitude < minSpacingBetweenTowers) return true;
        }
        return false;
    }

    private static bool IsInsideBuildExclusionZone(Vector3 point)
    {
        foreach (BuildExclusionZone zone in BuildExclusionZone.ActiveZones)
        {
            if (zone == null || !zone.isActiveAndEnabled) continue;
            if (zone.Contains(point)) return true;
        }
        return false;
    }

    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}
