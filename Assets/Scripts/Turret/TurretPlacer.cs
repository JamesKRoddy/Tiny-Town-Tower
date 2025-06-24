using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Managers;

public class TurretPlacer : PlacementManager<TurretScriptableObject>
{
    [Header("Turret Grid")]
    [SerializeField] private bool showGridBounds;

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

    public override Vector2 GetXBounds() => CampManager.Instance != null ? CampManager.Instance.SharedXBounds : new Vector2(-25f, 25f);
    public override Vector2 GetZBounds() => CampManager.Instance != null ? CampManager.Instance.SharedZBounds : new Vector2(-25f, 25f);

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
            PlayerInventory.Instance.RemoveItem(requiredItem.resourceScriptableObj, requiredItem.count);
        }

        // Create a construction site for the turret
        GameObject constructionSitePrefab = CampManager.Instance.BuildManager.GetConstructionSitePrefab(selectedObject.size);
        GameObject constructionSite = Instantiate(constructionSitePrefab, currentPreview.transform.position, Quaternion.identity);

        if (constructionSite.TryGetComponent(out TurretConstructionTask constructionSiteScript)){
            constructionSiteScript.SetupConstruction(selectedObject);
        } else {
            constructionSite.AddComponent<TurretConstructionTask>();
            constructionSite.GetComponent<TurretConstructionTask>().SetupConstruction(selectedObject);
        }
        
        MarkGridSlotsOccupied(currentPreview.transform.position, selectedObject.size, constructionSite);
        CancelPlacement();
    }

    protected override bool IsValidPlacement(Vector3 position, out string errorMessage)
    {
        // Check if the grid slots are available.
        if (!AreGridSlotsAvailable(position, selectedObject.size))
        {
            errorMessage = " no space!";
            return false;
        }

        errorMessage = string.Empty;
        return true;
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
            EnableGrid(GetXBounds(), GetZBounds());
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
        PlayerUIManager.Instance.turretMenu.SetScreenActive(true, 0.1f);
    }

    private void OnDrawGizmos()
    {
        if (showGridBounds)
        {
            Gizmos.color = Color.green;
            Vector2 xBounds = GetXBounds();
            Vector2 zBounds = GetZBounds();
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
