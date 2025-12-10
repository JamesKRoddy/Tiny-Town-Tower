using System.Collections.Generic;
using UnityEngine;
using Managers;

/// <summary>
/// Specialized room parent for boss encounters.
/// Inherits from RogueLiteRoomParent to maintain the same room generation system,
/// but adds boss-specific functionality like dedicated boss spawn points and boss spawning logic.
/// 
/// STRUCTURE:
/// - BossRoomParent is a complete building section (just like regular RogueLiteRoomParent)
/// - It generates rooms using the same hierarchical placement system
/// - After rooms are generated, it spawns the boss at a designated spawn point
/// - Boss rooms can be complex multi-room arenas designed specifically for boss fights
/// 
/// SETUP:
/// 1. Create a Building Parent prefab with BossRoomParent component (instead of RogueLiteRoomParent)
/// 2. Set up room spawn points as normal (RoomSpawnPoints parent with spawn point children)
/// 3. Add a "BossSpawnPoint" transform as a child of the prefab (where the boss will spawn)
/// 4. Create RoomParentDataScriptableObj with room configuration
///    - Assign this BossRoomParent prefab (which has spawn points configured)
///    - Add room prefabs that can spawn in the arena
///    - (Room count is determined by spawn points in the prefab)
/// 5. Assign RoomParentDataScriptableObj to BossScriptableObj.bossRoomData
/// 
/// BOSS SPAWNING:
/// - The boss is spawned AFTER all rooms are generated
/// - Boss spawns at the "BossSpawnPoint" transform (if found)
/// - Falls back to center piece position if no spawn point is defined
/// - Boss reference is set during initialization from the building generation system
/// </summary>
public class BossRoomParent : RogueLiteRoomParent
{
    [Header("Boss Room Settings")]
    [Tooltip("Transform where the boss enemy will spawn. If not set, boss spawns at center piece.")]
    [SerializeField] private Transform bossSpawnPoint;
    
    [Tooltip("Delay in seconds before spawning the boss after room generation")]
    [SerializeField] private float bossSpawnDelay = 2f;
    
    [Tooltip("If true, plays a cinematic camera effect when boss spawns")]
    [SerializeField] private bool useBossIntroCamera = true;
    
    // Boss reference set by the building generation system
    private BossScriptableObj assignedBoss = null;
    private GameObject spawnedBossInstance = null;
    
    /// <summary>
    /// Set the boss that will spawn in this room
    /// Called by the building generation system before GenerateRandomRooms
    /// </summary>
    public void SetBoss(BossScriptableObj boss)
    {
        assignedBoss = boss;
        Debug.Log($"[BossRoomParent] Boss assigned: {(boss != null ? boss.bossName : "None")}");
    }
    
    /// <summary>
    /// Generate the boss arena using custom room data
    /// This is the preferred method for boss rooms
    /// </summary>
    public void GenerateBossArena(RoomParentDataScriptableObj roomData, int currentDifficulty)
    {
        if (roomData == null)
        {
            Debug.LogError("[BossRoomParent] Room data is null!");
            return;
        }
        
        if (!roomData.IsValid())
        {
            Debug.LogError("[BossRoomParent] Room data is invalid!");
            return;
        }
        
        // Use a temporary wrapper to pass room data through the base generation system
        RoomDataWrapper wrapper = ScriptableObject.CreateInstance<RoomDataWrapper>();
        wrapper.Initialize(roomData);
        
        // Generate rooms using the base system
        base.GenerateRandomRooms(wrapper);
        
        // Spawn the boss after rooms are generated
        if (assignedBoss != null)
        {
            // Delay boss spawn slightly to ensure NavMesh is baked
            Invoke(nameof(SpawnBoss), bossSpawnDelay);
        }
        else
        {
            Debug.LogWarning("[BossRoomParent] No boss assigned to this boss room! Call SetBoss() before GenerateBossArena()");
        }
        
        // Clean up temporary wrapper
        Destroy(wrapper);
    }
    
    /// <summary>
    /// Override to add boss spawning after room generation
    /// This method is called when using the base GenerateRandomRooms with RogueLikeBuildingDataScriptableObj
    /// For boss rooms, prefer using GenerateBossArena() instead
    /// </summary>
    public override void GenerateRandomRooms(RogueLikeBuildingDataScriptableObj buildingScriptableObj)
    {
        // Call base implementation to generate rooms normally
        base.GenerateRandomRooms(buildingScriptableObj);
        
        // Spawn the boss after rooms are generated
        if (assignedBoss != null)
        {
            // Delay boss spawn slightly to ensure NavMesh is baked
            Invoke(nameof(SpawnBoss), bossSpawnDelay);
        }
        else
        {
            Debug.LogWarning("[BossRoomParent] No boss assigned to this boss room! Call SetBoss() before GenerateRandomRooms()");
        }
    }
    
