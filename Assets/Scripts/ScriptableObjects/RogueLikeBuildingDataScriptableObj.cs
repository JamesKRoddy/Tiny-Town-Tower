using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for buildings in the roguelite section of the game.
/// 
/// BOSS SYSTEM INTEGRATION:
/// This scriptable object now supports boss encounters at the end of buildings.
/// 
/// HOW IT WORKS:
/// 1. Configure boss spawn chance (0-100%) in the inspector
/// 2. Add BossScriptableObj references to the possibleBosses array
/// 3. Each BossScriptableObj contains:
///    - The boss enemy prefab (with EnemyBase component)
///    - The boss room parent prefab (BossRoomParent - a complete building section for the boss fight)
///    - Difficulty requirements and spawn weight
/// 4. During building generation, the system:
///    - Rolls for boss spawn based on bossSpawnChance
///    - Selects a boss using weighted random selection from suitable bosses
///    - Instantiates the boss's BossRoomParent as the final building section
///    - The BossRoomParent generates its own room layout and spawns the boss
/// 
/// BOSS ROOM ARCHITECTURE:
/// - Boss rooms are NOT just single room prefabs - they are complete building parents (BossRoomParent)
/// - Each boss room can have multiple sub-rooms for a complex arena
/// - BossRoomParent inherits from RogueLiteRoomParent and uses the same room generation system
/// - The boss spawns at a designated "BossSpawnPoint" transform within the boss room parent
/// 
/// SETUP STEPS:
/// 1. Create a Building Parent prefab with BossRoomParent component (not just RogueLiteRoomParent)
/// 2. Add "BossSpawnPoint" transform to define where the boss spawns
/// 3. Configure room spawn points as normal for the boss arena
/// 4. Create RoomParentDataScriptableObj (Right-click > Create > Scriptable Objects > Roguelite > RoomParentDataScriptableObj)
///    - Assign the BossRoomParent prefab (which has spawn points configured)
///    - Add room prefabs that can spawn in the arena
///    - (Room count is determined by spawn points in the prefab)
/// 5. Create BossScriptableObj assets (Right-click > Create > Scriptable Objects > Roguelite > Enemies > BossScriptableObj)
///    - Assign boss enemy prefab
///    - Assign RoomParentDataScriptableObj
///    - Configure difficulty, loot table, etc.
/// 6. Add BossScriptableObj references to this building data's possibleBosses array
/// 7. Adjust bossSpawnChance as needed (default: 75%)
/// </summary>

[CreateAssetMenu(fileName = "BuildingDataScriptableObj", menuName = "Scriptable Objects/Roguelite/BuildingDataScriptableObj")]
public class RogueLikeBuildingDataScriptableObj : ScriptableObject
{
    public RogueLikeBuildingType buildingType;
    public GameObject buildingEntrance;
    public List<BuildingParents> buildingParents;
    public List<BuildingRooms> buildingRooms;

    [Header("Friendly Rooms")]
    public List<BuildingRooms> friendlyRooms = new List<BuildingRooms>();
    [SerializeField, Range(0f, 100f)] private float friendlyRoomSpawnChance = 15f; // 15% chance for friendly rooms by default
    
    [Header("Room Extenders")]
    [Tooltip("Room extenders add additional spawn points to expand the building")]
    public List<BuildingRooms> extenderRooms = new List<BuildingRooms>();
    [SerializeField, Range(0f, 100f)] private float extenderSpawnChance = 30f; // 30% chance for room extenders by default
    [SerializeField, Range(0, 5)] private int maxExtendersPerBuilding = 2; // Maximum number of extenders per building
    
    [Header("NPC Configuration")]
    [SerializeField] private NPCScriptableObj[] buildingNPCs;
    [SerializeField] private bool autoSpawnNPCs = true;
    [SerializeField, Range(0f, 100f)] private float npcSpawnChance = 75f; // 75% chance to spawn an NPC per spawn point

