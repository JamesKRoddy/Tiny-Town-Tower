using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for buildings in the camp section of the game
/// </summary>

[CreateAssetMenu(fileName = "BuildingScriptableObj", menuName = "Scriptable Objects/Camp/BuildingScriptableObj")]
public class BuildingScriptableObj : PlaceableObjectParent
{
    [Header("Building Category")]
    public BuildingCategory buildingCategory;

    //TODO put in unlock requirements in here to check if the player is able to build this at their current level or whatever
}
