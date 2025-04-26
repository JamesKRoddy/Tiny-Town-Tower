using System.Collections.Generic;
using UnityEngine;

public class PlaceableObjectParent : WorldItemBase
{
    public List<ResourceItemCount> _resourceCost;
    public GameObject prefab;
    public Vector2Int size = new Vector2Int(1, 1);
}
