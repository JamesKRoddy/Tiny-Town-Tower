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

    [Header("Construction Parameters")]
    public float constructionTime = 10.0f;

    [Header("Health Parameters")]
    public float maxHealth = 100f;
    public float repairTime = 20f;
    public float healthRestoredPerRepair = 50f;
    public ResourceItemCount[] repairResources;

    [Header("Upgrade Parameters")]
    public BuildingScriptableObj upgradeTarget;
    public float upgradeTime = 30f;
    public ResourceItemCount[] upgradeResources;

    [Header("Destruction Parameters")]
    public GameObject destructionPrefab;
    public float destructionTime = 15f;
    public ResourceItemCount[] reclaimedResources;

    [Header("Work Area")]
    public float taskRadius = 2f; // Radius around the building where NPCs can work

    //TODO put in unlock requirements in here to check if the player is able to build this at their current level or whatever
}
