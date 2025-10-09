using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
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
    
    [Header("Room Collision Settings")]
    [SerializeField] private float overlapTolerancePercent = 15f; // Allow up to 15% overlap for connections
    [SerializeField] private float minRoomDistance = 5f; // Minimum distance between room centers
    [SerializeField] private bool showCollisionDebug = false;
    [Tooltip("If true, shows all placed rooms' bounds in Scene view")]
    [SerializeField] private bool showPlacedRoomBounds = true;
    
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
        
        public PlacedRoomData(GameObject room, GameObject prefab, int spawnIdx, string name)
        {
            roomObject = room;
            originalPrefab = prefab;
            spawnIndex = spawnIdx;
            debugName = name;
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

    public void GenerateRandomRooms(RogueLikeBuildingDataScriptableObj buildingScriptableObj)
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

        if (navMeshSurface == null)
        {
            navMeshSurface = FindAnyObjectByType<NavMeshSurface>();
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

    private bool WouldRoomOverlapAtPosition(RogueLiteRoom roomToTest, Vector3 position)
    {
        if (roomToTest == null)
        {
            Debug.LogError("[WouldRoomOverlapAtPosition] roomToTest is null!");
            return true; // Fail safe - consider it as overlapping
        }

        // Use the new CalculateTestBounds method that doesn't modify the original prefab
        Bounds testBounds = roomToTest.CalculateTestBounds(position);
        
        if (showCollisionDebug)
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
            
            if (showCollisionDebug)
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
                if (showCollisionDebug)
                    Debug.Log($"[RoomCollision] Rooms too close: {centerDistance:F2} < {dynamicMinDistance:F2} (dynamic min based on room sizes)");
                return true;
            }
            
            // Check for excessive overlap using volume calculation
            if (testBounds.Intersects(existingBounds))
            {
                float overlapPercentage = CalculateOverlapPercentage(testBounds, existingBounds);
                if (showCollisionDebug)
                {
                    Debug.Log($"[RoomCollision] Overlap detected: {overlapPercentage:F1}% (tolerance: {overlapTolerancePercent}%)");
                }
                
                if (overlapPercentage > overlapTolerancePercent)
                {
                    if (showCollisionDebug)
                        Debug.Log($"[RoomCollision] EXCESSIVE overlap: {overlapPercentage:F1}% > {overlapTolerancePercent}%");
                    return true; // Excessive overlap detected
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

    /// <summary>
    /// Intelligently place rooms at all spawn points
    /// Prioritizes collision-free placement over filling every spawn point
    /// </summary>
    private void HierarchicalRoomPlacement(RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty)
    {
        if (roomTransforms == null || roomTransforms.Length == 0)
        {
            Debug.LogError("[RogueLiteRoomParent] No room transforms available for placement");
            return;
        }

        List<int> availableSpawnPoints = new List<int>();
        for (int i = 0; i < roomTransforms.Length; i++)
        {
            availableSpawnPoints.Add(i);
        }

        // Track which spawn points we've attempted and failed
        List<int> failedSpawnPoints = new List<int>();
        
        // Add loop protection
        int maxIterations = availableSpawnPoints.Count * 3; // Increased to allow more retries
        int iterations = 0;
        
        while (availableSpawnPoints.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            int spawnIndex = availableSpawnPoints[0];
            GameObject roomPrefab = buildingScriptableObj.GetBuildingRoom(currentDifficulty);
            
            if (GuaranteedRoomPlacement(roomTransforms[spawnIndex], roomPrefab, buildingScriptableObj, currentDifficulty, $"Room {spawnIndex}"))
            {
                availableSpawnPoints.RemoveAt(0);
                if (showCollisionDebug)
                    Debug.Log($"[HierarchicalPlacement] Successfully placed room at spawn {spawnIndex}");
            }
            else
            {
                // Try to find ANY room that fits without excessive overlap
                bool placed = TryPlaceAnyValidRoom(spawnIndex, buildingScriptableObj, currentDifficulty);
                
                if (placed)
                {
                    availableSpawnPoints.RemoveAt(0);
                    if (showCollisionDebug)
                        Debug.Log($"[HierarchicalPlacement] Placed valid room at spawn {spawnIndex} after retry");
                }
                else
                {
                    // Mark as failed and move to end of queue
                    availableSpawnPoints.RemoveAt(0);
                    
                    if (!failedSpawnPoints.Contains(spawnIndex))
                    {
                        // First failure - try again later
                        availableSpawnPoints.Add(spawnIndex);
                        failedSpawnPoints.Add(spawnIndex);
                        if (showCollisionDebug)
                            Debug.Log($"[HierarchicalPlacement] Moving spawn {spawnIndex} to end of queue for retry");
                    }
                    else
                    {
                        // Already failed once - skip this spawn point to avoid excessive overlap
                        Debug.LogWarning($"[HierarchicalPlacement] Skipping spawn {spawnIndex} - no valid room fits without excessive overlap");
                    }
                }
            }
        }
        
        // Check if we hit the safety limit
        if (iterations >= maxIterations)
        {
            Debug.LogWarning($"[HierarchicalPlacement] Hit safety limit ({maxIterations} iterations) with {availableSpawnPoints.Count} remaining spawn points");
        }

        // Final report
        int placedCount = placedRoomsBySpawnIndex.Count;
        int totalSpawns = roomTransforms.Length;
        
        if (placedCount < totalSpawns)
        {
            Debug.LogWarning($"[HierarchicalPlacement] Placed {placedCount}/{totalSpawns} rooms. " +
                           $"Skipped {totalSpawns - placedCount} spawn points to prevent excessive overlap.");
            
            // Log which spawn points were skipped
            for (int i = 0; i < roomTransforms.Length; i++)
            {
                if (!placedRoomsBySpawnIndex.ContainsKey(i))
                {
                    Debug.Log($"[HierarchicalPlacement] Skipped spawn point {i} at position {roomTransforms[i].position}");
                }
            }
        }
        else
        {
            Debug.Log($"[HierarchicalPlacement] Successfully placed all {placedCount} rooms without excessive overlap");
        }
    }

    /// <summary>
    /// Intelligently place rooms using constraint satisfaction with room swapping
    /// Tries to resolve conflicts by swapping blocking rooms with smaller alternatives
    /// </summary>
    private bool GuaranteedRoomPlacement(Transform targetTransform, GameObject preferredRoomPrefab, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, string debugName)
    {
        int spawnIndex = GetSpawnIndex(targetTransform);
        if (spawnIndex == -1)
        {
            Debug.LogError($"[SmartPlacement] Could not find spawn index for {targetTransform.name}");
            return false;
        }

        return SmartRoomPlacement(spawnIndex, preferredRoomPrefab, buildingScriptableObj, currentDifficulty, debugName, 0);
    }

    /// <summary>
    /// Smart room placement with conflict resolution through room swapping and minimum overlap selection
    /// </summary>
    private bool SmartRoomPlacement(int spawnIndex, GameObject preferredRoomPrefab, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, string debugName, int retryCount)
    {
        if (retryCount >= maxSwapRetries)
        {
            // Use minimum overlap placement after max retries
            return ForcePlaceAnyRoom(spawnIndex, buildingScriptableObj, currentDifficulty, debugName + " (Min Overlap After Retries)");
        }

        Transform targetTransform = roomTransforms[spawnIndex];

        // Strategy 1: Try the preferred room first
        if (preferredRoomPrefab != null)
        {
            if (TryPlaceWithConflictResolution(spawnIndex, preferredRoomPrefab, buildingScriptableObj, currentDifficulty, debugName + " (Preferred)", retryCount))
                return true;
        }

        // Strategy 2: Try room with minimum overlap directly
        GameObject bestRoom = FindRoomWithMinimumOverlap(spawnIndex, buildingScriptableObj, currentDifficulty, out float minOverlap);
        if (bestRoom != null && minOverlap <= overlapTolerancePercent)
        {
            if (TryPlaceWithConflictResolution(spawnIndex, bestRoom, buildingScriptableObj, currentDifficulty, debugName + $" (Min Overlap {minOverlap:F1}%)", retryCount))
                return true;
        }

        // Strategy 3: Try any available room
        GameObject anyRoom = buildingScriptableObj.GetBuildingRoom(currentDifficulty);
        if (anyRoom != null)
        {
            if (TryPlaceWithConflictResolution(spawnIndex, anyRoom, buildingScriptableObj, currentDifficulty, debugName + " (Any)", retryCount))
                return true;
        }

        // Final fallback: Use minimum overlap placement
        return ForcePlaceAnyRoom(spawnIndex, buildingScriptableObj, currentDifficulty, debugName + " (Final Min Overlap)");
    }

    /// <summary>
    /// Get the spawn index for a given transform
    /// </summary>
    private int GetSpawnIndex(Transform targetTransform)
    {
        for (int i = 0; i < roomTransforms.Length; i++)
        {
            if (roomTransforms[i] == targetTransform)
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
        Transform targetTransform = roomTransforms[spawnIndex];
        
        // Check if room would fit without conflicts
        if (!WouldRoomOverlapAtPosition(roomPrefab.GetComponent<RogueLiteRoom>(), targetTransform.position))
        {
            // No conflicts - place the room directly
            return PlaceRoomAtSpawn(spawnIndex, roomPrefab, debugName);
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
                if (!WouldRoomOverlapAtPosition(roomPrefab.GetComponent<RogueLiteRoom>(), targetTransform.position))
                {
                    return PlaceRoomAtSpawn(spawnIndex, roomPrefab, debugName + " (After Swap)");
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Try to place ANY room from the available pool that fits within overlap tolerance
    /// Will NOT place rooms that exceed tolerance
    /// </summary>
    private bool TryPlaceAnyValidRoom(int spawnIndex, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty)
    {
        Transform targetTransform = roomTransforms[spawnIndex];
        GameObject[] allRooms = buildingScriptableObj.GetAllRooms(currentDifficulty);
        
        if (allRooms == null || allRooms.Length == 0)
        {
            Debug.LogError($"[TryPlaceAnyValidRoom] No rooms available for difficulty {currentDifficulty}");
            return false;
        }

        // Shuffle rooms to try different options
        List<GameObject> shuffledRooms = new List<GameObject>(allRooms);
        for (int i = 0; i < shuffledRooms.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffledRooms.Count);
            GameObject temp = shuffledRooms[i];
            shuffledRooms[i] = shuffledRooms[randomIndex];
            shuffledRooms[randomIndex] = temp;
        }

        // Try each room, but ONLY place if it fits within tolerance
        foreach (GameObject roomPrefab in shuffledRooms)
        {
            if (roomPrefab == null) continue;
            
            RogueLiteRoom roomComponent = roomPrefab.GetComponent<RogueLiteRoom>();
            if (roomComponent == null) continue;

            // Check if room fits WITHOUT exceeding tolerance
            float maxOverlap = CalculateMaxOverlapForRoom(roomPrefab, targetTransform.position);
            
            if (maxOverlap <= overlapTolerancePercent)
            {
                // This room fits! Place it
                if (PlaceRoomAtSpawn(spawnIndex, roomPrefab, $"Valid Room (Overlap: {maxOverlap:F1}%)"))
                {
                    if (showCollisionDebug)
                        Debug.Log($"[TryPlaceAnyValidRoom] Placed room at spawn {spawnIndex} with {maxOverlap:F1}% overlap");
                    return true;
                }
            }
            else if (showCollisionDebug)
            {
                Debug.Log($"[TryPlaceAnyValidRoom] Room {roomPrefab.name} rejected at spawn {spawnIndex} - {maxOverlap:F1}% overlap exceeds {overlapTolerancePercent}% tolerance");
            }
        }

        if (showCollisionDebug)
            Debug.LogWarning($"[TryPlaceAnyValidRoom] No valid room found for spawn {spawnIndex} - all options exceed overlap tolerance");
        
        return false;
    }

    /// <summary>
    /// Smart room placement that finds the room with minimum overlap
    /// Priority: 1) Rooms within tolerance, 2) Room with smallest overlap, 3) Force placement
    /// </summary>
    private bool ForcePlaceAnyRoom(int spawnIndex, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, string debugName)
    {
        Transform targetTransform = roomTransforms[spawnIndex];
        
        // First try: Find room with minimum overlap
        GameObject bestRoom = FindRoomWithMinimumOverlap(spawnIndex, buildingScriptableObj, currentDifficulty, out float minOverlap);
        
        if (bestRoom != null)
        {
            if (minOverlap <= overlapTolerancePercent)
            {
                // Room fits within tolerance - place it normally
                if (PlaceRoomAtSpawn(spawnIndex, bestRoom, debugName + $" (Min Overlap {minOverlap:F1}%)"))
                {
                    return true;
                }
            }
            else
            {
                // Room exceeds tolerance but is the best option available
                Debug.LogWarning($"[SmartPlacement] Placing room with {minOverlap:F1}% overlap (exceeds {overlapTolerancePercent}% tolerance) at spawn {spawnIndex}");
                if (PlaceRoomAtSpawn(spawnIndex, bestRoom, debugName + $" (Best Option {minOverlap:F1}%)"))
                {
                    return true;
                }
            }
        }
        
        // Absolute last resort: try any room without collision checking
        GameObject anyRoom = buildingScriptableObj.GetBuildingRoom(currentDifficulty);
        if (anyRoom != null)
        {
            if (PlaceRoomAtSpawn(spawnIndex, anyRoom, debugName + " (Force Any)"))
            {
                Debug.LogWarning($"[ForcePlacement] Used absolute force placement for {debugName} at spawn {spawnIndex}");
                return true;
            }
        }
        
        Debug.LogError($"[ForcePlacement] Failed to place any room at spawn {spawnIndex}");
        return false;
    }

    /// <summary>
    /// Find the room that produces the minimum overlap at a given spawn point
    /// </summary>
    private GameObject FindRoomWithMinimumOverlap(int spawnIndex, RogueLikeBuildingDataScriptableObj buildingScriptableObj, int currentDifficulty, out float minOverlap)
    {
        Transform targetTransform = roomTransforms[spawnIndex];
        
        GameObject bestRoom = null;
        minOverlap = float.MaxValue;
        
        // Test all available rooms
        GameObject[] allRooms = buildingScriptableObj.GetAllRooms(currentDifficulty);
        
        foreach (var roomPrefab in allRooms)
        {
            if (roomPrefab == null) continue;
            
            float maxOverlap = CalculateMaxOverlapForRoom(roomPrefab, targetTransform.position);
            
            if (maxOverlap < minOverlap)
            {
                minOverlap = maxOverlap;
                bestRoom = roomPrefab;
            }
        }
        
        return bestRoom;
    }

    /// <summary>
    /// Calculate the maximum overlap percentage this room would have with existing rooms
    /// </summary>
    private float CalculateMaxOverlapForRoom(GameObject roomPrefab, Vector3 position)
    {
        RogueLiteRoom testRoom = roomPrefab.GetComponent<RogueLiteRoom>();
        if (testRoom == null) return float.MaxValue;

        // Use the new CalculateTestBounds method that doesn't modify the original prefab
        Bounds testBounds = testRoom.CalculateTestBounds(position);

        float maxOverlap = 0f;

        // Check against all placed rooms
        foreach (var kvp in placedRoomsBySpawnIndex)
        {
            var roomData = kvp.Value;
            if (roomData.roomObject == null) continue;
            
            RogueLiteRoom existingRoom = roomData.roomObject.GetComponent<RogueLiteRoom>();
            if (existingRoom == null) continue;
            
            Bounds existingBounds = existingRoom.GetWorldBounds();
            
            // Check for overlap
            if (testBounds.Intersects(existingBounds))
            {
                float overlapPercentage = CalculateOverlapPercentage(testBounds, existingBounds);
                maxOverlap = Mathf.Max(maxOverlap, overlapPercentage);
            }
            
            // Also check minimum distance requirement
            float centerDistance = Vector3.Distance(testBounds.center, existingBounds.center);
            if (centerDistance < minRoomDistance)
            {
                // Penalize rooms that are too close by adding extra "virtual overlap"
                float distancePenalty = (minRoomDistance - centerDistance) / minRoomDistance * 50f; // Convert to percentage
                maxOverlap = Mathf.Max(maxOverlap, distancePenalty);
            }
        }

        return maxOverlap;
    }

    /// <summary>
    /// Place a room at a specific spawn index and track it properly
    /// </summary>
    private bool PlaceRoomAtSpawn(int spawnIndex, GameObject roomPrefab, string debugName)
    {
        if (!roomPrefab.GetComponent<RogueLiteRoom>())
        {
            Debug.LogError($"{roomPrefab.name} has no RogueLiteRoom Component");
            return false;
        }

        Transform targetTransform = roomTransforms[spawnIndex];
        
        // Remove existing room at this spawn if any
        if (placedRoomsBySpawnIndex.ContainsKey(spawnIndex))
        {
            var existingRoom = placedRoomsBySpawnIndex[spawnIndex];
            if (existingRoom.roomObject != null)
            {
                spawnedRooms.Remove(targetTransform.position);
                Destroy(existingRoom.roomObject);
            }
            placedRoomsBySpawnIndex.Remove(spawnIndex);
        }

        // Place the new room
        GameObject room = Instantiate(roomPrefab, targetTransform.position, targetTransform.rotation, targetTransform);
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
        
        // Track the placed room data
        var roomData = new PlacedRoomData(room, roomPrefab, spawnIndex, debugName);
        placedRoomsBySpawnIndex[spawnIndex] = roomData;

        // Setup the room (doors, chests, etc.)
        roomComponent.Setup();
        RandomizePropsInSection(room.transform);
        
        // Update the parent's room type based on placed rooms
        UpdateParentRoomType();
        
        if (showCollisionDebug)
        {
            Bounds placedBounds = roomComponent.GetWorldBounds();
            Debug.Log($"[PlaceRoomAtSpawn] Successfully placed {debugName} at spawn {spawnIndex}, " +
                     $"position: {targetTransform.position}, bounds center: {placedBounds.center}, size: {placedBounds.size}");
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
                float overlapPercentage = CalculateOverlapPercentage(testBounds, existingBounds);
                if (overlapPercentage > overlapTolerancePercent)
                {
                    conflictingSpawns.Add(spawnIndex);
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

        // Try to find a different room that fits better
        GameObject[] allRooms = buildingScriptableObj.GetAllRooms(currentDifficulty);
        
        foreach (var alternativeRoom in allRooms)
        {
            if (alternativeRoom == null) continue;
            
            // Test if the alternative room would work at this position
            Transform targetTransform = roomTransforms[spawnIndex];
            if (!WouldRoomOverlapAtPosition(alternativeRoom.GetComponent<RogueLiteRoom>(), targetTransform.position))
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
        if (checkCollisions && WouldRoomOverlapAtPosition(roomPrefab.GetComponent<RogueLiteRoom>(), targetTransform.position))
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

    private IEnumerator DelayedBakeNavMesh()
    {
        yield return new WaitForSeconds(0.1f);

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

        // If no valid doors found, fall back to using all doors (better than having no exits)
        if (validDoors.Count == 0)
        {
            Debug.LogWarning("[RogueLiteRoomParent] No doors with valid floors found! Using all doors as fallback.");
            validDoors = allDoors;
            invalidDoors.Clear();
        }

        // Assign a random door from valid doors as the EXIT (entrance from previous room)
        int exitIndex = Random.Range(0, validDoors.Count);
        playerSpawnPoint = validDoors[exitIndex].playerSpawn;
        validDoors[exitIndex].doorType = DoorStatus.EXIT;

        // Store the exit door for potential connections
        RogueLikeRoomDoor exitDoor = validDoors[exitIndex];
        validDoors.RemoveAt(exitIndex);

        // Select 1-3 doors to be ENTRANCE doors (from remaining valid doors)
        int entranceDoorsCount = Mathf.Clamp(Random.Range(1, 4), 1, validDoors.Count);

        for (int i = 0; i < entranceDoorsCount; i++)
        {
            if (validDoors.Count == 0) break; // Safety check

            int randomIndex = Random.Range(0, validDoors.Count);
            validDoors[randomIndex].doorType = DoorStatus.ENTRANCE;
            
            // Connect this entrance door to the exit door
            if (exitDoor != null)
            {
                // Forward connection: entrance door -> previous room
                validDoors[randomIndex].targetRoom = exitDoor.GetComponentInParent<RogueLiteRoomParent>();
                validDoors[randomIndex].targetSpawnPoint = exitDoor.playerSpawn;

                // Backward connection: exit door -> this room
                exitDoor.targetRoom = this;
                exitDoor.targetSpawnPoint = validDoors[randomIndex].playerSpawn;
            }

            validDoors.RemoveAt(randomIndex);
        }

        // Set remaining valid doors as LOCKED and optionally deactivate them
        foreach (var door in validDoors)
        {
            door.doorType = DoorStatus.LOCKED;
            
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

    private void OnDrawGizmos()
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
                        
                        // Draw wireframe in magenta for easy visibility
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawWireCube(bounds.center, bounds.size);
                        
                        #if UNITY_EDITOR
                        // Draw label with room info
                        UnityEditor.Handles.color = Color.white;
                        UnityEditor.Handles.Label(bounds.center + Vector3.up * (bounds.size.y * 0.5f + 1f), 
                            $"Spawn {kvp.Key}: {roomData.debugName}\nBounds: {bounds.size}");
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
    [UnityEditor.MenuItem("CONTEXT/RogueLiteRoomParent/Toggle Collision Debug")]
    private static void ToggleCollisionDebug(UnityEditor.MenuCommand command)
    {
        RogueLiteRoomParent parent = (RogueLiteRoomParent)command.context;
        parent.showCollisionDebug = !parent.showCollisionDebug;
        Debug.Log($"[RogueLiteRoomParent] Collision debug {(parent.showCollisionDebug ? "enabled" : "disabled")} for {parent.gameObject.name}");
    }

    [UnityEditor.MenuItem("CONTEXT/RogueLiteRoomParent/Log Current Room Placement")]
    private static void LogCurrentRoomPlacement(UnityEditor.MenuCommand command)
    {
        RogueLiteRoomParent parent = (RogueLiteRoomParent)command.context;
        Debug.Log($"[RogueLiteRoomParent] === Room Placement Info for {parent.gameObject.name} ===");
        Debug.Log($"Total spawn points: {(parent.roomTransforms != null ? parent.roomTransforms.Length : 0)}");
        Debug.Log($"Placed rooms: {parent.placedRoomsBySpawnIndex.Count}");
        Debug.Log($"Overlap tolerance: {parent.overlapTolerancePercent}%");
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
}


