using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// General-purpose scriptable object for configuring room parent layouts.
/// This is a streamlined version of RogueLikeBuildingDataScriptableObj,
/// containing only room configuration data without building-specific information.
/// 
/// USE CASES:
/// - Boss arenas with custom room layouts
/// - Special event rooms (treasure vaults, challenges, etc.)
/// - Any room parent that needs specific room configurations
/// - Reusable room layouts across different contexts
/// 
/// STRUCTURE:
/// - Room parent prefab (the RogueLiteRoomParent or derived class GameObject)
/// - Room prefabs that can spawn in this layout
/// - Optional extender rooms
/// - No building-specific data (no building type, entrance, NPCs, friendly rooms, etc.)
/// 
/// NOTE: Room count is determined by the number of spawn points in the room parent prefab,
/// not by configuration values. The system fills all available spawn points.
/// 
/// BENEFITS:
/// - Modular and reusable room configurations
/// - Boss rooms, challenge rooms, treasure vaults can all use this
/// - Separate room layout from building/boss data
/// - Designer-friendly configuration
/// </summary>
[CreateAssetMenu(fileName = "RoomParentDataScriptableObj", menuName = "Scriptable Objects/Roguelite/RoomParentDataScriptableObj")]
public class RoomParentDataScriptableObj : ScriptableObject
{
    [Header("Room Parent Prefab")]
    [Tooltip("The room parent prefab (RogueLiteRoomParent or derived class like BossRoomParent)")]
    public GameObject roomParentPrefab;
    
    [Header("Room Configuration")]
    [Tooltip("Room prefabs that can spawn in this layout. The system will fill all spawn points in the room parent prefab.")]
    public List<RoomData> rooms = new List<RoomData>();
    
    [Header("Optional: Room Extenders")]
    [Tooltip("Room extenders that add additional spawn points (optional)")]
    public List<RoomData> extenderRooms = new List<RoomData>();
    
    [SerializeField, Range(0f, 100f)] private float extenderSpawnChance = 20f;
    [SerializeField, Range(0, 2)] private int maxExtendersPerLayout = 1;
    
    /// <summary>
    /// Get a random room for this layout
    /// </summary>
    public GameObject GetRoom(int difficulty, int currentExtenderCount = 0, bool allowExtenders = true)
    {
        // Check if we should spawn an extender
        if (allowExtenders && extenderRooms.Count > 0 && currentExtenderCount < maxExtendersPerLayout)
        {
            float extenderRoll = Random.Range(0f, 100f);
            if (extenderRoll < extenderSpawnChance)
            {
                GameObject extender = GetExtenderRoom(difficulty);
                if (extender != null)
                {
                    return extender;
                }
            }
        }
        
        // Get a regular room
        List<RoomData> suitableRooms = new List<RoomData>();
        
        foreach (var room in rooms)
        {
            if (room.difficulty <= difficulty && !room.excludeFromTesting)
            {
                suitableRooms.Add(room);
            }
        }
        
        if (suitableRooms.Count == 0)
        {
            Debug.LogError($"[RoomParentData] No suitable rooms found for difficulty {difficulty} in {name}");
            return rooms.Count > 0 ? rooms[0].roomPrefab : null;
        }
        
        int randomIndex = Random.Range(0, suitableRooms.Count);
        return suitableRooms[randomIndex].roomPrefab;
    }
    
    /// <summary>
    /// Get an extender room
    /// </summary>
    private GameObject GetExtenderRoom(int difficulty)
    {
        List<RoomData> suitableExtenders = new List<RoomData>();
        
        foreach (var room in extenderRooms)
        {
            if (room.difficulty <= difficulty && !room.excludeFromTesting)
            {
                suitableExtenders.Add(room);
            }
        }
        
        if (suitableExtenders.Count == 0)
        {
            return null;
        }
        
        int randomIndex = Random.Range(0, suitableExtenders.Count);
        return suitableExtenders[randomIndex].roomPrefab;
    }
    
    /// <summary>
    /// Get all available rooms at the given difficulty
    /// </summary>
    public GameObject[] GetAllRooms(int difficulty, bool includeExtenders = true)
    {
        List<GameObject> allRooms = new List<GameObject>();
        
        // Add regular rooms
        foreach (var room in rooms)
        {
            if (room.difficulty <= difficulty && !room.excludeFromTesting)
            {
                allRooms.Add(room.roomPrefab);
            }
        }
        
        // Add extender rooms if requested
        if (includeExtenders)
        {
            foreach (var room in extenderRooms)
            {
                if (room.difficulty <= difficulty && !room.excludeFromTesting)
                {
                    allRooms.Add(room.roomPrefab);
                }
            }
        }
        
        return allRooms.ToArray();
    }
    
    /// <summary>
    /// Get the maximum number of extenders for this layout
    /// </summary>
    public int GetMaxExtenders()
    {
        return maxExtendersPerLayout;
    }
    
    /// <summary>
    /// Get the extender spawn chance
    /// </summary>
    public float GetExtenderSpawnChance()
    {
        return extenderSpawnChance;
    }
    
    /// <summary>
    /// Validate that the room parent data is properly configured
    /// </summary>
    public bool IsValid()
    {
        if (roomParentPrefab == null)
        {
            Debug.LogError($"[RoomParentData] Room parent prefab is null in {name}");
            return false;
        }
        
        if (roomParentPrefab.GetComponent<RogueLiteRoomParent>() == null)
        {
            Debug.LogError($"[RoomParentData] Room parent prefab does not have RogueLiteRoomParent component (or derived class) in {name}");
            return false;
        }
        
        if (rooms.Count == 0)
        {
            Debug.LogError($"[RoomParentData] No rooms configured in {name}");
            return false;
        }
        
        return true;
    }
}

/// <summary>
/// General-purpose room data structure
/// Can be used for any room configuration
/// </summary>
[System.Serializable]
public struct RoomData
{
    [Tooltip("Room prefab")]
    public GameObject roomPrefab;
    
    [Tooltip("Minimum difficulty required to spawn this room")]
    public int difficulty;
    
    [Tooltip("Exclude this room from testing builds")]
    public bool excludeFromTesting;
}