    [Header("Boss Configuration")]
    [Tooltip("List of possible bosses that can spawn at the end of this building type. Each boss has its own room and difficulty requirements.")]
    [SerializeField] private BossScriptableObj[] possibleBosses;
    [SerializeField, Range(0f, 100f)] private float bossSpawnChance = 75f; // 75% chance to spawn a boss at the end by default
    [Tooltip("If true, the final room will always be a boss room when a boss spawns. Recommended for proper boss encounter flow.")]
    [SerializeField] private bool guaranteeBossAsEndRoom = true;

    [Header("Room Settings")]
    public int minRoomCount = 3;
    public int maxRoomCount = 5;

    public int GetMaxRoomsForDifficulty(int difficulty)
    {
        // Scale the max rooms based on difficulty, but keep it within min and max bounds
        int scaledMax = Mathf.RoundToInt(maxRoomCount * (1 + (difficulty * 0.1f))); // 10% increase per difficulty level
        return Mathf.Clamp(scaledMax, minRoomCount, maxRoomCount);
    }

    public GameObject GetBuildingParent(int difficulty)
    {
        // Find all suitable parents based on difficulty
        List<BuildingParents> suitableParents = new List<BuildingParents>();
        
        foreach (var parent in buildingParents)
        {
            if (parent.difficulty <= difficulty && !parent.excludeFromTesting)
            {
                suitableParents.Add(parent);
            }
        }

        // If no suitable parents found, return the first parent
        if (suitableParents.Count == 0)
        {
            Debug.LogError("No suitable parents found for difficulty: " + difficulty + " for building: " + buildingType + " in " + name);
            return buildingParents[0].buildingParent;
        }

        // Randomly select from suitable parents
        int randomIndex = Random.Range(0, suitableParents.Count);
        return suitableParents[randomIndex].buildingParent;
    }

    /// <summary>
    /// Get a building room, considering extenders, friendly rooms, and hostile rooms based on spawn chances
    /// This method is used by the room placement system to select appropriate rooms
    /// </summary>
    public virtual GameObject GetBuildingRoom(int difficulty, int currentExtenderCount = 0, bool allowExtenders = true)
    {
        // Check if we should try to spawn an extender first
        if (allowExtenders && extenderRooms.Count > 0 && currentExtenderCount < maxExtendersPerBuilding)
        {
            float extenderRoll = Random.Range(0f, 100f);
            Debug.Log($"[GetBuildingRoom] Extender check - Roll: {extenderRoll:F1}, Chance: {extenderSpawnChance:F1}, Current: {currentExtenderCount}, Max: {maxExtendersPerBuilding}");
            
            if (extenderRoll < extenderSpawnChance)
            {
                GameObject extender = GetExtenderRoom(difficulty);
                if (extender != null)
                {
                    Debug.Log($"[GetBuildingRoom] Selected EXTENDER room: {extender.name}");
                    return extender;
                }
                else
                {
                    Debug.LogWarning($"[GetBuildingRoom] Extender roll passed ({extenderRoll:F1} < {extenderSpawnChance:F1}) but no extender room found for difficulty {difficulty}");
                }
            }
            else
            {
                Debug.Log($"[GetBuildingRoom] Extender roll failed: {extenderRoll:F1} >= {extenderSpawnChance:F1}");
            }
        }
        else
        {
            if (!allowExtenders)
                Debug.Log($"[GetBuildingRoom] Extenders not allowed for this spawn point");
            else if (extenderRooms.Count == 0)
                Debug.LogWarning($"[GetBuildingRoom] No extender rooms configured");
            else if (currentExtenderCount >= maxExtendersPerBuilding)
                Debug.Log($"[GetBuildingRoom] Max extenders reached: {currentExtenderCount}/{maxExtendersPerBuilding}");
        }
        
        // Randomly decide between friendly and hostile rooms based on spawn chance
        float randomValue = Random.Range(0f, 100f);
        if (friendlyRooms.Count > 0 && randomValue < friendlyRoomSpawnChance)
        {
            GameObject friendlyRoom = GetFriendlyRoom(difficulty);
            Debug.Log($"[GetBuildingRoom] Selected FRIENDLY room: {friendlyRoom.name} (roll: {randomValue:F1} < {friendlyRoomSpawnChance:F1})");
            return friendlyRoom;
        }
        else
        {
            GameObject hostileRoom = GetHostileRoom(difficulty);
            Debug.Log($"[GetBuildingRoom] Selected HOSTILE room: {hostileRoom.name} (roll: {randomValue:F1} >= {friendlyRoomSpawnChance:F1})");
            return hostileRoom;
        }
    }

