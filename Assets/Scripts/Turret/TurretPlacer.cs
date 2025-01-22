using UnityEngine;
using System.Collections.Generic;

public class TurretPlacer : MonoBehaviour
{
    private static TurretPlacer _instance;

    public static TurretPlacer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<TurretPlacer>();
                if (_instance == null)
                {
                    Debug.LogError("TurretPlacer instance not found in the scene!");
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
    private TurretScriptableObject selectedTurretObj;
    private Vector3 currentGridPosition;
    private float gridSize = 1f;

    [Header("Grid Settings")]
    public GameObject gridPrefab;
    public Vector2 gridDimensions = new Vector2(20, 20);
    public Vector3 gridOrigin = Vector3.zero;
    public Transform gridParent;
    private Dictionary<Vector3, GameObject> gridObjects;

    private void OnEnable()
    {
        if (PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls += HandleControlTypeUpdate;
    }

    private void OnDisable()
    {
        if (PlayerInput.Instance != null)
            PlayerInput.Instance.OnUpdatePlayerControls -= HandleControlTypeUpdate;
    }

    private void HandleControlTypeUpdate(PlayerControlType controlType)
    {
        if (controlType == PlayerControlType.TURRET_PLACEMENT)
        {
            EnableGrid();
            PlayerInput.Instance.OnLeftJoystick += HandleJoystickMovement;
            PlayerInput.Instance.OnAPressed += PlaceTurret;
            PlayerInput.Instance.OnBPressed += CancelPlacement;
        }
        else
        {
            DisableGrid();
            PlayerInput.Instance.OnLeftJoystick -= HandleJoystickMovement;
            PlayerInput.Instance.OnAPressed -= PlaceTurret;
            PlayerInput.Instance.OnBPressed -= CancelPlacement;
        }
    }

    public void StartPlacement(TurretScriptableObject turretObj)
    {
        selectedTurretObj = turretObj;
        currentPreview = Instantiate(turretObj.turretPrefab);
        SetPreviewMaterial(validPlacementMaterial);
        currentGridPosition = SnapToGrid(transform.position);
        currentPreview.transform.position = currentGridPosition;

        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.TURRET_PLACEMENT);
    }

    private void HandleJoystickMovement(Vector2 input)
    {
        if (currentPreview == null) return;

        float moveSpeed = 0.05f;
        Vector3 move = new Vector3(input.x, 0, input.y) * gridSize * moveSpeed;

        currentGridPosition += move;
        currentGridPosition.x = Mathf.Clamp(currentGridPosition.x, gridOrigin.x, gridOrigin.x + gridDimensions.x * gridSize - gridSize);
        currentGridPosition.z = Mathf.Clamp(currentGridPosition.z, gridOrigin.z, gridOrigin.z + gridDimensions.y * gridSize - gridSize);

        currentPreview.transform.position = SnapToGrid(currentGridPosition);

        if (IsValidPlacement(currentPreview.transform.position))
        {
            SetPreviewMaterial(validPlacementMaterial);
        }
        else
        {
            SetPreviewMaterial(invalidPlacementMaterial);
        }
    }

    private void PlaceTurret()
    {
        if (currentPreview == null) return;

        if (IsValidPlacement(currentPreview.transform.position))
        {
            GameObject turret = Instantiate(selectedTurretObj.turretPrefab, currentPreview.transform.position, Quaternion.identity);
            turret.GetComponent<BaseTurret>().SetupTurret();

            CancelPlacement();
        }
    }

    private void CancelPlacement()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        TurretMenu.Instance.SetScreenActive(true);
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
        // Add turret-specific placement validation logic here
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
                    gridSection.SetActive(false);
                    gridObjects.Add(gridPosition, gridSection);
                }
            }
        }

        foreach (var gridSection in gridObjects.Values)
        {
            gridSection.SetActive(true);
        }
    }

    private void DisableGrid()
    {
        if (gridObjects == null) return;

        foreach (var gridSection in gridObjects.Values)
        {
            gridSection.SetActive(false);
        }
    }
}
