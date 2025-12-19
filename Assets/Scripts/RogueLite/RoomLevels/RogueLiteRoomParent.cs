using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using Managers;
using Enemies;

public class RogueLiteRoomParent : MonoBehaviour
{
    [Header("Central Piece")]
    public Transform centerPiece;

    [Header("Room Spawn Points")]
    public Transform roomSpawnPointsParent;
    private Transform[] roomTransforms;
    private List<Transform> dynamicSpawnPoints = new List<Transform>(); // Includes original + extender spawn points
    private int extendersPlaced = 0; // Track number of extenders placed in this building
    
    [Header("Room Collision Settings")]
    [Tooltip("Maximum acceptable overlap in cubic units (lower = stricter)")]
    [SerializeField] private float maxAcceptableOverlapVolume = 100f; // Allow up to 100 cubic units overlap
    [SerializeField] private float minRoomDistance = 5f; // Minimum distance between room centers
    [SerializeField] private float spawnPointExclusionRadius = 3f; // Overlap within this radius of spawn points is ignored (for room connections)
    [Tooltip("If true, shows all placed rooms' bounds in Scene view")]
    [SerializeField] private bool showPlacedRoomBounds = true;
    
    // Public property to enable/disable detailed collision logging (controlled from RoomDebugMenu)
    public bool ShowCollisionDebug { get; set; } = false;
    
    [Header("Room Swapping Settings")]
    [SerializeField] private int maxSwapRetries = 5; // Maximum attempts to resolve conflicts through swapping
    [SerializeField] private bool showSwapDebug = true;

    [Header("Gizmo Settings")]
    [SerializeField] private bool showDirectionArrows = true;
    [SerializeField] private float arrowLength = 5f;
    [SerializeField] private float arrowHeadLength = 1.5f;
    [SerializeField] private float arrowHeadAngle = 20f;
    [SerializeField] private Color arrowColor = Color.cyan;

    [Header("Room Type")]
    [SerializeField, ReadOnly] private RogueLikeRoomType roomType = RogueLikeRoomType.HOSTILE;

    private Transform playerSpawnPoint;
    private List<GameObject> roomPrefabs;
    private NavMeshSurface navMeshSurface;
    private Dictionary<Vector3, GameObject> spawnedRooms = new Dictionary<Vector3, GameObject>();
    private Dictionary<int, PlacedRoomData> placedRoomsBySpawnIndex = new Dictionary<int, PlacedRoomData>();
    
    // Property to get the room type
    public RogueLikeRoomType RoomType => roomType;
    
    [System.Serializable]
    private class PlacedRoomData
    {
        public GameObject roomObject;
        public GameObject originalPrefab;
        public int spawnIndex;
        public string debugName;
        public float overlapPercentage;
        public bool exceededTolerance;
        
        public PlacedRoomData(GameObject room, GameObject prefab, int spawnIdx, string name)
        {
            roomObject = room;
            originalPrefab = prefab;
            spawnIndex = spawnIdx;
            debugName = name;
            overlapPercentage = 0f;
            exceededTolerance = false;
        }
    }

    private RogueLiteManager rogueLiteManager;

    private void Awake()
    {
        rogueLiteManager = FindFirstObjectByType<RogueLiteManager>();
        if (rogueLiteManager == null)
        {
            Debug.LogError("RogueLiteManager not found in scene!");
        }

        // Get all child transforms from the parent
        if (roomSpawnPointsParent != null)
        {
            roomTransforms = new Transform[roomSpawnPointsParent.childCount];
            for (int i = 0; i < roomSpawnPointsParent.childCount; i++)
            {
                roomTransforms[i] = roomSpawnPointsParent.GetChild(i);
            }
        }
        else
        {
            Debug.LogError("Room Spawn Points Parent is not assigned!");
        }
    }

    public virtual void GenerateRandomRooms(RogueLikeBuildingDataScriptableObj buildingScriptableObj)
    {
        if (buildingScriptableObj == null)
        {
            Debug.LogError("Building Scriptable Object is null!");
            return;
        }

        // Clear existing rooms
        ClearExistingRooms();

        // Clear props on the center piece
        ClearPropsOnCenterPiece();

        int currentDifficulty = GameManager.Instance.DifficultyManager.GetCurrentWaveDifficulty();

        // Use hierarchical room placement instead of random placement
        HierarchicalRoomPlacement(buildingScriptableObj, currentDifficulty);

        RandomizePropsInSection(centerPiece);

        SetupDoors();
        SetupChests();
        
        // Check if this is a friendly room and trigger door unlock if needed
        CheckAndHandleFriendlyRoomDoors();

        // Get or add NavMeshSurface component on THIS parent (not on child rooms)
        if (navMeshSurface == null)
        {
            navMeshSurface = GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                // If not found on parent, try to find one in scene
            navMeshSurface = FindAnyObjectByType<NavMeshSurface>();
                if (navMeshSurface != null)
                {
                    Debug.Log($"[RogueLiteRoomParent] Using scene NavMeshSurface from {navMeshSurface.gameObject.name}");
                }
                else
                {
                    Debug.LogError("[RogueLiteRoomParent] No NavMeshSurface found in scene! Please add one or attach it to the RogueLiteRoomParent GameObject.");
                }
            }
        }

        // Configure NavMeshSurface for dynamic room spawning
        if (navMeshSurface != null)
        {
            ConfigureNavMeshSurface();
        }

