using UnityEngine;
using System.Collections.Generic;

public class BuildingPlacer : PlacementManager<BuildingScriptableObj>
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

    protected override void PlaceObject()
    {
        // Deduct the required resources from the player's inventory
        foreach (var requiredItem in selectedObject.buildingResourceCost)
        {
            PlayerInventory.Instance.RemoveItem(requiredItem.resource, requiredItem.count);
        }

        GameObject constructionSite = Instantiate(selectedObject.constructionSite, currentPreview.transform.position, Quaternion.identity);
        constructionSite.GetComponent<ConstructionSite>().SetupConstruction(selectedObject);

        MarkGridSlotsOccupied(currentPreview.transform.position, selectedObject.buildingSize, constructionSite);
        CancelPlacement();
    }

    protected override bool IsValidPlacement(Vector3 position)
    {
        return AreGridSlotsAvailable(position, selectedObject.buildingSize);
    }

    protected override GameObject GetPrefabFromObject(BuildingScriptableObj obj)
    {
        return obj.buildingPrefab;
    }

    protected override void NotifyControlTypeChange()
    {
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.BUILDING_PLACEMENT);
    }

    protected override void HandleControlTypeUpdate(PlayerControlType controlType)
    {
        if (controlType == PlayerControlType.BUILDING_PLACEMENT)
        {
            EnableGrid(BuildManager.Instance.GetXBounds(), BuildManager.Instance.GetZBounds());
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
        UtilityMenu.Instance.EnableBuildMenu();
    }
}
