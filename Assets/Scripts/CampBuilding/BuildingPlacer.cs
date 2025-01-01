using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BuildingPlacer : MonoBehaviour
{
    private static BuildingPlacer _instance;

    public static BuildingPlacer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<BuildingPlacer>();
                if (_instance == null)
                {
                    Debug.LogError("BuildingPlacer instance not found in the scene!");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    private GameObject currentPreview;
    private BuildingScriptableObj selectedBuildingObj;
    private Vector3 currentGridPosition;
    private float gridSize = 1f; // Size of each grid cell

    [Header("Grid Settings")]
    public GameObject gridPrefab;                       // Prefab to use for grid sections
    public Vector2 gridDimensions = new Vector2(20, 20); // Width and height of the grid
    public Vector3 gridOrigin = Vector3.zero;           // Starting position of the grid
    public Transform gridParent;                        // Parent object to hold grid prefabs
    private Dictionary<Vector3, GameObject> gridObjects; // Stores grid cells for enabling/disabling dynamically

    private void OnEnable()
    {
        PlayerInput.Instance.OnUpdatePlayerControls += HandleControlTypeUpdate;
    }

    private void OnDisable()
    {
        if(PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= HandleControlTypeUpdate;
    }

    private void HandleControlTypeUpdate(PlayerControlType controlType)
    {
        if (controlType == PlayerControlType.BUILDING)
        {
            EnableGrid();
            // Subscribe to building-related inputs
            PlayerInput.Instance.OnLeftJoystick += HandleJoystickMovement;
            PlayerInput.Instance.OnAPressed += PlaceBuilding;
            PlayerInput.Instance.OnBPressed += CancelPlacement;
        }
        else
        {
            DisableGrid();
            // Unsubscribe when not in building mode
            PlayerInput.Instance.OnLeftJoystick -= HandleJoystickMovement;
            PlayerInput.Instance.OnAPressed -= PlaceBuilding;
            PlayerInput.Instance.OnBPressed -= CancelPlacement;
        }
    }

    public void StartPlacement(BuildingScriptableObj buildingObj)
    {
        selectedBuildingObj = buildingObj;
        currentPreview = Instantiate(buildingObj.buildingPrefab);
        SetPreviewMaterial(validPlacementMaterial);
        currentGridPosition = SnapToGrid(transform.position); // Start at player's position
        currentPreview.transform.position = currentGridPosition;

        // Notify PlayerInput to switch control type
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.BUILDING);
    }

    private void HandleJoystickMovement(Vector2 input)
    {
        if (currentPreview == null) return;

        // Slow down the movement by scaling the input
        float moveSpeed = 0.05f; // Adjust this value to control the speed of movement
        Vector3 move = new Vector3(input.x, 0, input.y) * gridSize * moveSpeed;

        // Update the current grid position by adding the movement vector
        currentGridPosition += move;

        // Clamp the position to the grid boundaries
        currentGridPosition.x = Mathf.Clamp(currentGridPosition.x, gridOrigin.x, gridOrigin.x + gridDimensions.x * gridSize - gridSize);
        currentGridPosition.z = Mathf.Clamp(currentGridPosition.z, gridOrigin.z, gridOrigin.z + gridDimensions.y * gridSize - gridSize);

        // Snap to grid and update the preview's position
        currentPreview.transform.position = SnapToGrid(currentGridPosition);

        // Update placement validity
        if (IsValidPlacement(currentPreview.transform.position))
        {
            SetPreviewMaterial(validPlacementMaterial);
        }
        else
        {
            SetPreviewMaterial(invalidPlacementMaterial);
        }
    }


    private void PlaceBuilding()
    {
        if (currentPreview == null) return;

        if (IsValidPlacement(currentPreview.transform.position))
        {
            GameObject constructionSite = Instantiate(selectedBuildingObj.constructionSite, currentPreview.transform.position, Quaternion.identity);

            constructionSite.GetComponent<ConstructionSite>().SetupConstruction(selectedBuildingObj);

            CancelPlacement(); // End placement after building is placed
        }
    }

    private void CancelPlacement()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        UtilityMenu.Instance.EnableBuildMenu();
    }

    

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            position.y,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }

    private bool IsValidPlacement(Vector3 position)
    {
        // Add logic to validate placement (e.g., collision checks, boundaries)
        return true;
    }

    private void SetPreviewMaterial(Material material)
    {
        if (currentPreview != null)
        {
            MeshRenderer renderer = currentPreview.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = material;
            }
        }
    }

    private void EnableGrid()
    {
        if (gridObjects == null)
        {
            gridObjects = new Dictionary<Vector3, GameObject>();

            for (float x = gridOrigin.x; x < gridOrigin.x + gridDimensions.x; x += gridSize)
            {
                for (float z = gridOrigin.z; z < gridOrigin.z + gridDimensions.y; z += gridSize)
                {
                    Vector3 gridPosition = new Vector3(x, gridOrigin.y, z);
                    GameObject gridSection = Instantiate(gridPrefab, gridPosition, Quaternion.identity, gridParent);
                    gridSection.SetActive(false); // Initially disable the grid cells
                    gridObjects.Add(gridPosition, gridSection);
                }
            }
        }

        foreach (var gridSection in gridObjects.Values)
        {
            gridSection.SetActive(true); // Enable all grid cells
        }
    }

    private void DisableGrid()
    {
        if (gridObjects == null) return;

        foreach (var gridSection in gridObjects.Values)
        {
            gridSection.SetActive(false); // Disable all grid cells
        }
    }
}
