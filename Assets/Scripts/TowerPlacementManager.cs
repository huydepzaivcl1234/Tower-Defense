using UnityEngine;

/// <summary>
/// Free placement (no PlaceBox spots needed): pick a tower type from the Build Menu, a
/// tinted ghost follows your mouse across the ground (green = valid, red = too close to the
/// enemy path), left-click to place. Placement mode STAYS ON after each build so you can drop
/// several of the same tower without re-clicking the build button - right-click or Escape
/// cancels/exits. Clicking an existing tower also cancels placement, since that click clearly
/// means "select that tower", not "place here".
/// </summary>
public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("Defaults to Camera.main if left empty")]
    public Camera raycastCamera;
    [Tooltip("The map's WaypointPath, used to block placement too close to it")]
    public WaypointPath path;
    [Tooltip("Layer(s) the ground/terrain is on. If left as 'Nothing' it falls back to raycasting everything.")]
    public LayerMask groundLayerMask;
    [Tooltip("A dedicated RangeIndicator instance used only for the placement ghost preview")]
    public RangeIndicator placementRangeIndicator;

    [Header("Ghost Colors")]
    public Color validColor = new Color(0.15f, 1f, 0.25f, 1f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Overlap Prevention")]
    [Tooltip("Minimum allowed ground distance between this ghost and any already-placed tower")]
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

    /// <summary>Called by BuildMenuUI when the player picks a tower type to place.</summary>
    public void SelectTowerToBuild(TowerData data)
    {
        if (data == null || data.towerPrefab == null) return;
        CancelPlacement(); // clear out any previous ghost/selection first
        selectedTowerData = data;
        SpawnGhost();
    }

    /// <summary>Exits placement mode entirely (right-click / Escape / picking a new type / clicking a tower).</summary>
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
        if (ghostTowerScript != null) ghostTowerScript.enabled = false; // don't shoot / don't react to clicks

        foreach (var col in ghostInstance.GetComponentsInChildren<Collider>())
            col.enabled = false; // ghost must never block raycasts or receive clicks itself

        ghostInstance.SetActive(false); // hidden until the first valid raycast hit below
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

            currentPositionValid = (path == null || !path.IsTooCloseToPath(hit.point)) &&
                                    !IsTooCloseToOtherTowers(hit.point);
            Color tint = currentPositionValid ? validColor : invalidColor;
            ApplyGhostTint(tint);

            if (placementRangeIndicator != null)
            {
                float range = (selectedTowerData.levels != null && selectedTowerData.levels.Length > 0)
                    ? selectedTowerData.levels[0].range : 0f;
                placementRangeIndicator.Show(currentGhostPosition, range, tint);
            }
        }
        else
        {
            // mouse isn't over the ground at all (e.g. pointing at the sky) - hide until it is again
            ghostInstance.SetActive(false);
            currentPositionValid = false;
            if (placementRangeIndicator != null) placementRangeIndicator.Hide();
        }
    }

    private void ApplyGhostTint(Color color)
    {
        foreach (var renderer in ghostInstance.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials) // .materials (not sharedMaterials) instances them per-ghost
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); // URP Lit
                else if (mat.HasProperty("_Color")) mat.color = color;                 // Built-in Standard
            }
        }
    }

    private void ConfirmPlacement()
    {
        if (GameManager.Instance == null || !GameManager.Instance.SpendGold(selectedTowerData.buildCost)) return;

        GameObject go = Instantiate(selectedTowerData.towerPrefab, currentGhostPosition, Quaternion.identity);
        Tower tower = go.GetComponent<Tower>();
        if (tower != null) tower.data = selectedTowerData;

        // Stay in placement mode with a fresh ghost of the same type - no need to re-click Build.
        // Want it to stop after one instead? Replace the two lines below with: CancelPlacement();
        Destroy(ghostInstance);
        SpawnGhost();
    }

    private bool IsTooCloseToOtherTowers(Vector3 point)
    {
        foreach (var t in Tower.ActiveTowers)
        {
            if (t == null) continue;
            Vector3 diff = t.transform.position - point;
            diff.y = 0f; // ground-plane distance only
            if (diff.magnitude < minSpacingBetweenTowers) return true;
        }
        return false;
    }

    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}