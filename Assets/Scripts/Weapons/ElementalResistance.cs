using UnityEngine;

/// <summary>
/// Simple class that pairs a WeaponElement with its DamageResistance
/// </summary>
[System.Serializable]
public class ElementalResistance
{
    [Tooltip("The elemental damage type")]
    public AttackElement damageType;
    
    [Tooltip("The resistance level for this damage type")]
    public DamageResistance resistance = DamageResistance.NORMAL;
}
