using System.Collections.Generic;
using UnityEngine;

public class PlaceableObjectParent : WorldItemBase
{
    [Header("Placeable Object Parameters")]
    public List<ResourceItemCount> _resourceCost;
    public GameObject prefab;
    public Vector2Int size = new Vector2Int(1, 1);
    public CampPlaceableObjectCategory placeableObjectCategory;

    [Header("Health Parameters")]
    public float maxHealth = 100f;
    public float repairTimeInGameHours = 2f; // Time to repair in game hours (default: 2 game hours)
    public float healthRestoredPerRepair = 50f;
    public ResourceItemCount[] repairResources;

    [Header("Construction Parameters")]
    public float constructionTimeInGameHours = 1f; // Time to construct in game hours (default: 1 game hour)
    
    [Header("Upgrade Parameters")]
    public PlaceableObjectParent upgradeTarget;
    public ResourceItemCount[] upgradeResources;    

    [Header("Destruction Parameters")]
    public float destructionTimeInGameHours = 0.5f; // Time to destroy in game hours (default: 0.5 game hours)
    public ResourceItemCount[] reclaimedResources;
}
