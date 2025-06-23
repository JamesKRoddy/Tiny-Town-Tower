using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TurretScriptableObject", menuName = "Scriptable Objects/Turret/TurretScriptableObject")]
public class TurretScriptableObject : PlaceableObjectParent
{
    [Header("Turret Category")]
    public TurretCategory turretCategory;

    [Header("Upgrade Parameters")]
    public TurretScriptableObject upgradeTarget;
    public float constructionTime = 3f;
    public ResourceScriptableObj[] upgradeResources;
    public int[] upgradeResourceCosts;
}

