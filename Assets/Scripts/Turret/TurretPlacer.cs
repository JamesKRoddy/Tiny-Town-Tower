using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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
        GameObject turret = Instantiate(selectedObject.turretPrefab, currentPreview.transform.position, Quaternion.identity);
        turret.GetComponent<BaseTurret>().SetupTurret();
        CancelPlacement();
    }

    protected override bool IsValidPlacement(Vector3 position)
    {
        return true; //TODO Add turret-specific validation logic
    }

    protected override GameObject GetPrefabFromObject(TurretScriptableObject obj)
    {
        return obj.turretPrefab;
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