using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TurretScriptableObject", menuName = "Scriptable Objects/Turret/TurretScriptableObject")]
public class TurretScriptableObject : ScriptableObject
{
    [Header("Turret Information")]
    public string turretName; // The name of the turret
    [TextArea(3, 5)] // Enables multi-line text input in the Inspector
    public string turretDescription; // A brief description of the turret
    public Sprite turretSprite; // The sprite used for UI representation
    public List<InventoryItem> turretResourceCost;
    public TurretCategory turretCategory;
    public GameObject turretPrefab; //Turret that will be spawned in
}