    /// <summary>
    /// Get a hostile room (original behavior)
    /// </summary>
    public GameObject GetHostileRoom(int difficulty)
    {
        // Find all suitable rooms based on difficulty
        List<BuildingRooms> suitableRooms = new List<BuildingRooms>();
        
        foreach (var room in buildingRooms)
        {
            if (room.difficulty <= difficulty && !room.excludeFromTesting)
            {
                suitableRooms.Add(room);
            }
        }

        // If no suitable rooms found, return the first room
        if (suitableRooms.Count == 0)
        {
            Debug.LogError("No suitable hostile rooms found for difficulty: " + difficulty + " for building: " + buildingType + " in " + name);
            return buildingRooms[0].buildingRoom;
        }

        // Randomly select from suitable rooms
        int randomIndex = Random.Range(0, suitableRooms.Count);
        return suitableRooms[randomIndex].buildingRoom;
    }

    /// <summary>
    /// Get a friendly room
    /// </summary>
    public GameObject GetFriendlyRoom(int difficulty)
    {
        // Find all suitable friendly rooms based on difficulty
        List<BuildingRooms> suitableFriendlyRooms = new List<BuildingRooms>();
        
        foreach (var room in friendlyRooms)
        {
            if (room.difficulty <= difficulty && !room.excludeFromTesting)
            {
                suitableFriendlyRooms.Add(room);
            }
        }

        // If no suitable friendly rooms found, fallback to hostile rooms
        if (suitableFriendlyRooms.Count == 0)
        {
            Debug.LogWarning("No suitable friendly rooms found for difficulty: " + difficulty + " for building: " + buildingType + " in " + name + ". Falling back to hostile room.");
            return GetHostileRoom(difficulty);
        }

        // Randomly select from suitable friendly rooms
        int randomIndex = Random.Range(0, suitableFriendlyRooms.Count);
        return suitableFriendlyRooms[randomIndex].buildingRoom;
    }

    /// <summary>
    /// Get a room extender
    /// </summary>
    public virtual GameObject GetExtenderRoom(int difficulty)
    {
        // Find all suitable extender rooms based on difficulty
        List<BuildingRooms> suitableExtenderRooms = new List<BuildingRooms>();
        
        foreach (var room in extenderRooms)
        {
            if (room.difficulty <= difficulty && !room.excludeFromTesting)
            {
                suitableExtenderRooms.Add(room);
            }
        }

        // If no suitable extender rooms found, return null
        if (suitableExtenderRooms.Count == 0)
        {
            return null;
        }

        // Randomly select from suitable extender rooms
        int randomIndex = Random.Range(0, suitableExtenderRooms.Count);
        return suitableExtenderRooms[randomIndex].buildingRoom;
    }



    /// <summary>
    /// Set the friendly room spawn chance (0-100%)
    /// </summary>
    public void SetFriendlyRoomSpawnChance(float chance)
    {
        friendlyRoomSpawnChance = Mathf.Clamp(chance, 0f, 100f);
    }

    /// <summary>
    /// Get the current friendly room spawn chance
    /// </summary>
    public float GetFriendlyRoomSpawnChance()
    {
        return friendlyRoomSpawnChance;
    }