        StartCoroutine(DelayedBakeNavMesh());
    }

    private void ClearExistingRooms()
    {
        foreach (var room in spawnedRooms.Values)
        {
            if (room != null)
            {
                Destroy(room);
            }
        }
        spawnedRooms.Clear();
        placedRoomsBySpawnIndex.Clear();
        dynamicSpawnPoints.Clear();
        extendersPlaced = 0;

        foreach (Transform roomTransform in roomTransforms)
        {
            foreach (Transform child in roomTransform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void ClearPropsOnCenterPiece()
    {
        PropRandomizer propRandomizer = centerPiece.GetComponentInChildren<PropRandomizer>();
        if (propRandomizer != null)
        {
            foreach (Transform child in propRandomizer.transform)
            {
                if (child != propRandomizer.transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    // Removed old InstantiateRoom and FindValidRoomPosition methods
    // These have been replaced by the new HierarchicalRoomPlacement system

    private bool WouldRoomOverlapAtPosition(RogueLiteRoom roomToTest, Vector3 position, Quaternion rotation)
    {
        if (roomToTest == null)
        {
            Debug.LogError("[WouldRoomOverlapAtPosition] roomToTest is null!");
            return true; // Fail safe - consider it as overlapping
        }

        // Use the new CalculateTestBounds method that accounts for rotation
        Bounds testBounds = roomToTest.CalculateTestBounds(position, rotation);
        
        if (ShowCollisionDebug)
        {
            Debug.Log($"[RoomCollision] Testing room at position {position}, bounds: center={testBounds.center}, size={testBounds.size}");
        }
        
        // Check against all placed rooms (using placedRoomsBySpawnIndex for more reliable tracking)
        foreach (var kvp in placedRoomsBySpawnIndex)
        {
            var roomData = kvp.Value;
            if (roomData == null || roomData.roomObject == null) continue;
            
            RogueLiteRoom existingRoomComponent = roomData.roomObject.GetComponent<RogueLiteRoom>();
            if (existingRoomComponent == null) continue;
            
            Bounds existingBounds = existingRoomComponent.GetWorldBounds();
            
            if (ShowCollisionDebug)
            {
                Debug.Log($"[RoomCollision] Checking against {roomData.debugName} at spawn {kvp.Key}, bounds: center={existingBounds.center}, size={existingBounds.size}");
            }
            
            // Dynamic minimum distance based on room sizes
            float combinedRadius = (testBounds.extents.magnitude + existingBounds.extents.magnitude) * 0.5f;
            float dynamicMinDistance = Mathf.Max(minRoomDistance, combinedRadius * 0.3f);
            
            // Check if rooms are too close (minimum distance check)
            float centerDistance = Vector3.Distance(testBounds.center, existingBounds.center);
            if (centerDistance < dynamicMinDistance)
            {
                if (ShowCollisionDebug)
                    Debug.Log($"[RoomCollision] Rooms too close: {centerDistance:F2} < {dynamicMinDistance:F2} (dynamic min based on room sizes)");
                return true;
            }
            
            // Check for overlap using intersection volume
            if (testBounds.Intersects(existingBounds))
            {
                // Calculate raw intersection volume
                Vector3 intersectionMin = Vector3.Max(testBounds.min, existingBounds.min);
                Vector3 intersectionMax = Vector3.Min(testBounds.max, existingBounds.max);
                
                if (intersectionMin.x < intersectionMax.x && 
                    intersectionMin.y < intersectionMax.y && 
                    intersectionMin.z < intersectionMax.z)
                {
                    Vector3 intersectionSize = intersectionMax - intersectionMin;
                    float intersectionVolume = intersectionSize.x * intersectionSize.y * intersectionSize.z;
                    
                    if (ShowCollisionDebug)
                    {
                        Debug.Log($"[RoomCollision] Overlap detected: {intersectionVolume:F1} cubic units (tolerance: {maxAcceptableOverlapVolume})");
                    }
                    
                    if (intersectionVolume > maxAcceptableOverlapVolume)
                    {
                        if (ShowCollisionDebug)
                            Debug.Log($"[RoomCollision] EXCESSIVE overlap: {intersectionVolume:F1} > {maxAcceptableOverlapVolume} cubic units");
                        return true; // Excessive overlap detected
                    }
                }
            }
        }
        
        return false; // No problematic overlap
    }
    
    private float CalculateOverlapPercentage(Bounds bounds1, Bounds bounds2)
    {
        // Calculate the intersection bounds
        Vector3 intersectionMin = Vector3.Max(bounds1.min, bounds2.min);
        Vector3 intersectionMax = Vector3.Min(bounds1.max, bounds2.max);
        
        // If no intersection, return 0
        if (intersectionMin.x >= intersectionMax.x || 
            intersectionMin.y >= intersectionMax.y || 
            intersectionMin.z >= intersectionMax.z)
        {
            return 0f;
        }
        
        // Calculate intersection volume
        Vector3 intersectionSize = intersectionMax - intersectionMin;
        float intersectionVolume = intersectionSize.x * intersectionSize.y * intersectionSize.z;
        
        // Calculate volumes of both bounds
        float volume1 = bounds1.size.x * bounds1.size.y * bounds1.size.z;
        float volume2 = bounds2.size.x * bounds2.size.y * bounds2.size.z;
        
        // Calculate overlap percentage relative to the smaller room
        float smallerVolume = Mathf.Min(volume1, volume2);
        float overlapPercentage = (intersectionVolume / smallerVolume) * 100f;
        
        return overlapPercentage;
    }

    // Track spawn counts for required room groups during building generation
    private Dictionary<int, int> requiredGroupSpawnCounts = new Dictionary<int, int>();
    
    /// <summary>
    /// Intelligently place rooms at all spawn points
    /// Prioritizes collision-free placement over filling every spawn point
    /// Supports room extenders that add additional spawn points dynamically
    /// Now also handles required room groups that must spawn first
    /// </summary>
    private void HierarchicalRoomPlacement(RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty)
    {
        if (roomTransforms == null || roomTransforms.Length == 0)
        {
            Debug.LogError("[RogueLiteRoomParent] No room transforms available for placement");
            return;
        }

        // Initialize dynamic spawn points with original transforms
        dynamicSpawnPoints.Clear();
        dynamicSpawnPoints.AddRange(roomTransforms);
        
        // Reset required group spawn counts for this building
        requiredGroupSpawnCounts.Clear();
        
        int originalSpawnCount = dynamicSpawnPoints.Count;
        int totalRequiredRooms = buildingScriptableObj.GetTotalRequiredRoomCount(currentDifficulty);
        
        if (totalRequiredRooms > 0)
        {
            Debug.Log($"[HierarchicalPlacement] Building has {totalRequiredRooms} required room(s) to spawn");
        }

        List<int> availableSpawnIndices = new List<int>();
        for (int i = 0; i < dynamicSpawnPoints.Count; i++)
        {
            availableSpawnIndices.Add(i);
        }
        
        // Shuffle the spawn indices so rooms don't always spawn in the same order
        ShuffleList(availableSpawnIndices);

        // Add loop protection - dynamically adjust max iterations as new spawns are added
        int iterations = 0;
        int previousSpawnCount = dynamicSpawnPoints.Count;
        
        while (availableSpawnIndices.Count > 0)
        {
            // Recalculate max iterations each time (accounts for dynamically added spawns)
            int maxIterations = dynamicSpawnPoints.Count * 5; // Increased multiplier for extenders
            
            if (iterations >= maxIterations)
            {
                Debug.LogWarning($"[HierarchicalPlacement] Hit safety limit ({maxIterations} iterations) with {availableSpawnIndices.Count} remaining spawn points");
                break;
            }
            
            iterations++;
            
            int spawnIndex = availableSpawnIndices[0];
            
            // CRITICAL: Validate that the spawn point still exists and is not destroyed
            if (spawnIndex >= dynamicSpawnPoints.Count || dynamicSpawnPoints[spawnIndex] == null)
            {
                Debug.LogError($"[HierarchicalPlacement] Spawn point {spawnIndex} is invalid or destroyed! Skipping...");
                availableSpawnIndices.RemoveAt(0);
                continue;
            }
            
            // Check if this is an extender spawn point (index >= original spawn count)
            bool isExtenderSpawnPoint = spawnIndex >= originalSpawnCount;
            bool allowExtenders = !isExtenderSpawnPoint; // Don't allow extenders at extender spawn points
            
            if (ShowCollisionDebug)
            {
                string spawnType = isExtenderSpawnPoint ? "EXTENDER" : "ORIGINAL";
                Debug.Log($"[HierarchicalPlacement] Processing spawn {spawnIndex} ({spawnType}) - allowExtenders: {allowExtenders}, currentExtenders: {extendersPlaced}");
            }
            
            // First, check if there are required rooms that still need to be spawned
            GameObject roomPrefab = null;
            int spawnedGroupIndex = -1;
            
            if (!buildingScriptableObj.AreAllRequirementsSatisfied(currentDifficulty, requiredGroupSpawnCounts))
            {
                // Get a room from the required groups
                roomPrefab = buildingScriptableObj.GetRequiredRoom(currentDifficulty, requiredGroupSpawnCounts, out spawnedGroupIndex);
            }
            
            // If no required room (or all requirements satisfied), use normal room selection
            if (roomPrefab == null)
            {
                roomPrefab = buildingScriptableObj.GetBuildingRoom(currentDifficulty, extendersPlaced, allowExtenders);
            }
            
            // Use dynamic spawn points list
            Transform targetTransform = dynamicSpawnPoints[spawnIndex];
            
            bool placedSuccessfully = false;
            
            if (GuaranteedRoomPlacement(targetTransform, roomPrefab, buildingScriptableObj, currentDifficulty, $"Room {spawnIndex}", originalSpawnCount))
            {
                placedSuccessfully = true;
                availableSpawnIndices.RemoveAt(0);
                if (ShowCollisionDebug)
                    Debug.Log($"[HierarchicalPlacement] Successfully placed room at spawn {spawnIndex}");
            }
            else
            {
                // Try to find ANY room that fits without excessive overlap
                bool placed = TryPlaceAnyValidRoom(spawnIndex, buildingScriptableObj, currentDifficulty, originalSpawnCount);
                
                if (placed)
                {
                    placedSuccessfully = true;
                    availableSpawnIndices.RemoveAt(0);
                    if (ShowCollisionDebug)
                        Debug.Log($"[HierarchicalPlacement] Placed valid room at spawn {spawnIndex} after retry");
                }
                else
                {
                    // No room fits within tolerance - find and place the room with MINIMUM overlap
                    if (PlaceRoomWithMinimumOverlap(spawnIndex, buildingScriptableObj, currentDifficulty, originalSpawnCount))
                    {
                        placedSuccessfully = true;
                        availableSpawnIndices.RemoveAt(0);
                        Debug.LogWarning($"[HierarchicalPlacement] Placed room with minimum overlap at spawn {spawnIndex} (exceeded tolerance but best option)");
                    }
                    else
                    {
                        // Should never happen, but handle gracefully
                        Debug.LogError($"[HierarchicalPlacement] Failed to place any room at spawn {spawnIndex}!");
                        availableSpawnIndices.RemoveAt(0);
                    }
                }
            }
            
            // Track required group spawn counts after successful placement
            if (placedSuccessfully && placedRoomsBySpawnIndex.ContainsKey(spawnIndex))
            {
                var placedRoomData = placedRoomsBySpawnIndex[spawnIndex];
                if (placedRoomData.originalPrefab != null)
                {
                    // Check if the placed room belongs to a required group
                    int placedGroupIndex;
                    if (buildingScriptableObj.IsRoomInRequiredGroup(placedRoomData.originalPrefab, out placedGroupIndex))
                    {
                        if (!requiredGroupSpawnCounts.ContainsKey(placedGroupIndex))
                        {
                            requiredGroupSpawnCounts[placedGroupIndex] = 0;
                        }
                        requiredGroupSpawnCounts[placedGroupIndex]++;
                        
                        var groups = buildingScriptableObj.requiredRoomGroups;
                        if (placedGroupIndex < groups.Count)
                        {
                            Debug.Log($"[HierarchicalPlacement] Required group '{groups[placedGroupIndex].groupName}' count: " +
                                     $"{requiredGroupSpawnCounts[placedGroupIndex]}/{groups[placedGroupIndex].requiredCount}");
                        }
                    }
                }
            }
            
            // Check if a room extender added new spawn points
            if (dynamicSpawnPoints.Count > previousSpawnCount)
            {
                int newSpawnsAdded = dynamicSpawnPoints.Count - previousSpawnCount;
                Debug.Log($"[HierarchicalPlacement] ⚠️ Room extender added {newSpawnsAdded} new spawn points - MUST be filled!");
                
                // Add the new spawn indices to the FRONT of the available list (high priority)
                // Insert in reverse order so they maintain their original order at the front
                for (int i = dynamicSpawnPoints.Count - 1; i >= previousSpawnCount; i--)
                {
                    availableSpawnIndices.Insert(0, i);
                    Debug.Log($"[HierarchicalPlacement] Prioritizing extender spawn {i} at position {dynamicSpawnPoints[i].position}");
                }
                
                previousSpawnCount = dynamicSpawnPoints.Count;
                
                // Reset iteration counter to ensure extender spawns get processed
                iterations = 0;
                Debug.Log($"[HierarchicalPlacement] Reset iteration counter to ensure extender spawns are processed");
            }
        }

        // Final report
        int placedCount = placedRoomsBySpawnIndex.Count;
        int totalSpawns = dynamicSpawnPoints.Count;
        int extenderSpawns = totalSpawns - originalSpawnCount;
        
        if (placedCount < totalSpawns)
        {
            Debug.LogWarning($"[HierarchicalPlacement] Placed {placedCount}/{totalSpawns} rooms ({originalSpawnCount} original + {extenderSpawns} from extenders)");
            
            // Log which spawn points are missing rooms and attempt emergency placement
            List<int> emptySpawnIndices = new List<int>();
            for (int i = 0; i < dynamicSpawnPoints.Count; i++)
            {
                if (!placedRoomsBySpawnIndex.ContainsKey(i))
                {
                    emptySpawnIndices.Add(i);
                    bool isFromExtender = i >= originalSpawnCount;
                    string extenderNote = isFromExtender ? " [FROM EXTENDER - CRITICAL!]" : "";
                    Debug.LogError($"[HierarchicalPlacement] ⚠️ Spawn point {i} at position {dynamicSpawnPoints[i].position} was not filled!{extenderNote}");
                }
            }
            
            // Emergency fill for empty spawn points (especially important for extender spawns)
            if (emptySpawnIndices.Count > 0)
            {
                Debug.LogWarning($"[HierarchicalPlacement] Attempting emergency fill for {emptySpawnIndices.Count} empty spawn points...");
                foreach (int emptyIndex in emptySpawnIndices)
                {
                    // CRITICAL: Validate spawn point is still valid before attempting emergency fill
                    if (emptyIndex >= dynamicSpawnPoints.Count || dynamicSpawnPoints[emptyIndex] == null)
                    {
                        Debug.LogError($"[HierarchicalPlacement] Emergency fill skipped - spawn point {emptyIndex} is invalid or destroyed");
                        continue;
                    }
                    
                    // Try to get a basic room (non-extender) for emergency fill
                    GameObject[] allRooms = buildingScriptableObj.GetAllRooms(currentDifficulty, includeExtenders: false);
                    if (allRooms != null && allRooms.Length > 0)
                    {
                        // Find the room with minimum overlap
                        GameObject bestRoom = null;
                        float minOverlap = float.MaxValue;
                        
                        foreach (GameObject room in allRooms)
                        {
                            // Skip extenders for emergency fill to avoid infinite recursion
                            if (room.GetComponent<RoomExtender>() != null) continue;

                            float overlap = CalculateMaxOverlapForRoom(room, dynamicSpawnPoints[emptyIndex].position, dynamicSpawnPoints[emptyIndex].rotation);
                            if (overlap < minOverlap)
                            {
                                minOverlap = overlap;
                                bestRoom = room;
                            }
                        }
                        
                        if (bestRoom != null)
                        {
                            bool placed = PlaceRoomAtSpawn(emptyIndex, bestRoom, $"EMERGENCY Fill for Spawn {emptyIndex}");
                            if (placed)
                            {
                                Debug.Log($"[HierarchicalPlacement] ✓ Emergency filled spawn {emptyIndex} with {bestRoom.name}");
                            }
                            else
                            {
                                Debug.LogError($"[HierarchicalPlacement] ✗ Failed to emergency fill spawn {emptyIndex}");
                            }
                        }
                    }
                }
            }
        }
        else
        {
            string extenderInfo = extenderSpawns > 0 ? $" ({originalSpawnCount} original + {extenderSpawns} from extenders)" : "";
            Debug.Log($"[HierarchicalPlacement] ✓ Successfully placed all {placedCount} rooms{extenderInfo}");
        }
        
        // Report required room group satisfaction
        if (buildingScriptableObj.requiredRoomGroups != null && buildingScriptableObj.requiredRoomGroups.Count > 0)
        {
            Debug.Log($"[HierarchicalPlacement] Required Room Group Summary:");
            bool allSatisfied = true;
            
            for (int i = 0; i < buildingScriptableObj.requiredRoomGroups.Count; i++)
            {
                var group = buildingScriptableObj.requiredRoomGroups[i];
                if (group == null) continue;
                
                int spawnedCount = requiredGroupSpawnCounts.ContainsKey(i) ? requiredGroupSpawnCounts[i] : 0;
                bool satisfied = group.IsSatisfied(spawnedCount);
                string status = satisfied ? "✓" : "✗";
                
                Debug.Log($"  {status} '{group.groupName}': {spawnedCount}/{group.requiredCount} spawned" +
                         $"{(group.maxCount > 0 ? $" (max: {group.maxCount})" : "")}");
                
                if (!satisfied)
                {
                    allSatisfied = false;
                }
            }
            
            if (!allSatisfied)
            {
                Debug.LogWarning($"[HierarchicalPlacement] ⚠️ Not all required room groups were satisfied!");
            }
        }
    }

    /// <summary>
    /// Intelligently place rooms using constraint satisfaction with room swapping
    /// Tries to resolve conflicts by swapping blocking rooms with smaller alternatives
    /// </summary>
    private bool GuaranteedRoomPlacement(Transform targetTransform, GameObject preferredRoomPrefab, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, string debugName, int originalSpawnCount)
    {
        int spawnIndex = GetSpawnIndex(targetTransform);
        if (spawnIndex == -1)
        {
            Debug.LogError($"[SmartPlacement] Could not find spawn index for {targetTransform.name}");
            return false;
        }

        return SmartRoomPlacement(spawnIndex, preferredRoomPrefab, buildingScriptableObj, currentDifficulty, debugName, 0, originalSpawnCount);
    }

    /// <summary>
    /// Smart room placement with conflict resolution through room swapping and minimum overlap selection
    /// </summary>
    private bool SmartRoomPlacement(int spawnIndex, GameObject preferredRoomPrefab, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, string debugName, int retryCount, int originalSpawnCount)
    {
        if (retryCount >= maxSwapRetries)
        {
            // Use minimum overlap placement after max retries
            return PlaceRoomWithMinimumOverlap(spawnIndex, buildingScriptableObj, currentDifficulty, originalSpawnCount);
        }

        Transform targetTransform = dynamicSpawnPoints[spawnIndex];

        // Check if this is an extender spawn point
        bool isExtenderSpawnPoint = spawnIndex >= originalSpawnCount;

        // Strategy 1: Try the preferred room first
        if (preferredRoomPrefab != null)
        {
            // Check if the preferred room is an extender
            RoomExtender preferredExtender = preferredRoomPrefab.GetComponent<RoomExtender>();
            bool isPreferredExtender = preferredExtender != null;
            
            if (TryPlaceWithConflictResolution(spawnIndex, preferredRoomPrefab, buildingScriptableObj, currentDifficulty, debugName + " (Preferred)", retryCount))
                return true;
            
            // If we specifically wanted an extender but it failed, don't fall back to other room types
            // This preserves the intent to place an extender at original spawn points
            if (isPreferredExtender && !isExtenderSpawnPoint)
            {
                Debug.Log($"[SmartPlacement] Extender placement failed at spawn {spawnIndex}, but not falling back to other room types to preserve extender intent");
                return false;
            }
        }

        // Strategy 2: Try room with minimum overlap directly (only for non-extender spawn points)
        if (!isExtenderSpawnPoint)
        {
            GameObject bestRoom = FindRoomWithMinimumOverlap(spawnIndex, buildingScriptableObj, currentDifficulty, originalSpawnCount, out float minOverlapVolume);
            if (bestRoom != null && minOverlapVolume <= maxAcceptableOverlapVolume)
            {
                if (TryPlaceWithConflictResolution(spawnIndex, bestRoom, buildingScriptableObj, currentDifficulty, debugName + $" (Min Overlap {minOverlapVolume:F1} units)", retryCount))
                return true;
            }
        }

        // Strategy 3: Try any available room
        bool allowExtenders = !isExtenderSpawnPoint; // Don't allow extenders at extender spawn points
        
        GameObject anyRoom = buildingScriptableObj.GetBuildingRoom(currentDifficulty, extendersPlaced, allowExtenders);
        if (anyRoom != null)
        {
            if (TryPlaceWithConflictResolution(spawnIndex, anyRoom, buildingScriptableObj, currentDifficulty, debugName + " (Any)", retryCount))
                return true;
        }

        // Final fallback: Use minimum overlap placement
        return PlaceRoomWithMinimumOverlap(spawnIndex, buildingScriptableObj, currentDifficulty, originalSpawnCount);
    }

    /// <summary>
    /// Get the spawn index for a given transform
    /// </summary>
    private int GetSpawnIndex(Transform targetTransform)
    {
        // Search in dynamic spawn points (includes original + extender spawns)
        for (int i = 0; i < dynamicSpawnPoints.Count; i++)
        {
            if (dynamicSpawnPoints[i] == targetTransform)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Try to place a room with intelligent conflict resolution through room swapping
    /// </summary>
    private bool TryPlaceWithConflictResolution(int spawnIndex, GameObject roomPrefab, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, string debugName, int retryCount)
    {
        Transform targetTransform = dynamicSpawnPoints[spawnIndex];
        
        // Check if the room being placed is an extender
        RoomExtender extenderComponent = roomPrefab.GetComponent<RoomExtender>();
        bool isExtender = extenderComponent != null;
        
        // Check if room would fit without conflicts
        if (!WouldRoomOverlapAtPosition(roomPrefab.GetComponent<RogueLiteRoom>(), targetTransform.position, targetTransform.rotation))
        {
            // No conflicts - place the room directly
            return PlaceRoomAtSpawn(spawnIndex, roomPrefab, debugName);
        }

        // If this is an extender and it has conflicts, don't try to resolve them
        // Extenders should only be placed if they fit perfectly
        if (isExtender)
        {
            Debug.Log($"[ConflictResolution] Extender at spawn {spawnIndex} has conflicts - will not place it");
            return false;
        }

        // Find conflicting rooms
        var conflictingSpawns = FindConflictingRooms(roomPrefab, targetTransform.position);
        
        if (conflictingSpawns.Count == 0)
        {
            Debug.LogWarning($"[ConflictResolution] No specific conflicts found, but room still doesn't fit at spawn {spawnIndex}");
            return false;
        }

        // Limit the number of swaps to prevent infinite loops
        int maxSwapsPerConflict = 2;
        int swapAttempts = 0;

        // Try to resolve conflicts by swapping conflicting rooms with smaller alternatives
        foreach (int conflictingSpawn in conflictingSpawns)
        {
            if (swapAttempts >= maxSwapsPerConflict)
            {
                break;
            }

            if (AttemptRoomSwap(conflictingSpawn, buildingScriptableObj, currentDifficulty, retryCount))
            {
                swapAttempts++;
                
                // Conflict resolved, try placing the original room again
                if (!WouldRoomOverlapAtPosition(roomPrefab.GetComponent<RogueLiteRoom>(), targetTransform.position, targetTransform.rotation))
                {
                    return PlaceRoomAtSpawn(spawnIndex, roomPrefab, debugName + " (After Swap)");
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Find and place the room with the absolute MINIMUM overlap, even if it exceeds tolerance
    /// This is used as a last resort to ensure every spawn point gets a room
    /// </summary>
    private bool PlaceRoomWithMinimumOverlap(int spawnIndex, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, int originalSpawnCount)
    {
        Transform targetTransform = dynamicSpawnPoints[spawnIndex];
        
        // Check if this is an extender spawn point and exclude extenders accordingly
        bool isExtenderSpawnPoint = spawnIndex >= originalSpawnCount;
        bool includeExtenders = !isExtenderSpawnPoint; // Don't include extenders at extender spawn points
        
        GameObject[] allRooms = buildingScriptableObj.GetAllRooms(currentDifficulty, includeExtenders);
        
        if (allRooms == null || allRooms.Length == 0)
        {
            Debug.LogError($"[PlaceRoomWithMinimumOverlap] No rooms available for difficulty {currentDifficulty}");
            return false;
        }

        // Track all rooms with their overlap values
        List<(GameObject room, float overlap)> roomOptions = new List<(GameObject, float)>();
        float minOverlapVolume = float.MaxValue;
        
        Debug.Log($"[PlaceRoomWithMinimumOverlap] Testing {allRooms.Length} rooms for spawn {spawnIndex}:");
        
        // Test every available room and track their overlap values
        foreach (GameObject roomPrefab in allRooms)
        {
            if (roomPrefab == null) continue;
            
            RogueLiteRoom roomComponent = roomPrefab.GetComponent<RogueLiteRoom>();
            if (roomComponent == null) continue;

            float overlapVolume = CalculateMaxOverlapForRoom(roomPrefab, targetTransform.position, targetTransform.rotation);
            roomOptions.Add((roomPrefab, overlapVolume));

            string selectionMarker = "";
            if (overlapVolume < minOverlapVolume)
            {
                selectionMarker = " ← NEW BEST";
                minOverlapVolume = overlapVolume;
            }
            
            // ALWAYS log all room comparisons showing cubic units
            Debug.Log($"  • {roomPrefab.name}: {overlapVolume:F1} cubic units overlap{selectionMarker}");
        }
        
        // Find all rooms that match the minimum overlap (within small tolerance for floating point comparison)
        const float equalityTolerance = 0.01f;
        List<GameObject> bestRooms = new List<GameObject>();
        foreach (var option in roomOptions)
        {
            if (Mathf.Abs(option.overlap - minOverlapVolume) < equalityTolerance)
            {
                bestRooms.Add(option.room);
            }
        }

        // Randomly select from rooms with minimum overlap for variety
        GameObject bestRoom = bestRooms.Count > 0 ? bestRooms[Random.Range(0, bestRooms.Count)] : null;
        
        // Place the room with minimum overlap
        if (bestRoom != null)
        {
            string statusMessage = minOverlapVolume == 0 ? "✓ NO OVERLAP" : $"Overlap: {minOverlapVolume:F1} cubic units";
            string randomInfo = bestRooms.Count > 1 ? $" [Randomly selected from {bestRooms.Count} rooms with same overlap]" : "";
            Debug.Log($"[PlaceRoomWithMinimumOverlap] Selected {bestRoom.name} at spawn {spawnIndex} ({statusMessage}){randomInfo}");
            
            bool placed = PlaceRoomAtSpawn(spawnIndex, bestRoom, $"Min Overlap Room ({minOverlapVolume:F1} units)");
            
            // Store overlap metadata for visualization (convert to approximate percentage for display)
            if (placed && placedRoomsBySpawnIndex.ContainsKey(spawnIndex))
            {
                // Calculate approximate percentage for visualization only
                RogueLiteRoom placedRoomComp = bestRoom.GetComponent<RogueLiteRoom>();
                if (placedRoomComp != null)
                {
                    Bounds roomBounds = placedRoomComp.CalculateTestBounds(targetTransform.position);
                    float roomVolume = roomBounds.size.x * roomBounds.size.y * roomBounds.size.z;
                    float approxPercentage = roomVolume > 0 ? (minOverlapVolume / roomVolume) * 100f : 0f;
                    
                    placedRoomsBySpawnIndex[spawnIndex].overlapPercentage = approxPercentage;
                    placedRoomsBySpawnIndex[spawnIndex].exceededTolerance = minOverlapVolume > 0;
                }
            }
            
            return placed;
        }

        return false;
    }

    /// <summary>
    /// Try to place ANY room from the available pool that fits with minimal or zero overlap
    /// Prioritizes rooms with least overlap
    /// </summary>
    private bool TryPlaceAnyValidRoom(int spawnIndex, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, int originalSpawnCount)
    {
        Transform targetTransform = dynamicSpawnPoints[spawnIndex];
        
        // Check if this is an extender spawn point and exclude extenders accordingly
        bool isExtenderSpawnPoint = spawnIndex >= originalSpawnCount;
        bool includeExtenders = !isExtenderSpawnPoint; // Don't include extenders at extender spawn points
        
        GameObject[] allRooms = buildingScriptableObj.GetAllRooms(currentDifficulty, includeExtenders);
        
        if (allRooms == null || allRooms.Length == 0)
        {
            Debug.LogError($"[TryPlaceAnyValidRoom] No rooms available for difficulty {currentDifficulty}");
            return false;
        }

        // Collect all rooms that fit within tolerance
        List<(GameObject room, float overlap)> validRooms = new List<(GameObject, float)>();
        
        foreach (GameObject roomPrefab in allRooms)
        {
            if (roomPrefab == null) continue;
            
            RogueLiteRoom roomComponent = roomPrefab.GetComponent<RogueLiteRoom>();
            if (roomComponent == null) continue;

            // Check if room fits with minimal overlap
            float overlapVolume = CalculateMaxOverlapForRoom(roomPrefab, targetTransform.position, targetTransform.rotation);

            if (overlapVolume <= maxAcceptableOverlapVolume)
            {
                validRooms.Add((roomPrefab, overlapVolume));
            }
            else if (ShowCollisionDebug)
            {
                Debug.Log($"[TryPlaceAnyValidRoom] Room {roomPrefab.name} rejected at spawn {spawnIndex} - {overlapVolume:F1} cubic units exceeds {maxAcceptableOverlapVolume} tolerance");
            }
        }
        
        // If we have valid rooms, randomly select one
        if (validRooms.Count > 0)
        {
            var selectedRoom = validRooms[Random.Range(0, validRooms.Count)];
            
            if (PlaceRoomAtSpawn(spawnIndex, selectedRoom.room, $"Valid Room ({selectedRoom.overlap:F1} units)"))
            {
                if (ShowCollisionDebug)
                    Debug.Log($"[TryPlaceAnyValidRoom] Placed {selectedRoom.room.name} at spawn {spawnIndex} with {selectedRoom.overlap:F1} cubic units overlap [Randomly selected from {validRooms.Count} valid options]");
                return true;
            }
        }
        
        if (ShowCollisionDebug)
            Debug.LogWarning($"[TryPlaceAnyValidRoom] No room found for spawn {spawnIndex} with less than {maxAcceptableOverlapVolume} cubic units overlap");
        
        return false;
    }

    /// <summary>
    /// Find the room that produces the minimum overlap at a given spawn point
    /// Randomly selects from all rooms with the same minimum overlap for variety
    /// </summary>
    private GameObject FindRoomWithMinimumOverlap(int spawnIndex, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, int originalSpawnCount, out float minOverlap)
    {
        Transform targetTransform = dynamicSpawnPoints[spawnIndex];
        
        minOverlap = float.MaxValue;
        
        // Check if this is an extender spawn point and exclude extenders accordingly
        bool isExtenderSpawnPoint = spawnIndex >= originalSpawnCount;
        bool includeExtenders = !isExtenderSpawnPoint; // Don't include extenders at extender spawn points
        
        // Test all available rooms and track their overlap values
        GameObject[] allRooms = buildingScriptableObj.GetAllRooms(currentDifficulty, includeExtenders);
        List<(GameObject room, float overlap)> roomOptions = new List<(GameObject, float)>();
        
        foreach (var roomPrefab in allRooms)
        {
            if (roomPrefab == null) continue;

            float maxOverlap = CalculateMaxOverlapForRoom(roomPrefab, targetTransform.position, targetTransform.rotation);
            roomOptions.Add((roomPrefab, maxOverlap));

            if (maxOverlap < minOverlap)
            {
                minOverlap = maxOverlap;
            }
        }
        
        // Find all rooms that match the minimum overlap
        const float equalityTolerance = 0.01f;
        List<GameObject> bestRooms = new List<GameObject>();
        foreach (var option in roomOptions)
        {
            if (Mathf.Abs(option.overlap - minOverlap) < equalityTolerance)
            {
                bestRooms.Add(option.room);
            }
        }
        
        // Randomly select from rooms with minimum overlap
        return bestRooms.Count > 0 ? bestRooms[Random.Range(0, bestRooms.Count)] : null;
    }

    /// <summary>
    /// Calculate the maximum overlap volume (in cubic units) for this room with existing rooms.
    /// Returns raw intersection volume - smaller is better.
    /// This naturally favors smaller rooms and rooms with minimal physical overlap.
    /// </summary>
    private float CalculateMaxOverlapForRoom(GameObject roomPrefab, Vector3 position, Quaternion rotation)
    {
        RogueLiteRoom testRoom = roomPrefab.GetComponent<RogueLiteRoom>();
        if (testRoom == null) return float.MaxValue;

        // Use the new CalculateTestBounds method that accounts for rotation
        Bounds testBounds = testRoom.CalculateTestBounds(position, rotation);

        float maxIntersectionVolume = 0f;
        
        if (ShowCollisionDebug)
        {
            Debug.Log($"[OverlapCalc] Testing {roomPrefab.name} at position {position}");
            Debug.Log($"[OverlapCalc]   Test bounds: center={testBounds.center}, size={testBounds.size}, min={testBounds.min}, max={testBounds.max}");
        }

        // Check against all placed rooms
        foreach (var kvp in placedRoomsBySpawnIndex)
        {
            var roomData = kvp.Value;
            if (roomData.roomObject == null) continue;
            
            RogueLiteRoom existingRoom = roomData.roomObject.GetComponent<RogueLiteRoom>();
            if (existingRoom == null) continue;
            
            Bounds existingBounds = existingRoom.GetWorldBounds();
            
            if (ShowCollisionDebug)
            {
                Debug.Log($"[OverlapCalc]   Checking against {roomData.debugName} (spawn {kvp.Key})");
                Debug.Log($"[OverlapCalc]     Existing bounds: center={existingBounds.center}, size={existingBounds.size}, min={existingBounds.min}, max={existingBounds.max}");
            }
            
            // Check for overlap
            if (testBounds.Intersects(existingBounds))
            {
                // Calculate intersection volume (RAW cubic units)
                Vector3 intersectionMin = Vector3.Max(testBounds.min, existingBounds.min);
                Vector3 intersectionMax = Vector3.Min(testBounds.max, existingBounds.max);
                
                if (intersectionMin.x < intersectionMax.x && 
                    intersectionMin.y < intersectionMax.y && 
                    intersectionMin.z < intersectionMax.z)
                {
                    Vector3 intersectionSize = intersectionMax - intersectionMin;
                    float intersectionVolume = intersectionSize.x * intersectionSize.y * intersectionSize.z;
                    
                    // Check if this intersection is SPECIFICALLY between the two spawn points (for doorway connections)
                    // Only ignore overlap if it's directly on the line between this spawn and the existing room's spawn
                    Vector3 intersectionCenter = (intersectionMin + intersectionMax) * 0.5f;
                    bool isConnectionOverlap = false;
                    
                    // Get the spawn point positions for both rooms
                    Vector3 thisSpawnPosition = position; // Current room being placed
                    Vector3 existingSpawnPosition = roomData.roomObject.transform.position; // Existing room's position
                    
                    // Check if the intersection is approximately on the line between the two spawns
                    Vector3 spawnToSpawn = existingSpawnPosition - thisSpawnPosition;
                    float spawnDistance = spawnToSpawn.magnitude;
                    
                    if (spawnDistance > 0.1f) // Avoid division by zero
                    {
                        // Project intersection center onto the line between spawns
                        Vector3 spawnDirection = spawnToSpawn / spawnDistance;
                        Vector3 toIntersection = intersectionCenter - thisSpawnPosition;
                        float projectionLength = Vector3.Dot(toIntersection, spawnDirection);
                        Vector3 projectionPoint = thisSpawnPosition + spawnDirection * projectionLength;
                        
                        // Check if intersection is close to the line and between the spawn points
                        float distanceFromLine = Vector3.Distance(intersectionCenter, projectionPoint);
                        bool onConnectionLine = distanceFromLine <= spawnPointExclusionRadius;
                        bool betweenSpawns = projectionLength >= 0 && projectionLength <= spawnDistance;
                        
                        isConnectionOverlap = onConnectionLine && betweenSpawns;
                        
                        if (ShowCollisionDebug && isConnectionOverlap)
                        {
                            Debug.Log($"[OverlapCalc]     OVERLAP ON CONNECTION LINE (dist from line: {distanceFromLine:F1} <= {spawnPointExclusionRadius:F1}) - IGNORED for doorway");
                        }
                    }
                    
                    // Only count overlap if it's NOT a connection overlap
                    if (!isConnectionOverlap)
                    {
                        if (ShowCollisionDebug)
                        {
                            Debug.Log($"[OverlapCalc]     INTERSECTION DETECTED! Volume: {intersectionVolume:F1} cubic units");
                            Debug.Log($"[OverlapCalc]       Intersection: min={intersectionMin}, max={intersectionMax}, size={intersectionSize}");
                        }
                        
                        // Use the largest intersection found
                        maxIntersectionVolume = Mathf.Max(maxIntersectionVolume, intersectionVolume);
                    }
                }
            }
            else if (ShowCollisionDebug)
            {
                Debug.Log($"[OverlapCalc]     No intersection detected");
            }
            
            // Also check minimum distance requirement
            float centerDistance = Vector3.Distance(testBounds.center, existingBounds.center);
            if (centerDistance < minRoomDistance)
            {
                // Penalize rooms that are too close with a large virtual intersection volume
                float distancePenalty = (minRoomDistance - centerDistance) * 500f; // Scale to cubic units
                
                if (ShowCollisionDebug)
                {
                    Debug.Log($"[OverlapCalc]     DISTANCE PENALTY! Distance: {centerDistance:F1} < min: {minRoomDistance:F1}, penalty: {distancePenalty:F1}");
                }
                
                maxIntersectionVolume = Mathf.Max(maxIntersectionVolume, distancePenalty);
            }
            else if (ShowCollisionDebug)
            {
                Debug.Log($"[OverlapCalc]     Distance OK: {centerDistance:F1} >= {minRoomDistance:F1}");
            }
        }

        if (ShowCollisionDebug)
        {
            Debug.Log($"[OverlapCalc] Result for {roomPrefab.name}: Max overlap volume = {maxIntersectionVolume:F1} cubic units");
        }

        return maxIntersectionVolume;
    }

    /// <summary>
    /// Place a room at a specific spawn index and track it properly
    /// Handles room extenders that add additional spawn points
    /// </summary>
    private bool PlaceRoomAtSpawn(int spawnIndex, GameObject roomPrefab, string debugName)
    {
        if (!roomPrefab.GetComponent<RogueLiteRoom>())
        {
            Debug.LogError($"{roomPrefab.name} has no RogueLiteRoom Component");
            return false;
        }

        Transform targetTransform = dynamicSpawnPoints[spawnIndex];
        
        // Remove existing room at this spawn if any
        if (placedRoomsBySpawnIndex.ContainsKey(spawnIndex))
        {
            var existingRoom = placedRoomsBySpawnIndex[spawnIndex];
            if (existingRoom.roomObject != null)
            {
                // CRITICAL: Check if the room being removed is an extender
                // Extenders add spawn points to the dynamic list, and removing them would orphan those spawn points
                RoomExtender existingExtender = existingRoom.roomObject.GetComponent<RoomExtender>();
                if (existingExtender != null)
                {
                    Debug.LogError($"[PlaceRoomAtSpawn] CANNOT replace extender at spawn {spawnIndex}! " +
                                  $"Extenders add spawn points that would become orphaned. Keeping existing extender.");
                    return false; // Refuse to replace the extender
                }
                
                spawnedRooms.Remove(targetTransform.position);
                Destroy(existingRoom.roomObject);
            }
            placedRoomsBySpawnIndex.Remove(spawnIndex);
        }

        // Place the new room (parent to this RogueLiteRoomParent, not to the spawn point itself)
        // This prevents nested transform hierarchies when using extender spawn points
        GameObject room = Instantiate(roomPrefab, targetTransform.position, targetTransform.rotation, this.transform);
        spawnedRooms[targetTransform.position] = room;

        // Get room component and ensure bounds are calculated at the new position
        RogueLiteRoom roomComponent = room.GetComponent<RogueLiteRoom>();
        if (roomComponent == null)
        {
            Debug.LogError($"[PlaceRoomAtSpawn] Instantiated room has no RogueLiteRoom component!");
            Destroy(room);
            return false;
        }

        // Force bounds recalculation at the new position (Awake should have done this, but be safe)
        roomComponent.CalculateRoomBounds();
        
        // Get the actual placed bounds for verification
        Bounds placedBounds = roomComponent.GetWorldBounds();

        // Track the placed room data
        var roomData = new PlacedRoomData(room, roomPrefab, spawnIndex, debugName);
        placedRoomsBySpawnIndex[spawnIndex] = roomData;

        // Setup the room (doors, chests, etc.)
        roomComponent.Setup();
        RandomizePropsInSection(room.transform);
        
        // Update the parent's room type based on placed rooms
        UpdateParentRoomType();
        
        // Log placement with full bounds info
        Debug.Log($"[PlaceRoomAtSpawn] Placed {roomPrefab.name} as '{debugName}' at spawn {spawnIndex}");
        Debug.Log($"  Position: {targetTransform.position}");
        Debug.Log($"  Bounds: center={placedBounds.center}, size={placedBounds.size}");
        Debug.Log($"  Bounds: min={placedBounds.min}, max={placedBounds.max}");
        
        // Now check if this newly placed room overlaps with any existing rooms
        if (ShowCollisionDebug)
        {
            Debug.Log($"[PlaceRoomAtSpawn] Checking for overlaps with previously placed rooms...");
            foreach (var kvp in placedRoomsBySpawnIndex)
            {
                if (kvp.Key == spawnIndex) continue; // Skip self
                
                var otherRoomData = kvp.Value;
                if (otherRoomData.roomObject == null) continue;
                
                RogueLiteRoom otherRoom = otherRoomData.roomObject.GetComponent<RogueLiteRoom>();
                if (otherRoom == null) continue;
                
                Bounds otherBounds = otherRoom.GetWorldBounds();
                
                if (placedBounds.Intersects(otherBounds))
                {
                    Vector3 intersectionMin = Vector3.Max(placedBounds.min, otherBounds.min);
                    Vector3 intersectionMax = Vector3.Min(placedBounds.max, otherBounds.max);
                    
                    if (intersectionMin.x < intersectionMax.x && 
                        intersectionMin.y < intersectionMax.y && 
                        intersectionMin.z < intersectionMax.z)
                    {
                        Vector3 intersectionSize = intersectionMax - intersectionMin;
                        float intersectionVolume = intersectionSize.x * intersectionSize.y * intersectionSize.z;
                        
                        Debug.LogWarning($"[PlaceRoomAtSpawn] ⚠️ OVERLAP DETECTED after placement!");
                        Debug.LogWarning($"  {roomPrefab.name} at spawn {spawnIndex} overlaps with {otherRoomData.debugName} at spawn {kvp.Key}");
                        Debug.LogWarning($"  Intersection volume: {intersectionVolume:F1} cubic units");
                        Debug.LogWarning($"  Intersection: min={intersectionMin}, max={intersectionMax}, size={intersectionSize}");
                    }
                }
            }
        }
        
        // Check if this is a room extender and add its spawn points
        RoomExtender extender = room.GetComponent<RoomExtender>();
        if (extender != null)
        {
            // Increment extender counter
            extendersPlaced++;
            
            // Set the extender's room type based on the current building composition
            RogueLikeRoomType extenderType = DetermineExtenderRoomType();
            extender.SetRoomType(extenderType);
            
            // Add spawn points if available
            if (extender.HasSpawnPoints())
            {
                Transform[] extenderSpawnPoints = extender.GetAdditionalSpawnPoints();
                int addedCount = extenderSpawnPoints.Length;
                
                Debug.Log($"[PlaceRoomAtSpawn] Room extender #{extendersPlaced} detected! Type: {extenderType}, Adding {addedCount} new spawn points to building");
                
                foreach (Transform newSpawnPoint in extenderSpawnPoints)
                {
                    dynamicSpawnPoints.Add(newSpawnPoint);
                    Debug.Log($"[PlaceRoomAtSpawn] Added extender spawn point at position {newSpawnPoint.position}");
                }
                
                // Add these new indices to the available spawn list in HierarchicalRoomPlacement
                // Note: The calling method (HierarchicalRoomPlacement) will handle adding these to availableSpawnIndices
                Debug.Log($"[PlaceRoomAtSpawn] Building now has {dynamicSpawnPoints.Count} total spawn points ({extendersPlaced} extenders placed)");
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Update the parent's room type based on the placed rooms
    /// If any single room is friendly, the entire parent becomes friendly
    /// </summary>
    private void UpdateParentRoomType()
    {
        bool hasFriendlyRoom = false;
        
        foreach (var kvp in placedRoomsBySpawnIndex)
        {
            var roomData = kvp.Value;
            if (roomData.roomObject != null)
            {
                RogueLiteRoom roomComponent = roomData.roomObject.GetComponent<RogueLiteRoom>();
                if (roomComponent != null)
                {
                    // Skip extenders when determining parent type (they inherit, not determine)
                    if (roomComponent is RoomExtender)
                        continue;
                    
                    if (roomComponent.RoomType == RogueLikeRoomType.FRIENDLY)
                    {
                        hasFriendlyRoom = true;
                    }
                }
            }
        }
        
        // If any room is friendly, set the parent to friendly
        if (hasFriendlyRoom)
        {
            roomType = RogueLikeRoomType.FRIENDLY;
        }
        else
        {
            roomType = RogueLikeRoomType.HOSTILE;
        }
    }
    
    /// <summary>
    /// Determine what room type an extender should have based on existing rooms
    /// Returns FRIENDLY if any non-extender room is friendly, otherwise HOSTILE
    /// </summary>
    private RogueLikeRoomType DetermineExtenderRoomType()
    {
        foreach (var kvp in placedRoomsBySpawnIndex)
        {
            var roomData = kvp.Value;
            if (roomData.roomObject != null)
            {
                RogueLiteRoom roomComponent = roomData.roomObject.GetComponent<RogueLiteRoom>();
                if (roomComponent != null && !(roomComponent is RoomExtender))
                {
                    if (roomComponent.RoomType == RogueLikeRoomType.FRIENDLY)
                    {
                        return RogueLikeRoomType.FRIENDLY;
                    }
                }
            }
        }
        
        // Default to hostile if no friendly rooms found
        return RogueLikeRoomType.HOSTILE;
    }

    /// <summary>
    /// Find which spawn indices have rooms that would conflict with a placement
    /// </summary>
    private List<int> FindConflictingRooms(GameObject roomPrefab, Vector3 testPosition)
    {
        List<int> conflictingSpawns = new List<int>();
        
        RogueLiteRoom testRoom = roomPrefab.GetComponent<RogueLiteRoom>();
        if (testRoom == null) return conflictingSpawns;

        // Use the new CalculateTestBounds method that doesn't modify the original prefab
        Bounds testBounds = testRoom.CalculateTestBounds(testPosition);

        // Check against all placed rooms
        foreach (var kvp in placedRoomsBySpawnIndex)
        {
            int spawnIndex = kvp.Key;
            var roomData = kvp.Value;
            
            if (roomData.roomObject == null) continue;
            
            RogueLiteRoom existingRoom = roomData.roomObject.GetComponent<RogueLiteRoom>();
            if (existingRoom == null) continue;
            
            Bounds existingBounds = existingRoom.GetWorldBounds();
            
            // Check for excessive overlap
            if (testBounds.Intersects(existingBounds))
            {
                // Calculate intersection volume
                Vector3 intersectionMin = Vector3.Max(testBounds.min, existingBounds.min);
                Vector3 intersectionMax = Vector3.Min(testBounds.max, existingBounds.max);
                
                if (intersectionMin.x < intersectionMax.x && 
                    intersectionMin.y < intersectionMax.y && 
                    intersectionMin.z < intersectionMax.z)
                {
                    Vector3 intersectionSize = intersectionMax - intersectionMin;
                    float intersectionVolume = intersectionSize.x * intersectionSize.y * intersectionSize.z;
                    
                    if (intersectionVolume > maxAcceptableOverlapVolume)
                {
                    conflictingSpawns.Add(spawnIndex);
                    }
                }
            }
        }

        return conflictingSpawns;
    }

    /// <summary>
    /// Attempt to swap a room at the given spawn index with a different alternative
    /// </summary>
    private bool AttemptRoomSwap(int spawnIndex, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, int retryCount)
    {
        if (!placedRoomsBySpawnIndex.ContainsKey(spawnIndex))
        {
            return false; // No room to swap
        }

        // Check if the current room is an extender - if so, NEVER swap it out
        PlacedRoomData currentRoomData = placedRoomsBySpawnIndex[spawnIndex];
        if (currentRoomData.roomObject != null)
        {
            RoomExtender existingExtender = currentRoomData.roomObject.GetComponent<RoomExtender>();
            if (existingExtender != null)
            {
                Debug.Log($"[AttemptRoomSwap] Room at spawn {spawnIndex} is an extender - will NOT swap it out");
                return false;
            }
        }

        // Try to find a different room that fits better (but DON'T include extenders as alternatives)
        // Extenders should only be placed intentionally, not as conflict resolution alternatives
        GameObject[] allRooms = buildingScriptableObj.GetAllRooms(currentDifficulty, includeExtenders: false);
        
        foreach (var alternativeRoom in allRooms)
        {
            if (alternativeRoom == null) continue;
            
            // Test if the alternative room would work at this position
            Transform targetTransform = dynamicSpawnPoints[spawnIndex];
            if (!WouldRoomOverlapAtPosition(alternativeRoom.GetComponent<RogueLiteRoom>(), targetTransform.position, targetTransform.rotation))
            {
                // Swap successful
                if (PlaceRoomAtSpawn(spawnIndex, alternativeRoom, $"Swapped to alternative"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Attempt to instantiate a room with optional collision checking
    /// </summary>
    private bool TryInstantiateRoom(Transform targetTransform, GameObject roomPrefab, string debugName, bool checkCollisions)
    {
        if (!roomPrefab.GetComponent<RogueLiteRoom>())
        {
            Debug.LogError($"{roomPrefab.name} has no RogueLiteRoom Component");
            return false;
        }

        // Check for collisions if requested
        if (checkCollisions && WouldRoomOverlapAtPosition(roomPrefab.GetComponent<RogueLiteRoom>(), targetTransform.position, targetTransform.rotation))
        {
            Debug.Log($"[RoomPlacement] {debugName} would overlap at {targetTransform.position} - trying next option");
            return false;
        }

        // Place the room
        GameObject room = Instantiate(roomPrefab, targetTransform.position, targetTransform.rotation, targetTransform);
        spawnedRooms[targetTransform.position] = room;

        RogueLiteRoom roomComponent = room.GetComponent<RogueLiteRoom>();
        roomComponent.Setup();
        RandomizePropsInSection(room.transform);
        
        string collisionNote = checkCollisions ? "with collision check" : "FORCED (no collision check)";
        Debug.Log($"[RoomPlacement] Successfully placed {debugName} at {targetTransform.position} ({collisionNote})");
        return true;
    }

    private void RandomizePropsInSection(Transform sectionTransform)
    {
        if (sectionTransform == null) return;

        PropRandomizer[] propRandomizers = sectionTransform.GetComponentsInChildren<PropRandomizer>();

        foreach (PropRandomizer propRandomizer in propRandomizers)
        {
            propRandomizer.RandomizeProps();
        }
    }

    /// <summary>
    /// Configures the NavMeshSurface with optimal settings for dynamic room spawning.
    /// </summary>
    private void ConfigureNavMeshSurface()
    {
        bool settingsChanged = false;

        // Ensure Collect Objects is set to All (required for dynamically spawned rooms)
        if (navMeshSurface.collectObjects != Unity.AI.Navigation.CollectObjects.All)
        {
            Debug.Log($"[RogueLiteRoomParent] Changing NavMeshSurface 'Collect Objects' from '{navMeshSurface.collectObjects}' to 'All' for dynamic room spawning");
            navMeshSurface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
            settingsChanged = true;
        }

        // Ensure Use Geometry is set to Physics Colliders (doesn't require mesh read/write, works in builds)
        if (navMeshSurface.useGeometry != NavMeshCollectGeometry.PhysicsColliders)
        {
            Debug.Log($"[RogueLiteRoomParent] Changing NavMeshSurface 'Use Geometry' from '{navMeshSurface.useGeometry}' to 'PhysicsColliders' for proper floor detection");
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            settingsChanged = true;
        }

        if (settingsChanged)
        {
            Debug.Log("[RogueLiteRoomParent] NavMeshSurface configured for dynamic room spawning");
        }
        else
        {
            Debug.Log("[RogueLiteRoomParent] NavMeshSurface already configured correctly");
        }
    }

    private IEnumerator DelayedBakeNavMesh()
    {
        // Wait for end of frame to ensure all Instantiate calls are complete
        yield return new WaitForEndOfFrame();
        
        // Wait for physics to update (important after transform hierarchy changes)
        yield return new WaitForFixedUpdate();
        
        // Add a small additional delay to ensure all colliders are in final positions
        yield return new WaitForSeconds(0.2f);

        Debug.Log("[RogueLiteRoomParent] Starting NavMesh baking...");

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("[RogueLiteRoomParent] NavMesh baking complete");
        }
        else
        {
            Debug.LogError("NavMeshSurface is not assigned.");
        }
    }

    private void SetupDoors()
    {
        List<RogueLikeRoomDoor> allDoors = new List<RogueLikeRoomDoor>(transform.GetComponentsInChildren<RogueLikeRoomDoor>());

        if (allDoors.Count == 0)
        {
            Debug.LogWarning("[RogueLiteRoomParent] No doors found to setup!");
            return;
        }

        // Filter out doors that don't have valid floors behind them
        List<RogueLikeRoomDoor> validDoors = new List<RogueLikeRoomDoor>();
        List<RogueLikeRoomDoor> invalidDoors = new List<RogueLikeRoomDoor>();

        foreach (var door in allDoors)
        {
            if (door.HasValidFloorBehindDoor())
            {
                validDoors.Add(door);
            }
            else
            {
                invalidDoors.Add(door);
                Debug.Log($"[SetupDoors] Door {door.gameObject.name} excluded - no valid floor behind it");
            }
        }

        // CRITICAL FIX: If we don't have enough valid doors for EXIT + at least 1 ENTRANCE, use all doors
        // We need minimum 2 doors: 1 for EXIT (player spawn) and 1 for ENTRANCE (exit to next room)
        if (validDoors.Count < 2)
        {
            Debug.LogWarning($"[RogueLiteRoomParent] Only {validDoors.Count} valid doors found (need 2+ for EXIT and ENTRANCE)! Using all {allDoors.Count} doors as fallback.");
            validDoors = allDoors;
            invalidDoors.Clear();
        }

        // Assign a random door as the player spawn door (locked - can't go back)
        int spawnDoorIndex = Random.Range(0, validDoors.Count);
        playerSpawnPoint = validDoors[spawnDoorIndex].playerSpawn;
        validDoors[spawnDoorIndex].doorType = DoorStatus.LOCKED;
        validDoors[spawnDoorIndex].SetAsSpawnDoor(true); // Mark as spawn door - stays locked forever

        // Store the spawn door
        RogueLikeRoomDoor spawnDoor = validDoors[spawnDoorIndex];
        validDoors.RemoveAt(spawnDoorIndex);

        // Select 1-3 doors to be progression doors (initially locked, unlock when waves cleared)
        // These doors will be assigned target rooms by the building manager when player enters them
        int progressionDoorsCount = Mathf.Clamp(Random.Range(1, 4), 1, Mathf.Max(1, validDoors.Count));

        for (int i = 0; i < progressionDoorsCount; i++)
        {
            if (validDoors.Count == 0) break; // Safety check

            int randomIndex = Random.Range(0, validDoors.Count);
            // Start as LOCKED (will unlock when waves clear)
            validDoors[randomIndex].doorType = DoorStatus.LOCKED;
            validDoors[randomIndex].SetAsSpawnDoor(false); // Not spawn door
            validDoors[randomIndex].SetAsProgressionDoor(true); // THIS door unlocks after waves clear

            // targetRoom will be set by RogueLikeBuildingManager when player enters this door

            validDoors.RemoveAt(randomIndex);
        }

        // Set remaining valid doors as LOCKED and optionally deactivate them
        // These are NOT progression doors - they won't unlock after waves
        foreach (var door in validDoors)
        {
            door.doorType = DoorStatus.LOCKED;
            door.SetAsProgressionDoor(false); // NOT a progression door - stays locked
            
            // 75% chance to deactivate locked doors
            if (Random.Range(0f, 1f) < 0.75f)
            {
                door.gameObject.SetActive(false);
            }
        }

        // Deactivate all invalid doors (those without proper floors)
        foreach (var door in invalidDoors)
        {
            door.doorType = DoorStatus.LOCKED;
            door.gameObject.SetActive(false);
            Debug.Log($"[SetupDoors] Deactivated invalid door: {door.gameObject.name}");
        }
    }

    internal Vector3 GetPlayerSpawnPoint()
    {
        if (playerSpawnPoint != null)
        {
            return playerSpawnPoint.position;
        }
        
        // Fallback to center piece if no spawn point is set
        return centerPiece != null ? centerPiece.position : Vector3.zero;
    }

    public void SetupChests()
    {
        List<ChestParent> chests = new List<ChestParent>(transform.GetComponentsInChildren<ChestParent>());

        if (chests == null || chests.Count == 0)
        {
            Debug.LogWarning("No chests found in the scene.");
            return;
        }

        foreach (var chest in chests)
        {
            if (Random.value < 0.75f)
            {
                chest.gameObject.SetActive(false);
                continue;
            }

            chest.SetupChest(GameManager.Instance.DifficultyManager.GetCurrentRoomDifficulty());
        }
    }


    
    /// <summary>
    /// Get a summary of room types in this parent for debugging
    /// </summary>
    public string GetRoomTypeSummary()
    {
        int friendlyCount = 0;
        int hostileCount = 0;
        
        foreach (var kvp in placedRoomsBySpawnIndex)
        {
            var roomData = kvp.Value;
            if (roomData.roomObject != null)
            {
                RogueLiteRoom roomComponent = roomData.roomObject.GetComponent<RogueLiteRoom>();
                if (roomComponent != null)
                {
                    if (roomComponent.RoomType == RogueLikeRoomType.FRIENDLY)
                        friendlyCount++;
                    else if (roomComponent.RoomType == RogueLikeRoomType.HOSTILE)
                        hostileCount++;
                }
            }
        }
        
        return $"Parent Type: {roomType}, Rooms: {friendlyCount} friendly, {hostileCount} hostile";
    }
    
    /// <summary>
    /// Check if this is a friendly room and handle door unlocking if enemy spawning was skipped
    /// </summary>
    private void CheckAndHandleFriendlyRoomDoors()
    {
        if (roomType == RogueLikeRoomType.FRIENDLY)
        {
            // The EnemySpawnManager will handle setting ALL_WAVES_CLEARED for friendly rooms
            // No backup mechanism needed - let the normal flow handle it
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (!showDirectionArrows) return;

        // Get room transforms for gizmo drawing (works in editor)
        Transform[] gizmoRoomTransforms = GetRoomTransforms();
        if (gizmoRoomTransforms == null) return;

        Gizmos.color = arrowColor;

        for (int i = 0; i < gizmoRoomTransforms.Length; i++)
        {
            if (gizmoRoomTransforms[i] != null)
            {
                DrawDirectionArrow(gizmoRoomTransforms[i], $"Room {i}");
            }
        }
        
        // Draw placed room bounds for debugging
        if (showPlacedRoomBounds && Application.isPlaying && placedRoomsBySpawnIndex != null)
        {
            foreach (var kvp in placedRoomsBySpawnIndex)
            {
                var roomData = kvp.Value;
                if (roomData != null && roomData.roomObject != null)
                {
                    RogueLiteRoom roomComponent = roomData.roomObject.GetComponent<RogueLiteRoom>();
                    if (roomComponent != null)
                    {
                        Bounds bounds = roomComponent.GetWorldBounds();
                        
                        // Color code based on overlap status
                        if (roomData.exceededTolerance)
                        {
                            // Red/orange for rooms that exceeded tolerance
                            Gizmos.color = new Color(1f, 0.5f, 0f, 1f); // Orange
                        }
                        else if (roomData.overlapPercentage > 0)
                        {
                            // Yellow for rooms with acceptable overlap
                            Gizmos.color = Color.yellow;
                        }
                        else
                        {
                            // Green for rooms with no overlap
                            Gizmos.color = Color.green;
                        }
                        
                        Gizmos.DrawWireCube(bounds.center, bounds.size);
                        
                        #if UNITY_EDITOR
                        // Draw label with room info and overlap status
                        UnityEditor.Handles.color = Color.white;
                        string overlapInfo = roomData.overlapPercentage > 0 
                            ? $"\nOverlap: {roomData.overlapPercentage:F1}% {(roomData.exceededTolerance ? "⚠️" : "✓")}"
                            : "\nOverlap: None ✓";
                        
                        UnityEditor.Handles.Label(bounds.center + Vector3.up * (bounds.size.y * 0.5f + 1f), 
                            $"Spawn {kvp.Key}: {roomData.debugName}\nBounds: {bounds.size}{overlapInfo}");
                        #endif
                    }
                }
            }
        }
    }

    private Transform[] GetRoomTransforms()
    {
        // If we're in play mode and roomTransforms is already populated, use it
        if (Application.isPlaying && roomTransforms != null)
        {
            return roomTransforms;
        }

        // Otherwise, get from parent (for editor gizmos)
        if (roomSpawnPointsParent != null)
        {
            Transform[] transforms = new Transform[roomSpawnPointsParent.childCount];
            for (int i = 0; i < roomSpawnPointsParent.childCount; i++)
            {
                transforms[i] = roomSpawnPointsParent.GetChild(i);
            }
            return transforms;
        }

        return null;
    }

    private void DrawDirectionArrow(Transform targetTransform, string label)
    {
        Vector3 position = targetTransform.position;
        Vector3 forward = targetTransform.forward;
        
        // Draw the main arrow shaft
        Vector3 arrowEnd = position + forward * arrowLength;
        Gizmos.DrawLine(position, arrowEnd);
        
        // Draw arrow head
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;
        
        Vector3 arrowHeadRight = arrowEnd - forward * arrowHeadLength + right * arrowHeadLength * 0.5f;
        Vector3 arrowHeadLeft = arrowEnd - forward * arrowHeadLength - right * arrowHeadLength * 0.5f;
        
        Gizmos.DrawLine(arrowEnd, arrowHeadRight);
        Gizmos.DrawLine(arrowEnd, arrowHeadLeft);
        
        // Draw label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(position + Vector3.up * 0.5f, label);
        #endif
    }

    #if UNITY_EDITOR
    [UnityEditor.MenuItem("CONTEXT/RogueLiteRoomParent/Log Current Room Placement")]
    private static void LogCurrentRoomPlacement(UnityEditor.MenuCommand command)
    {
        RogueLiteRoomParent parent = (RogueLiteRoomParent)command.context;
        Debug.Log($"[RogueLiteRoomParent] === Room Placement Info for {parent.gameObject.name} ===");
        Debug.Log($"Total spawn points: {(parent.roomTransforms != null ? parent.roomTransforms.Length : 0)}");
        Debug.Log($"Placed rooms: {parent.placedRoomsBySpawnIndex.Count}");
        Debug.Log($"Max acceptable overlap: {parent.maxAcceptableOverlapVolume} cubic units");
        Debug.Log($"Min room distance: {parent.minRoomDistance}");
        
        foreach (var kvp in parent.placedRoomsBySpawnIndex)
        {
            var roomData = kvp.Value;
            if (roomData != null && roomData.roomObject != null)
            {
                RogueLiteRoom roomComponent = roomData.roomObject.GetComponent<RogueLiteRoom>();
                if (roomComponent != null)
                {
                    Bounds bounds = roomComponent.GetWorldBounds();
                    Debug.Log($"  Spawn {kvp.Key}: {roomData.debugName} at {roomData.roomObject.transform.position}, bounds center: {bounds.center}, size: {bounds.size}");
                }
            }
        }
    }
    #endif
    
    #region Context Menu Debug Tools
    
    [ContextMenu("Check All Room Overlaps")]
    private void CheckAllRoomOverlaps()
    {
        Debug.Log($"=== Checking All Room Overlaps ===");
        Debug.Log($"Total placed rooms: {placedRoomsBySpawnIndex.Count}");
        Debug.Log($"Max acceptable overlap volume: {maxAcceptableOverlapVolume}");
        
        if (placedRoomsBySpawnIndex.Count == 0)
        {
            Debug.LogWarning("No rooms have been placed yet!");
            return;
        }
        
        int overlapCount = 0;
        
        // Check each pair of rooms
        var roomList = new List<KeyValuePair<int, PlacedRoomData>>(placedRoomsBySpawnIndex);
        
        for (int i = 0; i < roomList.Count; i++)
        {
            var room1Data = roomList[i];
            if (room1Data.Value.roomObject == null) continue;
            
            RogueLiteRoom room1Component = room1Data.Value.roomObject.GetComponent<RogueLiteRoom>();
            if (room1Component == null) continue;
            
            Bounds bounds1 = room1Component.GetWorldBounds();
            
            Debug.Log($"\n[Room {i}] {room1Data.Value.debugName} at spawn {room1Data.Key}:");
            Debug.Log($"  Prefab: {room1Data.Value.originalPrefab.name}");
            Debug.Log($"  Position: {room1Data.Value.roomObject.transform.position}");
            Debug.Log($"  Bounds: center={bounds1.center}, size={bounds1.size}");
            Debug.Log($"  Bounds: min={bounds1.min}, max={bounds1.max}");
            
            for (int j = i + 1; j < roomList.Count; j++)
            {
                var room2Data = roomList[j];
                if (room2Data.Value.roomObject == null) continue;
                
                RogueLiteRoom room2Component = room2Data.Value.roomObject.GetComponent<RogueLiteRoom>();
                if (room2Component == null) continue;
                
                Bounds bounds2 = room2Component.GetWorldBounds();
                
                if (bounds1.Intersects(bounds2))
                {
                    Vector3 intersectionMin = Vector3.Max(bounds1.min, bounds2.min);
                    Vector3 intersectionMax = Vector3.Min(bounds1.max, bounds2.max);
                    
                    if (intersectionMin.x < intersectionMax.x && 
                        intersectionMin.y < intersectionMax.y && 
                        intersectionMin.z < intersectionMax.z)
                    {
                        Vector3 intersectionSize = intersectionMax - intersectionMin;
                        float intersectionVolume = intersectionSize.x * intersectionSize.y * intersectionSize.z;
                        
                        overlapCount++;
                        
                        string status = intersectionVolume <= maxAcceptableOverlapVolume ? "ACCEPTABLE" : "⚠️ EXCEEDS TOLERANCE";
                        
                        Debug.LogWarning($"  OVERLAP #{overlapCount} with {room2Data.Value.debugName} (Prefab: {room2Data.Value.originalPrefab.name}) at spawn {room2Data.Key}: {status}");
                        Debug.LogWarning($"    Intersection volume: {intersectionVolume:F1} cubic units (tolerance: {maxAcceptableOverlapVolume:F1})");
                        Debug.LogWarning($"    Intersection: min={intersectionMin}, max={intersectionMax}");
                        Debug.LogWarning($"    Intersection size: {intersectionSize}");
                    }
                }
            }
        }
        
        if (overlapCount == 0)
        {
            Debug.Log($"\n✓ No overlaps detected!");
        }
        else
        {
            Debug.LogWarning($"\n⚠️ Total overlaps found: {overlapCount}");
        }
    }
    
    [ContextMenu("Log Current Room Placement")]
    private void LogCurrentRoomPlacement()
    {
        Debug.Log($"=== Current Room Placement ===");
        Debug.Log($"Placed Rooms: {placedRoomsBySpawnIndex.Count}");
        
        if (placedRoomsBySpawnIndex.Count == 0)
        {
            Debug.LogWarning("No rooms have been placed yet!");
            return;
        }
        
        foreach (var kvp in placedRoomsBySpawnIndex)
        {
            var roomData = kvp.Value;
            if (roomData.roomObject != null)
            {
                RogueLiteRoom roomComp = roomData.roomObject.GetComponent<RogueLiteRoom>();
                if (roomComp != null)
                {
                    Bounds bounds = roomComp.GetWorldBounds();
                    Debug.Log($"  Spawn {kvp.Key}: {roomData.debugName}");
                    Debug.Log($"    Prefab: {roomData.originalPrefab.name}");
                    Debug.Log($"    Position: {roomData.roomObject.transform.position}");
                    Debug.Log($"    Bounds: center={bounds.center}, size={bounds.size}");
                }
            }
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Shuffle a list using Fisher-Yates algorithm
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
    
    #endregion
}


