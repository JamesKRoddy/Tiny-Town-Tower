using System.Collections.Generic;
using UnityEngine;

public class PlaceableObjectParent : WorldItemBase
{
    public List<ResourceItemCount> _resourceCost;
    public GameObject prefab;
    public Vector2Int size = new Vector2Int(1, 1);
    
    [Header("Upgrade Parameters")]
    public PlaceableObjectParent upgradeTarget;
    public float upgradeTime = 30f;
    public ResourceItemCount[] upgradeResources;
}