    /// <summary>
    /// Get the settler NPCs available for this building type
    /// </summary>
    public NPCScriptableObj[] GetBuildingNPCs()
    {
        return buildingNPCs ?? new NPCScriptableObj[0];
    }

    /// <summary>
    /// Get whether NPCs should auto-spawn in friendly rooms
    /// </summary>
    public bool GetAutoSpawnNPCs()
    {
        return autoSpawnNPCs;
    }

    /// <summary>
    /// Get the NPC spawn chance (0-100%)
    /// </summary>
    public float GetNPCSpawnChance()
    {
        return npcSpawnChance;
    }

    /// <summary>
    /// Set the NPC spawn chance (0-100%)
    /// </summary>
    public void SetNPCSpawnChance(float chance)
    {
        npcSpawnChance = Mathf.Clamp(chance, 0f, 100f);
    }

    /// <summary>
    /// Get all available rooms for this building at the given difficulty
    /// </summary>
    /// <param name="difficulty">Current difficulty level</param>
    /// <param name="includeExtenders">Whether to include room extenders in the list</param>
    public virtual GameObject[] GetAllRooms(int difficulty, bool includeExtenders = true)
    {
        List<GameObject> allRooms = new List<GameObject>();
        
        // Add hostile rooms
        foreach (var room in buildingRooms)
        {
            if (room.difficulty <= difficulty && !room.excludeFromTesting)
            {
                allRooms.Add(room.buildingRoom);
            }
        }

        // Add friendly rooms
        foreach (var room in friendlyRooms)
        {
            if (room.difficulty <= difficulty && !room.excludeFromTesting)
            {
                allRooms.Add(room.buildingRoom);
            }
        }

        // Add extender rooms if requested
        if (includeExtenders)
        {
            foreach (var room in extenderRooms)
            {
                if (room.difficulty <= difficulty && !room.excludeFromTesting)
                {
                    allRooms.Add(room.buildingRoom);
                }
            }
        }

        int hostileCount = 0;
        foreach (var room in buildingRooms)
            if (room.difficulty <= difficulty && !room.excludeFromTesting) hostileCount++;
        
        int friendlyCount = 0;
        foreach (var room in friendlyRooms)
            if (room.difficulty <= difficulty && !room.excludeFromTesting) friendlyCount++;
        
        int extenderCount = 0;
        if (includeExtenders)
        {
            foreach (var room in extenderRooms)
                if (room.difficulty <= difficulty && !room.excludeFromTesting) extenderCount++;
        }
        
        Debug.Log($"[GetAllRooms] Found {allRooms.Count} rooms for difficulty {difficulty} (includeExtenders: {includeExtenders}) - " +
                 $"Hostile: {hostileCount}, Friendly: {friendlyCount}, Extenders: {extenderCount}");

        return allRooms.ToArray();
    }

    /// <summary>
    /// Get the maximum number of extenders allowed per building
    /// </summary>
    public virtual int GetMaxExtendersPerBuilding()
    {
        return maxExtendersPerBuilding;
    }

    /// <summary>
    /// Get the extender spawn chance (0-100%)
    /// </summary>
    public virtual float GetExtenderSpawnChance()
    {
        return extenderSpawnChance;
    }

    /// <summary>
    /// Set the extender spawn chance (0-100%)
    /// </summary>
    public void SetExtenderSpawnChance(float chance)
    {
        extenderSpawnChance = Mathf.Clamp(chance, 0f, 100f);
    }

    /// <summary>
    /// Check if a boss should spawn based on spawn chance
    /// </summary>
    public bool ShouldSpawnBoss()
    {
        if (possibleBosses == null || possibleBosses.Length == 0)
        {
            return false;
        }

        float roll = Random.Range(0f, 100f);
        bool shouldSpawn = roll < bossSpawnChance;
        Debug.Log($"[ShouldSpawnBoss] Roll: {roll:F1}, Chance: {bossSpawnChance:F1}, Result: {shouldSpawn}");
        return shouldSpawn;
    }

