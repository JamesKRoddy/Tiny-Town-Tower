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
        // Selects the most appropriate parent based on difficulty
        BuildingParents selectedParent = buildingParents[0];

        foreach (var parent in buildingParents)
        {
            if (parent.difficulty <= difficulty)
            {
                selectedParent = parent;
            }
        }

        return selectedParent.buildingParent;
    }

    public GameObject GetBuildingRoom(int difficulty)
    {
        // Selects the most appropriate room based on difficulty
        BuildingRooms selectedRoom = buildingRooms[0];

        foreach (var room in buildingRooms)
        {
            if (room.difficulty <= difficulty)
            {
                selectedRoom = room;
            }
        }

        return selectedRoom.buildingRoom;
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
