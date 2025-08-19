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

        int currentDifficulty = DifficultyManager.Instance.GetCurrentWaveDifficulty();

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
        // Use the new CalculateTestBounds method that doesn't modify the original prefab
        Bounds testBounds = roomToTest.CalculateTestBounds(position);
        
        // Check against all spawned rooms
        foreach (var spawnedRoom in spawnedRooms.Values)
        {
            if (spawnedRoom == null) continue;
            
            RogueLiteRoom spawnedRoomComponent = spawnedRoom.GetComponent<RogueLiteRoom>();
            if (spawnedRoomComponent == null) continue;
            
            Bounds spawnedBounds = spawnedRoomComponent.GetWorldBounds();
            
            // Check if rooms are too close (minimum distance check)
            float centerDistance = Vector3.Distance(testBounds.center, spawnedBounds.center);
            if (centerDistance < minRoomDistance)
            {
                if (showCollisionDebug)
                    Debug.Log($"[RoomCollision] Rooms too close: {centerDistance} < {minRoomDistance}");
                return true;
            }
            
            // Check for excessive overlap using volume calculation
            if (testBounds.Intersects(spawnedBounds))
            {
                float overlapPercentage = CalculateOverlapPercentage(testBounds, spawnedBounds);
                if (overlapPercentage > overlapTolerancePercent)
                {
                    if (showCollisionDebug)
                        Debug.Log($"[RoomCollision] Excessive overlap: {overlapPercentage:F1}% > {overlapTolerancePercent}%");
                    return true; // Excessive overlap detected
                }
                else if (showCollisionDebug)
                {
                    Debug.Log($"[RoomCollision] Acceptable overlap: {overlapPercentage:F1}% <= {overlapTolerancePercent}%");
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
    /// GUARANTEES that every spawn point gets a room
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

        // Add loop protection
        int maxIterations = availableSpawnPoints.Count * 2; // Safety limit
        int iterations = 0;
        
        while (availableSpawnPoints.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            
            int spawnIndex = availableSpawnPoints[0];
            GameObject roomPrefab = buildingScriptableObj.GetBuildingRoom(currentDifficulty);
            
            if (GuaranteedRoomPlacement(roomTransforms[spawnIndex], roomPrefab, buildingScriptableObj, currentDifficulty, $"Room {spawnIndex}"))
            {
                availableSpawnPoints.RemoveAt(0);
            }
            else
            {
                // Force placement with fallback to prevent infinite loop
                if (ForcePlaceAnyRoom(spawnIndex, buildingScriptableObj, currentDifficulty, $"Forced Room {spawnIndex}"))
                {
                    availableSpawnPoints.RemoveAt(0);
                }
                else
                {
                    Debug.LogError($"[HierarchicalPlacement] Cannot place any room at spawn {spawnIndex}!");
                    availableSpawnPoints.RemoveAt(0); // Remove to prevent infinite loop
                }
            }
        }
        
        // Check if we hit the safety limit
        if (iterations >= maxIterations)
        {
            Debug.LogError($"[HierarchicalPlacement] Hit safety limit ({maxIterations} iterations). Forcing remaining {availableSpawnPoints.Count} rooms.");
            
            // Force place remaining rooms without collision checking
            foreach (int spawnIndex in availableSpawnPoints)
            {
                ForcePlaceAnyRoom(spawnIndex, buildingScriptableObj, currentDifficulty, $"Safety Forced Room {spawnIndex}");
            }
        }

        // Verify all spawn points have rooms
        if (placedRoomsBySpawnIndex.Count != roomTransforms.Length)
        {
            Debug.LogError($"[HierarchicalPlacement] FAILED TO PLACE ALL ROOMS! Expected {roomTransforms.Length}, got {placedRoomsBySpawnIndex.Count}");
            
            // Log which spawn points are missing rooms
            for (int i = 0; i < roomTransforms.Length; i++)
            {
                if (!placedRoomsBySpawnIndex.ContainsKey(i))
                {
                    Debug.LogError($"[HierarchicalPlacement] Missing room at spawn point {i}");
                }
            }
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

        // Track the placed room data
        var roomData = new PlacedRoomData(room, roomPrefab, spawnIndex, debugName);
        placedRoomsBySpawnIndex[spawnIndex] = roomData;

        RogueLiteRoom roomComponent = room.GetComponent<RogueLiteRoom>();
        roomComponent.Setup();
        RandomizePropsInSection(room.transform);
        
        // Update the parent's room type based on placed rooms
        UpdateParentRoomType();
        
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

        // Assign a random door as the EXIT (entrance from previous room)
        int exitIndex = Random.Range(0, allDoors.Count);
        playerSpawnPoint = allDoors[exitIndex].playerSpawn;
        allDoors[exitIndex].doorType = DoorStatus.EXIT;

        // Store the exit door for potential connections
        RogueLikeRoomDoor exitDoor = allDoors[exitIndex];
        allDoors.RemoveAt(exitIndex);

        // Select 1-3 doors to be ENTRANCE doors (from remaining doors)
        int entranceDoorsCount = Mathf.Clamp(Random.Range(1, 4), 1, allDoors.Count);

        for (int i = 0; i < entranceDoorsCount; i++)
        {
            int randomIndex = Random.Range(0, allDoors.Count);
            allDoors[randomIndex].doorType = DoorStatus.ENTRANCE;
            
            // Connect this entrance door to the exit door
            if (exitDoor != null)
            {
                // Forward connection: entrance door -> previous room
                allDoors[randomIndex].targetRoom = exitDoor.GetComponentInParent<RogueLiteRoomParent>();
                allDoors[randomIndex].targetSpawnPoint = exitDoor.playerSpawn;

                // Backward connection: exit door -> this room
                exitDoor.targetRoom = this;
                exitDoor.targetSpawnPoint = allDoors[randomIndex].playerSpawn;
            }

            allDoors.RemoveAt(randomIndex);
        }

        // Set remaining doors as LOCKED and optionally deactivate them
        foreach (var door in allDoors)
        {
            door.doorType = DoorStatus.LOCKED;
            
            // 75% chance to deactivate locked doors
            if (Random.Range(0f, 1f) < 0.75f)
            {
                door.gameObject.SetActive(false);
            }
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

            chest.SetupChest(DifficultyManager.Instance.GetCurrentRoomDifficulty());
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
}