    /// <summary>
    /// Get a boss for the current difficulty level using weighted random selection
    /// Returns null if no suitable boss is found
    /// </summary>
    public BossScriptableObj GetBossForDifficulty(int difficulty)
    {
        if (possibleBosses == null || possibleBosses.Length == 0)
        {
            Debug.LogWarning($"[GetBossForDifficulty] No bosses configured for building: {buildingType} in {name}");
            return null;
        }

        // Find all suitable bosses based on difficulty
        List<BossScriptableObj> suitableBosses = new List<BossScriptableObj>();
        List<int> weights = new List<int>();
        
        foreach (var boss in possibleBosses)
        {
            if (boss != null && boss.CanSpawnAtDifficulty(difficulty))
            {
                suitableBosses.Add(boss);
                weights.Add(boss.GetSpawnWeight());
            }
        }

        // If no suitable bosses found, return null
        if (suitableBosses.Count == 0)
        {
            Debug.LogWarning($"[GetBossForDifficulty] No suitable bosses found for difficulty: {difficulty} for building: {buildingType} in {name}");
            return null;
        }

        // Select boss using weighted random selection
        int totalWeight = 0;
        foreach (int weight in weights)
        {
            totalWeight += weight;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < suitableBosses.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue < currentWeight)
            {
                Debug.Log($"[GetBossForDifficulty] Selected boss: {suitableBosses[i].bossName} for difficulty {difficulty}");
                return suitableBosses[i];
            }
        }

        // Fallback to first suitable boss (should never reach here)
        Debug.Log($"[GetBossForDifficulty] Fallback to first boss: {suitableBosses[0].bossName}");
        return suitableBosses[0];
    }

    /// <summary>
    /// Get the boss room data for a specific boss
    /// Returns the RoomParentDataScriptableObj which contains the room parent prefab and room configuration
    /// </summary>
    public RoomParentDataScriptableObj GetBossRoomData(BossScriptableObj boss)
    {
        if (boss == null)
        {
            Debug.LogError($"[GetBossRoomData] Boss is null!");
            return null;
        }

        if (boss.bossRoomData == null)
        {
            Debug.LogError($"[GetBossRoomData] Boss room data is null for boss: {boss.bossName}");
            return null;
        }
        
        if (!boss.bossRoomData.IsValid())
        {
            Debug.LogError($"[GetBossRoomData] Boss room data is invalid for boss: {boss.bossName}");
            return null;
        }

        Debug.Log($"[GetBossRoomData] Retrieved boss room data for: {boss.bossName}");
        return boss.bossRoomData;
    }

    /// <summary>
    /// Get all possible bosses for this building type
    /// </summary>
    public BossScriptableObj[] GetPossibleBosses()
    {
        return possibleBosses ?? new BossScriptableObj[0];
    }

    /// <summary>
    /// Get the boss spawn chance (0-100%)
    /// </summary>
    public float GetBossSpawnChance()
    {
        return bossSpawnChance;
    }

    /// <summary>
    /// Set the boss spawn chance (0-100%)
    /// </summary>
    public void SetBossSpawnChance(float chance)
    {
        bossSpawnChance = Mathf.Clamp(chance, 0f, 100f);
    }

    /// <summary>
    /// Check if boss should always be the final room when spawned
    /// </summary>
    public bool ShouldGuaranteeBossAsEndRoom()
    {
        return guaranteeBossAsEndRoom;
    }
}

[System.Serializable]
public struct BuildingParents
{
    public GameObject buildingParent;
    public int difficulty;
    public bool excludeFromTesting;
}

[System.Serializable]
public struct BuildingRooms
{
    public GameObject buildingRoom;
    public int difficulty;
    public bool excludeFromTesting;
}

