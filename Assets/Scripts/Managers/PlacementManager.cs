using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Managers
{
    public class GridSlot
    {
        public bool IsOccupied { get; set; }
        public GameObject OccupyingObject { get; set; }
        public GameObject GridObject { get; set; }
        public bool NeedsVisualUpdate { get; set; } = false;
    }

    public class PlacementManager : MonoBehaviour
    {
        public Material validPlacementMaterial;
        public Material invalidPlacementMaterial;

        [Header("Grid Settings")]
        public GameObject gridPrefab;
        public GameObject takenGridPrefab;

        public enum PlacementType
        {
            Building,
            Turret
        }

        private static PlacementManager _instance;
        public static PlacementManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlacementManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("PlacementManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        protected GameObject currentPreview;
        protected ScriptableObject selectedObject;
        protected Vector3 currentGridPosition;
        protected PlacementType currentPlacementType;
        protected float gridSize => CampManager.Instance != null ? CampManager.Instance.SharedGridSize : 2f;

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

        public void StartBuildingPlacement(BuildingScriptableObj buildingToPlace)
        {
            currentPlacementType = PlacementType.Building;
            selectedObject = buildingToPlace;
            StartPlacement();
        }

        public void StartTurretPlacement(TurretScriptableObject turretToPlace)
        {
            currentPlacementType = PlacementType.Turret;
            selectedObject = turretToPlace;
            StartPlacement();
        }

        private void StartPlacement()
        {
            currentPreview = Instantiate(GetPrefabFromObject(selectedObject));
            SetPreviewMaterial(validPlacementMaterial);
            currentGridPosition = SnapToGrid(transform.position);
            currentPreview.transform.position = currentGridPosition;
            PlayerController.Instance.playerCamera.UpdateTarget(currentPreview.transform);

            NotifyControlTypeChange();
        }

        private GameObject GetPrefabFromObject(ScriptableObject obj)
        {
            if (obj is BuildingScriptableObj buildingObj)
                return buildingObj.prefab;
            else if (obj is TurretScriptableObject turretObj)
                return turretObj.prefab;
            
            Debug.LogError($"Unknown scriptable object type: {obj.GetType()}");
            return null;
        }

        private void NotifyControlTypeChange()
        {
            PlayerControlType controlType = currentPlacementType == PlacementType.Building ? 
                PlayerControlType.BUILDING_PLACEMENT : PlayerControlType.TURRET_PLACEMENT;
            PlayerInput.Instance.UpdatePlayerControls(controlType);
        }

        protected void HandleJoystickMovement(Vector2 input)
        {
            if (currentPreview == null) return;

            float moveSpeed = 0.05f;
            Vector3 move = new Vector3(input.x, 0, input.y) * gridSize * moveSpeed;

            currentGridPosition += move;

            Vector2 xBounds = GetXBounds();
            Vector2 zBounds = GetZBounds();

            currentGridPosition.x = Mathf.Clamp(currentGridPosition.x, xBounds.x, xBounds.y - gridSize);
            currentGridPosition.z = Mathf.Clamp(currentGridPosition.z, zBounds.x, zBounds.y - gridSize);

            currentPreview.transform.position = SnapToGrid(currentGridPosition);

            if (IsValidPlacement(currentPreview.transform.position, out string errorMessage))
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

        private void PlaceObject()
        {
            if (!IsValidPlacement(currentGridPosition, out string errorMessage))
            {
                string errorMsg = currentPlacementType == PlacementType.Building ? 
                    "Cannot place building" : $"Cannot place turret - {errorMessage}";
                PlayerUIManager.Instance.DisplayUIErrorMessage(errorMsg);
                return;        
            }

            // Deduct the required resources from the player's inventory
            if (selectedObject is PlaceableObjectParent placeableObj)
            {
                foreach (var requiredItem in placeableObj._resourceCost)
                {
                    PlayerInventory.Instance.RemoveItem(requiredItem.resourceScriptableObj, requiredItem.count);
                }
            }

            // Create construction site
            GameObject constructionSitePrefab = CampManager.Instance.BuildManager.GetConstructionSitePrefab(GetObjectSize());
            GameObject constructionSite = Instantiate(constructionSitePrefab, currentPreview.transform.position, Quaternion.identity);

            if (currentPlacementType == PlacementType.Building)
            {
                SetupBuildingConstruction(constructionSite);
            }
            else if (currentPlacementType == PlacementType.Turret)
            {
                SetupTurretConstruction(constructionSite);
            }
            
            MarkGridSlotsOccupied(currentPreview.transform.position, GetObjectSize(), constructionSite);
            CancelPlacement();
        }

        private void SetupBuildingConstruction(GameObject constructionSite)
        {
            if (constructionSite.TryGetComponent(out ConstructionTask constructionSiteScript))
            {
                constructionSiteScript.SetupConstruction(selectedObject as BuildingScriptableObj);
            }
            else
            {
                constructionSite.AddComponent<ConstructionTask>();
                constructionSite.GetComponent<ConstructionTask>().SetupConstruction(selectedObject as BuildingScriptableObj);
            }
        }

        private void SetupTurretConstruction(GameObject constructionSite)
        {
            if (constructionSite.TryGetComponent(out TurretConstructionTask constructionSiteScript))
            {
                constructionSiteScript.SetupConstruction(selectedObject as TurretScriptableObject);
            }
            else
            {
                constructionSite.AddComponent<TurretConstructionTask>();
                constructionSite.GetComponent<TurretConstructionTask>().SetupConstruction(selectedObject as TurretScriptableObject);
            }
        }

        private Vector2Int GetObjectSize()
        {
            if (selectedObject is PlaceableObjectParent placeableObj)
                return placeableObj.size;
            
            return new Vector2Int(1, 1); // Default size
        }

        private bool IsValidPlacement(Vector3 position, out string errorMessage)
        {
            errorMessage = null;
            return AreGridSlotsAvailable(position, GetObjectSize());
        }

        private void OnPlacementCancelled()
        {
            if (currentPlacementType == PlacementType.Building)
            {
                PlayerUIManager.Instance.utilityMenu.EnableBuildMenu();
            }
            else if (currentPlacementType == PlacementType.Turret)
            {
                PlayerUIManager.Instance.turretMenu.SetScreenActive(true, 0.1f);
            }
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                0,
                Mathf.Round(position.z / gridSize) * gridSize
            );
        }

        private void SetPreviewMaterial(Material material)
        {
            if (currentPreview != null)
            {
                MeshRenderer[] renderer = currentPreview.GetComponentsInChildren<MeshRenderer>();
                if (renderer != null && renderer.Length > 0)
                {
                    foreach (var meshRenderer in renderer)
                    {
                        meshRenderer.material = material;
                    }
                }
            }
        }

        private void HandleControlTypeUpdate(PlayerControlType controlType)
        {
            bool isBuildingPlacement = controlType == PlayerControlType.BUILDING_PLACEMENT;
            bool isTurretPlacement = controlType == PlayerControlType.TURRET_PLACEMENT;

            if (isBuildingPlacement || isTurretPlacement)
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

        private void EnableGrid(Vector2 xBounds, Vector2 zBounds)
            {
            // Use the efficient grid object management from CampManager
            if (CampManager.Instance != null)
                    {
                CampManager.Instance.InitializeGridObjects(gridPrefab, takenGridPrefab);
                CampManager.Instance.ShowGridObjects();
            }
        }

        private void DisableGrid()
        {
            // Just hide grid objects, don't destroy them
            CampManager.Instance?.HideGridObjects();
        }

        public Vector2 GetXBounds() => CampManager.Instance != null ? CampManager.Instance.SharedXBounds : new Vector2(-25f, 25f);
        public Vector2 GetZBounds() => CampManager.Instance != null ? CampManager.Instance.SharedZBounds : new Vector2(-25f, 25f);

        private bool AreGridSlotsAvailable(Vector3 position, Vector2Int size)
        {
            return CampManager.Instance?.AreSharedGridSlotsAvailable(position, size) ?? false;
        }

        private void MarkGridSlotsOccupied(Vector3 position, Vector2Int size, GameObject placedObject)
        {
            CampManager.Instance?.MarkSharedGridSlotsOccupied(position, size, placedObject);
        }

        private void MarkGridSlotsUnoccupied(Vector3 position, Vector2Int size)
        {
            CampManager.Instance?.MarkSharedGridSlotsUnoccupied(position, size);
        }
    }
}