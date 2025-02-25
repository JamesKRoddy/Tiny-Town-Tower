using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingScriptableObj", menuName = "Scriptable Objects/Camp/BuildingScriptableObj")]
public class BuildingScriptableObj : PlaceableObjectParent
{
    public BuildingCategory buildingCategory;

    [Header("Construction Parameters")]
    public float constructionTime = 10.0f;
    public GameObject constructionSite;
    //TODO put in unlock requirements in here to check if the player is able to build this at their current level or whatever
}
