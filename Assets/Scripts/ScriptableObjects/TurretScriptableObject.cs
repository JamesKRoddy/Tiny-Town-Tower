using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TurretScriptableObject", menuName = "Scriptable Objects/Turret/TurretScriptableObject")]
public class TurretScriptableObject : PlaceableObjectParent
{
    [Header("Turret Information")]
    public string _name; // The name of the turret
    [TextArea(3, 5)] // Enables multi-line text input in the Inspector
    public string _description; // A brief description of the turret
    public Sprite _sprite; // The sprite used for UI representation
    public List<InventoryItem> _resourceCost;
    public TurretCategory turretCategory;
    public GameObject turretPrefab; //Turret that will be spawned in
}

