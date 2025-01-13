using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingScriptableObj", menuName = "Scriptable Objects/BuildingScriptableObj")]
public class BuildingScriptableObj : ScriptableObject
{
    [Header("Building Parameters")]
    public Sprite buildingSprite;
    public string buildingName;
    public string buildingDescription;
    public BuildingCategory buildingCategory;
    public List<InventoryItem> buildingResourceCost;
    public GameObject buildingPrefab;

    [Header("Construction Parameters")]
    public float constructionTime = 10.0f;
    public GameObject constructionSite;
    public Vector2 constructionSize; //TODO have to implement this, itll be used to block off areas of the build grid so that these are in use
    //TODO put in unlock requirements in here to check if the player is able to build this at their current level or whatever
}
