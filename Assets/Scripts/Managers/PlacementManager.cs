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
    }

    public abstract class PlacementManager<T> : MonoBehaviour where T : ScriptableObject
    {
        public Material validPlacementMaterial;
        public Material invalidPlacementMaterial;

        [Header("Grid Settings")]
        public GameObject gridPrefab;
        public GameObject takenGridPrefab;
        public Transform gridParent;

        protected GameObject currentPreview;
        protected T selectedObject;
        protected Vector3 currentGridPosition;
        protected float gridSize => CampManager.Instance != null ? CampManager.Instance.SharedGridSize : 2f;

        protected abstract void PlaceObject();
        protected abstract bool IsValidPlacement(Vector3 position, out string errorMessage);
        public abstract Vector2 GetXBounds();
        public abstract Vector2 GetZBounds();

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
            PlayerController.Instance.playerCamera.UpdateTarget(currentPreview.transform);

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

        protected abstract void OnPlacementCancelled();

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

        protected void EnableGrid(Vector2 xBounds, Vector2 zBounds)
        {
            // Use shared grid slots from CampManager
            var sharedGridSlots = CampManager.Instance?.SharedGridSlots;
            if (sharedGridSlots == null) return;

            // Create visual grid objects for occupied slots
            foreach (var kvp in sharedGridSlots)
            {
                Vector3 gridPosition = kvp.Key;
                GridSlot slot = kvp.Value;
                
                Vector3 displayPosition = new Vector3(gridPosition.x + gridSize / 2, 0, gridPosition.z + gridSize / 2);
                
                if (slot.GridObject == null)
                {
                    GameObject gridSection = Instantiate(gridPrefab, displayPosition, Quaternion.identity, gridParent);
                    gridSection.SetActive(false);
                    slot.GridObject = gridSection;
                }
                
                // Update visual representation based on occupation
                if (slot.IsOccupied && takenGridPrefab != null)
                {
                    if (slot.GridObject != null)
                    {
                        Destroy(slot.GridObject);
                    }
                    slot.GridObject = Instantiate(takenGridPrefab, displayPosition, Quaternion.identity, gridParent);
                }
                
                slot.GridObject.SetActive(true);
            }
        }

        protected void DisableGrid()
        {
            var sharedGridSlots = CampManager.Instance?.SharedGridSlots;
            if (sharedGridSlots == null) return;

            foreach (var slot in sharedGridSlots.Values)
            {
                if (slot.GridObject != null)
                {
                    slot.GridObject.SetActive(false);
                }
            }
        }

        protected bool AreGridSlotsAvailable(Vector3 position, Vector2Int size)
        {
            return CampManager.Instance?.AreSharedGridSlotsAvailable(position, size) ?? false;
        }

        protected void MarkGridSlotsOccupied(Vector3 position, Vector2Int size, GameObject placedObject)
        {
            CampManager.Instance?.MarkSharedGridSlotsOccupied(position, size, placedObject);
        }

        protected void MarkGridSlotsUnoccupied(Vector3 position, Vector2Int size)
        {
            CampManager.Instance?.MarkSharedGridSlotsUnoccupied(position, size);
        }
    }
}