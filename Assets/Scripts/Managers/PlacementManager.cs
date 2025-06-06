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
        protected float gridSize = 2f;

        private Dictionary<Vector3, GridSlot> gridSlots = new Dictionary<Vector3, GridSlot>();

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
            if (gridSlots.Count == 0)
            {
                for (float x = xBounds.x; x < xBounds.y; x += gridSize)
                {
                    for (float z = zBounds.x; z < zBounds.y; z += gridSize)
                    {
                        // Create grid position without offset
                        Vector3 gridPosition = new Vector3(x, 0, z);
                        Vector3 displayPosition = new Vector3(x + gridSize / 2, 0, z + gridSize / 2);

                        GameObject gridSection = Instantiate(gridPrefab, displayPosition, Quaternion.identity, gridParent);
                        gridSection.SetActive(false);

                        gridSlots[gridPosition] = new GridSlot { IsOccupied = false, GridObject = gridSection };
                    }
                }
            }

            foreach (var slot in gridSlots.Values)
            {
                if (slot.IsOccupied && takenGridPrefab != null)
                {
                    if (slot.GridObject != null)
                    {
                        Destroy(slot.GridObject);
                    }
                    slot.GridObject = Instantiate(takenGridPrefab, slot.GridObject.transform.position, Quaternion.identity, gridParent);
                }
                slot.GridObject.SetActive(true);
            }
        }

        protected void DisableGrid()
        {
            foreach (var slot in gridSlots.Values)
            {
                slot.GridObject.SetActive(false);
            }
        }

        protected bool AreGridSlotsAvailable(Vector3 position, Vector2Int size)
        {
            List<Vector3> requiredSlots = GetRequiredGridSlots(position, size);

            foreach (var slot in requiredSlots)
            {
                if (gridSlots.ContainsKey(slot) && gridSlots[slot].IsOccupied)
                {
                    return false;
                }
            }
            return true;
        }

        protected void MarkGridSlotsOccupied(Vector3 position, Vector2Int size, GameObject placedObject)
        {
            List<Vector3> requiredSlots = GetRequiredGridSlots(position, size);

            foreach (var slot in requiredSlots)
            {
                if (gridSlots.ContainsKey(slot))
                {
                    if (gridSlots[slot].IsOccupied)
                    {
                        Debug.LogWarning($"Grid slot at {slot} is already occupied by {gridSlots[slot].OccupyingObject.name}!");
                        continue; // Skip marking if already occupied
                    }

                    gridSlots[slot].IsOccupied = true;
                    gridSlots[slot].OccupyingObject = placedObject;
                }
                else
                {
                    Debug.LogError($"Grid slot at {slot} does not exist in the dictionary!");
                }
            }
        }

        private List<Vector3> GetRequiredGridSlots(Vector3 position, Vector2Int size)
        {
            List<Vector3> requiredSlots = new List<Vector3>();

            Vector3 basePosition = SnapToGrid(position);

            // Calculate the starting position (bottom-left corner)
            float startX = basePosition.x - ((size.x * gridSize) / 2f);
            float startZ = basePosition.z - ((size.y * gridSize) / 2f);

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3 slotPosition = new Vector3(
                        startX + (x * gridSize),
                        0,
                        startZ + (z * gridSize)
                    );

                    requiredSlots.Add(slotPosition);
                }
            }

            return requiredSlots;
        }
    }
}