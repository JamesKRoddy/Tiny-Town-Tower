using System.Collections.Generic;
using UnityEngine;

public class PlaceableObjectParent : WorldItemBase
{
    [Header("Placeable Object Parameters")]
    public List<ResourceItemCount> _resourceCost;
    public GameObject prefab;
    public Vector2Int size = new Vector2Int(1, 1);
    public PlaceableObjectCategory placeableObjectCategory;

    [Header("Health Parameters")]
    public float maxHealth = 100f;
    public float repairTime = 20f;
    public float healthRestoredPerRepair = 50f;
    public ResourceItemCount[] repairResources;

    [Header("Construction Parameters")]
    public float constructionTime = 10.0f;
    
    [Header("Upgrade Parameters")]
    public PlaceableObjectParent upgradeTarget;
    public ResourceItemCount[] upgradeResources;    

    [Header("Destruction Parameters")]
    public float destructionTime = 15f;
    public ResourceItemCount[] reclaimedResources;
}
