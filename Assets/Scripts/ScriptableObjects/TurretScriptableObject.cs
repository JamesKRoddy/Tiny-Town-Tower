using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TurretScriptableObject", menuName = "Scriptable Objects/Turret/TurretScriptableObject")]
public class TurretScriptableObject : PlaceableObjectParent
{
    [Header("Turret Category")]
    public TurretCategory turretCategory;

    [Header("Turret Stats")]
    public float damage = 10f;
    public float range = 10f;
    public float fireRate = 1f;
    public float turretTurnSpeed = 5f;
}

