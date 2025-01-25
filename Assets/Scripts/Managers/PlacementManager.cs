using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class PlacementManager<T> : MonoBehaviour where T : ScriptableObject
{
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;

    [Header("Grid Settings")]
    public GameObject gridPrefab;
    public Vector2 gridDimensions = new Vector2(20, 20);
    public Vector3 gridOrigin = Vector3.zero;
    public Transform gridParent;

    protected GameObject currentPreview;
    protected T selectedObject;
    protected Vector3 currentGridPosition;
    protected float gridSize = 1f;

    private Dictionary<Vector3, GameObject> gridObjects;

    protected abstract void PlaceObject();
    protected abstract bool IsValidPlacement(Vector3 position);

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

    protected abstract void HandleControlTypeUpdate(PlayerControlType controlType);

    public void StartPlacement(T objectToPlace)
    {
        selectedObject = objectToPlace;
        currentPreview = Instantiate(GetPrefabFromObject(selectedObject));
        SetPreviewMaterial(validPlacementMaterial);
        currentGridPosition = SnapToGrid(transform.position);
        currentPreview.transform.position = currentGridPosition;

        NotifyControlTypeChange();
    }

    protected abstract GameObject GetPrefabFromObject(T obj);
    protected abstract void NotifyControlTypeChange();

    protected void HandleJoystickMovement(Vector2 input)
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

    protected void CancelPlacement()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }

        OnPlacementCancelled();
    }

    protected abstract void OnPlacementCancelled();

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            position.y,
            Mathf.Round(position.z / gridSize) * gridSize
        );
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

    protected void EnableGrid()
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

    protected void DisableGrid()
    {
        if (gridObjects == null) return;

        foreach (var gridSection in gridObjects.Values)
        {
            gridSection.SetActive(false);
        }
    }
}