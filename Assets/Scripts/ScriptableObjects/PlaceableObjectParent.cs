using System.Collections.Generic;
using UnityEngine;

public class PlaceableObjectParent : ScriptableObject
{
    public string _name;
    [TextArea(3, 5)] // Enables multi-line text input in the Inspector
    public string _description;
    public Sprite _sprite;
    public List<InventoryItem> _resourceCost;
    public GameObject prefab;
    public Vector2Int size = new Vector2Int(1, 1);
}
