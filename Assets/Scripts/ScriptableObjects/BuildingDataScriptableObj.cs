using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for buildings in the roguelite section of the game
/// </summary>

[CreateAssetMenu(fileName = "BuildingDataScriptableObj", menuName = "Scriptable Objects/Roguelite/BuildingDataScriptableObj")]
public class BuildingDataScriptableObj : ScriptableObject
{
    public BuildingType buildingType;
    public List<BuildingParents> buildingParents;
    public List<BuildingRooms> buildingRooms;

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
            Debug.LogError("No suitable rooms found for difficulty: " + difficulty + " for building: " + buildingType + " in " + name);
            return buildingRooms[0].buildingRoom;
        }

        // Randomly select from suitable rooms
        int randomIndex = Random.Range(0, suitableRooms.Count);
        return suitableRooms[randomIndex].buildingRoom;
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
}