    /// <summary>
    /// Spawn the boss enemy in the arena
    /// </summary>
    private void SpawnBoss()
    {
        if (assignedBoss == null)
        {
            Debug.LogError("[BossRoomParent] Cannot spawn boss - no boss assigned!");
            return;
        }
        
        if (assignedBoss.bossPrefab == null)
        {
            Debug.LogError($"[BossRoomParent] Cannot spawn boss - boss prefab is null for {assignedBoss.bossName}!");
            return;
        }
        
        // Determine spawn position and rotation
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        
        if (bossSpawnPoint != null)
        {
            spawnPosition = bossSpawnPoint.position;
            spawnRotation = bossSpawnPoint.rotation;
            Debug.Log($"[BossRoomParent] Spawning boss at designated spawn point: {spawnPosition}");
        }
        else if (centerPiece != null)
        {
            // Fallback to center piece with slight offset upward
            spawnPosition = centerPiece.position + Vector3.up * 0.5f;
            spawnRotation = Quaternion.identity;
            Debug.LogWarning($"[BossRoomParent] No boss spawn point found, using center piece: {spawnPosition}");
        }
        else
        {
            // Last resort fallback
            spawnPosition = transform.position;
            spawnRotation = Quaternion.identity;
            Debug.LogWarning($"[BossRoomParent] No boss spawn point or center piece found, using room parent position: {spawnPosition}");
        }
        
        // Instantiate the boss
        spawnedBossInstance = Instantiate(assignedBoss.bossPrefab, spawnPosition, spawnRotation, transform);
        
        Debug.Log($"[BossRoomParent] ✓ Successfully spawned boss '{assignedBoss.bossName}' at position {spawnPosition}");
        
        // Optional: Play boss intro camera/effects
        if (useBossIntroCamera)
        {
            PlayBossIntroduction();
        }
        
        // Optional: Lock entrance doors until boss is defeated
        LockEntranceDoors();
    }
    
    /// <summary>
    /// Play a cinematic introduction for the boss (optional feature)
    /// Override this in derived classes or implement via events
    /// </summary>
    protected virtual void PlayBossIntroduction()
    {
        Debug.Log($"[BossRoomParent] Playing boss introduction for {assignedBoss.bossName}");
        // TODO: Implement camera shake, zoom, boss roar animation, etc.
        // Could hook into a CameraController or CinematicManager here
    }
    
    /// <summary>
    /// Lock entrance doors so player can't leave during boss fight
    /// Doors will unlock when boss is defeated (handled by enemy death events)
    /// </summary>
    private void LockEntranceDoors()
    {
        var doors = GetComponentsInChildren<RogueLikeRoomDoor>();
        foreach (var door in doors)
        {
            if (door.doorType == DoorStatus.ENTRANCE)
            {
                // Set door to locked status - prevents player from leaving during boss fight
                door.doorType = DoorStatus.LOCKED;
                // TODO: Add visual locked effect (chains, barrier, etc.)
                Debug.Log($"[BossRoomParent] Locked entrance door for boss fight");
            }
        }
    }
    
    /// <summary>
    /// Get the spawned boss instance (for tracking, health bars, etc.)
    /// </summary>
    public GameObject GetSpawnedBoss()
    {
        return spawnedBossInstance;
    }
    
    /// <summary>
    /// Get the assigned boss data
    /// </summary>
    public BossScriptableObj GetAssignedBoss()
    {
        return assignedBoss;
    }
    
    /// <summary>
    /// Check if the boss has been defeated
    /// Useful for unlocking doors or triggering victory events
    /// </summary>
    public bool IsBossDefeated()
    {
        return spawnedBossInstance == null || !spawnedBossInstance.activeInHierarchy;
    }
    
#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        // Call base gizmos
        base.OnDrawGizmos();
        
        // Draw boss spawn point indicator
        if (bossSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bossSpawnPoint.position, 2f);
            Gizmos.DrawLine(bossSpawnPoint.position, bossSpawnPoint.position + bossSpawnPoint.forward * 3f);
            
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(bossSpawnPoint.position + Vector3.up * 3f, "BOSS SPAWN POINT");
        }
    }
#endif
}

/// <summary>
/// Temporary wrapper to make RoomParentDataScriptableObj compatible with RogueLiteRoomParent.GenerateRandomRooms()
/// This adapter pattern allows custom room layouts to use the base room generation system
/// without modifying RogueLiteRoomParent to depend on specific data types
/// </summary>
internal class RoomDataWrapper : RogueLikeBuildingDataScriptableObj
{
    private RoomParentDataScriptableObj roomData;
    
    public void Initialize(RoomParentDataScriptableObj data)
    {
        roomData = data;
        Debug.Log($"[RoomDataWrapper] Initialized with room data: {(data != null ? data.name : "NULL")}");
        
        // Initialize base class lists to prevent null reference errors
        // Even though we override the methods, initialize these for safety
        buildingRooms = new List<BuildingRooms>();
        friendlyRooms = new List<BuildingRooms>();
        extenderRooms = new List<BuildingRooms>();
        buildingParents = new List<BuildingParents>();
        
        // Note: Room count is determined by spawn points in the room parent prefab,
        // not by configuration values. The system fills all available spawn points.
    }
    
    // Override GetBuildingRoom to use custom room data
    public override GameObject GetBuildingRoom(int difficulty, int currentExtenderCount = 0, bool allowExtenders = true)
    {
        Debug.Log($"[RoomDataWrapper] GetBuildingRoom called - roomData is {(roomData != null ? "valid" : "NULL")}");
        if (roomData == null)
        {
            Debug.LogError("[RoomDataWrapper] roomData is null in GetBuildingRoom!");
            return null;
        }
        GameObject room = roomData.GetRoom(difficulty, currentExtenderCount, allowExtenders);
        Debug.Log($"[RoomDataWrapper] Returning room: {(room != null ? room.name : "NULL")}");
        return room;
    }
    
    // Override GetAllRooms to use custom room data
    public override GameObject[] GetAllRooms(int difficulty, bool includeExtenders = true)
    {
        if (roomData == null) return new GameObject[0];
        return roomData.GetAllRooms(difficulty, includeExtenders);
    }
    
    // Override extender methods
    public override int GetMaxExtendersPerBuilding()
    {
        if (roomData == null) return 0;
        return roomData.GetMaxExtenders();
    }
    
    public override float GetExtenderSpawnChance()
    {
        if (roomData == null) return 0f;
        return roomData.GetExtenderSpawnChance();
    }
}

