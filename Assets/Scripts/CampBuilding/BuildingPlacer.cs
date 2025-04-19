using UnityEngine;
using System.Collections.Generic;
using Managers;
public class BuildingPlacer : PlacementManager<BuildingScriptableObj>
{
    [Header("Building Grid")]
    [SerializeField] private Vector2 xBounds = new Vector2(-25f, 25f);
    [SerializeField] private Vector2 zBounds = new Vector2(-25f, 25f);
    [SerializeField] private bool showGridBounds;

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

    public override Vector2 GetXBounds() => xBounds;
    public override Vector2 GetZBounds() => zBounds;

    protected override void PlaceObject()
    {
        if (!IsValidPlacement(currentGridPosition, out string errorMessage))
        {
            PlayerUIManager.Instance.DisplayUIErrorMessage("Cannot place turret");
            return;        
        }

        // Deduct the required resources from the player's inventory
        foreach (var requiredItem in selectedObject._resourceCost)
        {
            PlayerInventory.Instance.RemoveItem(requiredItem.resource, requiredItem.count);
        }

        GameObject constructionSite = Instantiate(selectedObject.constructionSite, currentPreview.transform.position, Quaternion.identity);

        if (constructionSite.TryGetComponent(out ConstructionSite constructionSiteScript)){
            constructionSiteScript.SetupConstruction(selectedObject);
        } else {
            constructionSite.AddComponent<ConstructionSite>();
            constructionSite.GetComponent<ConstructionSite>().SetupConstruction(selectedObject);
        }
        
        MarkGridSlotsOccupied(currentPreview.transform.position, selectedObject.size, constructionSite);
        CancelPlacement();
    }

    protected override bool IsValidPlacement(Vector3 position, out string errorMessage)
    {
        errorMessage = null;
        return AreGridSlotsAvailable(position, selectedObject.size);
    }

    protected override GameObject GetPrefabFromObject(BuildingScriptableObj obj)
    {
        return obj.prefab;
    }

    protected override void NotifyControlTypeChange()
    {
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.BUILDING_PLACEMENT);
    }

    protected override void HandleControlTypeUpdate(PlayerControlType controlType)
    {
        if (controlType == PlayerControlType.BUILDING_PLACEMENT)
        {
            EnableGrid(xBounds, zBounds);
            PlayerInput.Instance.OnLeftJoystick += HandleJoystickMovement;
            PlayerInput.Instance.OnAPressed += PlaceObject;
            PlayerInput.Instance.OnBPressed += CancelPlacement;
        }
        else
        {
            DisableGrid();
            PlayerInput.Instance.OnLeftJoystick -= HandleJoystickMovement;
            PlayerInput.Instance.OnAPressed -= PlaceObject;
            PlayerInput.Instance.OnBPressed -= CancelPlacement;
        }
    }

    protected override void OnPlacementCancelled()
    {
        PlayerUIManager.Instance.utilityMenu.EnableBuildMenu();
    }

    private void OnDrawGizmos()
    {
        if (showGridBounds)
        {
            Gizmos.color = Color.blue; // Using blue to distinguish from turret grid
            Vector3 bottomLeft = new Vector3(xBounds.x, 0, zBounds.x);
            Vector3 bottomRight = new Vector3(xBounds.y, 0, zBounds.x);
            Vector3 topLeft = new Vector3(xBounds.x, 0, zBounds.y);
            Vector3 topRight = new Vector3(xBounds.y, 0, zBounds.y);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}
