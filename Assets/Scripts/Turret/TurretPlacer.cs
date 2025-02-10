using UnityEngine;
using System.Collections.Generic;

public class TurretPlacer : PlacementManager<TurretScriptableObject>
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

    protected override void PlaceObject()
    {
        if (!IsValidPlacement(currentGridPosition))
        {
            PlayerUIManager.Instance.DisplayUIErrorMessage("Cannot place turret");
            return;
        }

        // Deduct the required resources from the player's inventory.
        foreach (var requiredItem in selectedObject._resourceCost)
        {
            PlayerInventory.Instance.RemoveItem(requiredItem.resource, requiredItem.count);
        }

        GameObject turret = Instantiate(selectedObject.prefab, currentPreview.transform.position, Quaternion.identity);
        turret.GetComponent<BaseTurret>().SetupTurret();

        MarkGridSlotsOccupied(currentPreview.transform.position, selectedObject.size, turret);
        CancelPlacement();
    }

    protected override bool IsValidPlacement(Vector3 position)
    {
        return AreGridSlotsAvailable(position, selectedObject.size);
    }

    protected override GameObject GetPrefabFromObject(TurretScriptableObject obj)
    {
        return obj.prefab;
    }

    protected override void NotifyControlTypeChange()
    {
        PlayerInput.Instance.UpdatePlayerControls(PlayerControlType.TURRET_PLACEMENT);
    }

    protected override void HandleControlTypeUpdate(PlayerControlType controlType)
    {
        if (controlType == PlayerControlType.TURRET_PLACEMENT)
        {
            EnableGrid(TurretManager.Instance.GetXBounds(), TurretManager.Instance.GetZBounds());
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
        TurretMenu.Instance.SetScreenActive(true, 0.1f);
    }
}
