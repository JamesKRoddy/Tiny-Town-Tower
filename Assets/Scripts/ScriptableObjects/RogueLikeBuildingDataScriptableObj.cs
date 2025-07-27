using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for buildings in the roguelite section of the game
/// </summary>

[CreateAssetMenu(fileName = "BuildingDataScriptableObj", menuName = "Scriptable Objects/Roguelite/BuildingDataScriptableObj")]
public class RogueLikeBuildingDataScriptableObj : ScriptableObject
{
    public RogueLikeBuildingType buildingType;
    public GameObject buildingEntrance;
    public List<BuildingParents> buildingParents;
    public List<BuildingRooms> buildingRooms;

    [Header("Friendly Rooms")]
    [SerializeField] private List<BuildingRooms> friendlyRooms = new List<BuildingRooms>();
    [SerializeField, Range(0f, 100f)] private float friendlyRoomSpawnChance = 15f; // 15% chance for friendly rooms by default

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
            if (parent.difficulty <= difficulty)
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

    public GameObject GetBuildingRoom(int difficulty)
    {
        // Randomly decide between friendly and hostile rooms based on spawn chance
        float randomValue = Random.Range(0f, 100f);
        if (friendlyRooms.Count > 0 && randomValue < friendlyRoomSpawnChance)
        {
            return GetFriendlyRoom(difficulty);
        }
        else
        {
            return GetHostileRoom(difficulty);
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
            if (room.difficulty <= difficulty)
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
            if (room.difficulty <= difficulty)
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
    /// Get a room of a specific size that fits the difficulty requirement
    /// </summary>
    public GameObject GetBuildingRoomBySize(int difficulty, RogueLikeRoomSize preferredSize)
    {
        // Randomly decide between friendly and hostile rooms based on spawn chance
        float randomValue = Random.Range(0f, 100f);
        
        if (friendlyRooms.Count > 0 && randomValue < friendlyRoomSpawnChance)
        {
            return GetFriendlyRoomBySize(difficulty, preferredSize);
        }
        else
        {
            return GetHostileRoomBySize(difficulty, preferredSize);
        }
    }

    /// <summary>
    /// Get a hostile room of a specific size
    /// </summary>
    public GameObject GetHostileRoomBySize(int difficulty, RogueLikeRoomSize preferredSize)
    {
        // Find all suitable rooms based on difficulty and size
        List<BuildingRooms> suitableRooms = new List<BuildingRooms>();
        
        foreach (var room in buildingRooms)
        {
            if (room.difficulty <= difficulty && room.roomSize == preferredSize)
            {
                suitableRooms.Add(room);
            }
        }

        // If no rooms of preferred size found, return null to try different size
        if (suitableRooms.Count == 0)
        {
            return null;
        }

        // Randomly select from suitable rooms
        int randomIndex = Random.Range(0, suitableRooms.Count);
        return suitableRooms[randomIndex].buildingRoom;
    }

    /// <summary>
    /// Get a friendly room of a specific size
    /// </summary>
    public GameObject GetFriendlyRoomBySize(int difficulty, RogueLikeRoomSize preferredSize)
    {
        // Find all suitable friendly rooms based on difficulty and size
        List<BuildingRooms> suitableFriendlyRooms = new List<BuildingRooms>();
        
        foreach (var room in friendlyRooms)
        {
            if (room.difficulty <= difficulty && room.roomSize == preferredSize)
            {
                suitableFriendlyRooms.Add(room);
            }
        }

        // If no friendly rooms of preferred size found, fallback to hostile rooms
        if (suitableFriendlyRooms.Count == 0)
        {
            return GetHostileRoomBySize(difficulty, preferredSize);
        }

        // Randomly select from suitable friendly rooms
        int randomIndex = Random.Range(0, suitableFriendlyRooms.Count);
        GameObject selectedRoom = suitableFriendlyRooms[randomIndex].buildingRoom;
        return selectedRoom;
    }

    /// <summary>
    /// Get the best fitting room for a spawn point, trying sizes in order of preference
    /// </summary>
    public GameObject GetBestFittingRoom(int difficulty, RogueLikeRoomSize[] sizePreferences)
    {
        // Try each size preference in order
        foreach (var size in sizePreferences)
        {
            var room = GetBuildingRoomBySize(difficulty, size);
            if (room != null)
            {
                return room;
            }
        }

        // Fallback to any suitable room
        return GetBuildingRoom(difficulty);
    }

    /// <summary>
    /// Get all available room sizes for this building at the given difficulty
    /// </summary>
    public RogueLikeRoomSize[] GetAvailableRoomSizes(int difficulty)
    {
        List<RogueLikeRoomSize> availableSizes = new List<RogueLikeRoomSize>();
        
        // Check hostile rooms
        foreach (var room in buildingRooms)
        {
            if (room.difficulty <= difficulty && !availableSizes.Contains(room.roomSize))
            {
                availableSizes.Add(room.roomSize);
            }
        }

        // Check friendly rooms
        foreach (var room in friendlyRooms)
        {
            if (room.difficulty <= difficulty && !availableSizes.Contains(room.roomSize))
            {
                availableSizes.Add(room.roomSize);
            }
        }

        return availableSizes.ToArray();
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
}

[System.Serializable]
public struct BuildingParents
{
    public GameObject buildingParent;
    public int difficulty;
}

[System.Serializable]
public struct BuildingRooms
{
    public GameObject buildingRoom;
    public int difficulty;
    public RogueLikeRoomSize roomSize;
}

