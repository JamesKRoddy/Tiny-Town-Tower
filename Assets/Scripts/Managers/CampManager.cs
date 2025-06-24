using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Managers
{
    public class CampManager : MonoBehaviour
    {
        private static CampManager _instance;
        public static CampManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CampManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("CampManager instance not found in the scene!");
                    }
                }
                return _instance;
            }
        }

        [Header("Shared Placement Settings")]
        [SerializeField] private Vector2 sharedXBounds = new Vector2(-25f, 25f);
        [SerializeField] private Vector2 sharedZBounds = new Vector2(-25f, 25f);
        [SerializeField] private float sharedGridSize = 2f;
        [SerializeField] private bool showSharedGridBounds = true;

        // Shared grid system
        private Dictionary<Vector3, GridSlot> sharedGridSlots = new Dictionary<Vector3, GridSlot>();
        private bool gridObjectsInitialized = false;
        private Transform sharedGridParent;

        // References to other managers
        private ResearchManager researchManager;
        private CleanlinessManager cleanlinessManager;
        private WorkManager workManager;
        private BuildManager buildManager;
        private CookingManager cookingManager;
        private ResourceUpgradeManager resourceUpgradeManager;
        private ElectricityManager electricityManager;
        private FarmingManager farmingManager;

        // Public access to other managers
        public ResearchManager ResearchManager => researchManager;
        public CleanlinessManager CleanlinessManager => cleanlinessManager;
        public WorkManager WorkManager => workManager;
        public BuildManager BuildManager => buildManager;
        public CookingManager CookingManager => cookingManager;
        public ResourceUpgradeManager ResourceUpgradeManager => resourceUpgradeManager;
        public ElectricityManager ElectricityManager => electricityManager;
        public FarmingManager FarmingManager => farmingManager;

        // Public access to shared placement settings
        public Vector2 SharedXBounds => sharedXBounds;
        public Vector2 SharedZBounds => sharedZBounds;
        public float SharedGridSize => sharedGridSize;
        public bool ShowSharedGridBounds => showSharedGridBounds;

        // Public access to shared grid
        public Dictionary<Vector3, GridSlot> SharedGridSlots => sharedGridSlots;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
                InitializeManagers();
                InitializeSharedGrid();
            }
        }

        private void InitializeManagers()
        {
            // Create shared grid parent
            GameObject gridParentObj = new GameObject("SharedGridParent");
            gridParentObj.transform.SetParent(transform);
            sharedGridParent = gridParentObj.transform;

            // Find and cache references to other managers
            if (researchManager == null) researchManager = gameObject.GetComponentInChildren<ResearchManager>();
            if (cleanlinessManager == null) cleanlinessManager = gameObject.GetComponentInChildren<CleanlinessManager>();
            if (cookingManager == null) cookingManager = gameObject.GetComponentInChildren<CookingManager>();
            if (resourceUpgradeManager == null) resourceUpgradeManager = gameObject.GetComponentInChildren<ResourceUpgradeManager>();
            if (workManager == null) workManager = gameObject.GetComponentInChildren<WorkManager>();
            if (buildManager == null) buildManager = gameObject.GetComponentInChildren<BuildManager>();
            if (electricityManager == null) electricityManager = gameObject.GetComponentInChildren<ElectricityManager>();
            if (farmingManager == null) farmingManager = gameObject.GetComponentInChildren<FarmingManager>();
            // Log warnings for any missing managers
            if (researchManager == null) Debug.LogWarning("ResearchManager not found in scene!");
            if (cleanlinessManager == null) Debug.LogWarning("CleanlinessManager not found in scene!");
            if (cookingManager == null) Debug.LogWarning("CookingManager not found in scene!");
            if (resourceUpgradeManager == null) Debug.LogWarning("ResourceUpgradeManager not found in scene!");
            if (workManager == null) Debug.LogWarning("WorkManager not found in scene!");
            if (buildManager == null) Debug.LogWarning("BuildManager not found in scene!");
            if (electricityManager == null) Debug.LogWarning("ElectricityManager not found in scene!");
            if (farmingManager == null) Debug.LogWarning("FarmingManager not found in scene!");
            // Initialize managers
            if (researchManager != null) researchManager.Initialize();
            if (cookingManager != null) cookingManager.Initialize();
            if (resourceUpgradeManager != null) resourceUpgradeManager.Initialize();
            if (electricityManager != null) electricityManager.Initialize();
            if (cleanlinessManager != null) cleanlinessManager.Initialize();
            if (farmingManager != null) farmingManager.Initialize();
        }

        private void InitializeSharedGrid()
        {
            // Initialize the shared grid slots
            sharedGridSlots.Clear();
            
            for (float x = sharedXBounds.x; x < sharedXBounds.y; x += sharedGridSize)
            {
                for (float z = sharedZBounds.x; z < sharedZBounds.y; z += sharedGridSize)
                {
                    Vector3 gridPosition = new Vector3(x, 0, z);
                    sharedGridSlots[gridPosition] = new GridSlot { IsOccupied = false, GridObject = null };
                }
            }
            
            Debug.Log($"Initialized shared grid with {sharedGridSlots.Count} slots");
        }

        // Shared grid methods that placers can use
        public bool AreSharedGridSlotsAvailable(Vector3 position, Vector2Int size)
        {
            List<Vector3> requiredSlots = GetRequiredSharedGridSlots(position, size);

            foreach (var slot in requiredSlots)
            {
                if (sharedGridSlots.ContainsKey(slot) && sharedGridSlots[slot].IsOccupied)
                {
                    return false;
                }
            }
            return true;
        }

        public void MarkSharedGridSlotsOccupied(Vector3 position, Vector2Int size, GameObject placedObject)
        {
            List<Vector3> requiredSlots = GetRequiredSharedGridSlots(position, size);

            foreach (var slot in requiredSlots)
            {
                if (sharedGridSlots.ContainsKey(slot))
                {
                    if (sharedGridSlots[slot].IsOccupied)
                    {
                        Debug.LogWarning($"Shared grid slot at {slot} is already occupied by {sharedGridSlots[slot].OccupyingObject.name}!");
                        continue; // Skip marking if already occupied
                    }

                    sharedGridSlots[slot].IsOccupied = true;
                    sharedGridSlots[slot].OccupyingObject = placedObject;
                    
                    // Update the visual representation
                    UpdateGridSlotVisual(slot, true);
                }
                else
                {
                    Debug.LogError($"Shared grid slot at {slot} does not exist in the dictionary!");
                }
            }
        }

        public void MarkSharedGridSlotsUnoccupied(Vector3 position, Vector2Int size)
        {
            List<Vector3> requiredSlots = GetRequiredSharedGridSlots(position, size);

            foreach (var slot in requiredSlots)
            {
                if (sharedGridSlots.ContainsKey(slot))
                {
                    sharedGridSlots[slot].IsOccupied = false;
                    sharedGridSlots[slot].OccupyingObject = null;
                    
                    // Update the visual representation
                    UpdateGridSlotVisual(slot, false);
                }
            }
        }

        private void UpdateGridSlotVisual(Vector3 slotPosition, bool isOccupied)
        {
            if (!sharedGridSlots.ContainsKey(slotPosition)) return;
            
            GridSlot slot = sharedGridSlots[slotPosition];
            Vector3 displayPosition = new Vector3(slotPosition.x + sharedGridSize / 2, 0, slotPosition.z + sharedGridSize / 2);
            
            // This method will be called by placers to update visuals
            // The actual visual update will be handled by the current active placer
            if (isOccupied)
            {
                // Mark for visual update when grid is next shown
                slot.NeedsVisualUpdate = true;
            }
            else
            {
                // Mark for visual update when grid is next shown
                slot.NeedsVisualUpdate = true;
            }
        }

        private List<Vector3> GetRequiredSharedGridSlots(Vector3 position, Vector2Int size)
        {
            List<Vector3> requiredSlots = new List<Vector3>();

            Vector3 basePosition = SnapToSharedGrid(position);

            // Calculate the starting position (bottom-left corner)
            float startX = basePosition.x - ((size.x * sharedGridSize) / 2f);
            float startZ = basePosition.z - ((size.y * sharedGridSize) / 2f);

            for (int x = 0; x < size.x; x++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3 slotPosition = new Vector3(
                        startX + (x * sharedGridSize),
                        0,
                        startZ + (z * sharedGridSize)
                    );

                    requiredSlots.Add(slotPosition);
                }
            }

            return requiredSlots;
        }

        private Vector3 SnapToSharedGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / sharedGridSize) * sharedGridSize,
                0,
                Mathf.Round(position.z / sharedGridSize) * sharedGridSize
            );
        }

        public void ResetSharedGridObjects()
        {
            // Clear all grid objects from the shared grid
            foreach (var slot in sharedGridSlots.Values)
            {
                if (slot.GridObject != null)
                {
                    DestroyImmediate(slot.GridObject);
                    slot.GridObject = null;
                }
            }
            gridObjectsInitialized = false;
        }

        public void InitializeGridObjects(GameObject gridPrefab, GameObject takenGridPrefab)
        {
            if (gridObjectsInitialized) return;

            var sharedGridSlots = CampManager.Instance?.SharedGridSlots;
            if (sharedGridSlots == null) return;

            foreach (var kvp in sharedGridSlots)
            {
                Vector3 gridPosition = kvp.Key;
                GridSlot slot = kvp.Value;
                
                Vector3 displayPosition = new Vector3(gridPosition.x + sharedGridSize / 2, 0, gridPosition.z + sharedGridSize / 2);
                
                // Create or update grid object based on occupation status
                if (slot.IsOccupied && takenGridPrefab != null)
                {
                    // Destroy existing grid object if it's not the correct type
                    if (slot.GridObject != null && slot.GridObject.name != takenGridPrefab.name + "(Clone)")
                    {
                        DestroyImmediate(slot.GridObject);
                        slot.GridObject = null;
                    }
                    
                    // Create taken grid object if it doesn't exist
                    if (slot.GridObject == null)
                    {
                        slot.GridObject = Instantiate(takenGridPrefab, displayPosition, Quaternion.identity, sharedGridParent);
                    }
                }
                else
                {
                    // Destroy existing grid object if it's not the correct type
                    if (slot.GridObject != null && slot.GridObject.name != gridPrefab.name + "(Clone)")
                    {
                        DestroyImmediate(slot.GridObject);
                        slot.GridObject = null;
                    }
                    
                    // Create regular grid object if it doesn't exist
                    if (slot.GridObject == null)
                    {
                        slot.GridObject = Instantiate(gridPrefab, displayPosition, Quaternion.identity, sharedGridParent);
                    }
                }
                
                // Ensure the grid object is active
                if (slot.GridObject != null)
                {
                    slot.GridObject.SetActive(true);
                }
                
                // Reset visual update flag
                slot.NeedsVisualUpdate = false;
            }
            
            gridObjectsInitialized = true;
        }

        public void ShowGridObjects()
        {
            var sharedGridSlots = CampManager.Instance?.SharedGridSlots;
            if (sharedGridSlots == null) return;

            foreach (var slot in sharedGridSlots.Values)
            {
                if (slot.GridObject != null)
                {
                    slot.GridObject.SetActive(true);
                }
            }
        }

        public void HideGridObjects()
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

        private void OnDrawGizmos()
        {
            if (showSharedGridBounds)
            {
                Gizmos.color = Color.yellow; // Using yellow to distinguish shared grid
                Vector3 bottomLeft = new Vector3(sharedXBounds.x, 0, sharedZBounds.x);
                Vector3 bottomRight = new Vector3(sharedXBounds.y, 0, sharedZBounds.x);
                Vector3 topLeft = new Vector3(sharedXBounds.x, 0, sharedZBounds.y);
                Vector3 topRight = new Vector3(sharedXBounds.y, 0, sharedZBounds.y);

                Gizmos.DrawLine(bottomLeft, bottomRight);
                Gizmos.DrawLine(bottomRight, topRight);
                Gizmos.DrawLine(topRight, topLeft);
                Gizmos.DrawLine(topLeft, bottomLeft);
            }
        }
    } 
}