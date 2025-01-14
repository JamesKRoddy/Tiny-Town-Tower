using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDataScriptableObj", menuName = "Scriptable Objects/Roguelite/BuildingDataScriptableObj")]
public class BuildingDataScriptableObj : ScriptableObject
{
    public BuildingType buildingType;
    public List<BuildingParents> buildingParents;
    public List<BuildingRooms> buildingRooms;


    public GameObject GetBuildingParent(int difficulty) //TODO implement difficulty stuff here
    {
        return buildingParents[0].buildingParent;
    }

    public GameObject GetBuildingRoom(int difficulty) //TODO implement difficulty stuff here
    {
        return buildingRooms[0].buildingRoom;
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