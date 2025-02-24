using UnityEngine;
using UnityEngine.AI;
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
        if (!IsValidPlacement(currentGridPosition, out string errorMessage))
        {
            PlayerUIManager.Instance.DisplayUIErrorMessage($"Cannot place turret - {errorMessage}");
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

    protected override bool IsValidPlacement(Vector3 position, out string errorMessage)
    {
        // First, check if the grid slots are available.
        if (!AreGridSlotsAvailable(position, selectedObject.size))
        {
            errorMessage = " no space!";
            return false;
        }

        // Temporarily mark the grid cells as blocked by the turret.
        MarkGridSlotsOccupiedTemporarily(position, selectedObject.size);

        // Get enemy spawn and base (destination) points.
        // These should be defined in your game. For example, via a GameManager.
        Vector3? spawnPoint = EnemySpawnManager.Instance.SpawnPointPosition();
        if (!spawnPoint.HasValue)
        {
            errorMessage = " no spawn points (this is an error)!";
            return false;
        }
        Vector3 basePoint = TurretManager.Instance.baseTarget.transform.position;

        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(spawnPoint.Value, basePoint, NavMesh.AllAreas, path);

        // Revert the temporary grid changes.
        UnmarkGridSlotsTemporarily(position, selectedObject.size);

        if(path.status == NavMeshPathStatus.PathInvalid || path.status == NavMeshPathStatus.PathPartial)
        {
            // Allow placement only if a complete path exists.
            errorMessage = " path blocked!";
            return false;
        }
        else
        {
            errorMessage = string.Empty;
            return true;
        }

    }

    // These temporary grid methods must be implemented according to your grid system.
    private void MarkGridSlotsOccupiedTemporarily(Vector3 position, Vector2 size)
    {
        // Mark the grid cells as occupied without permanently changing them.
        // This could be as simple as setting flags in a grid array.
    }

    private void UnmarkGridSlotsTemporarily(Vector3 position, Vector2 size)
    {
        // Revert the temporary blocking of grid cells.
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
