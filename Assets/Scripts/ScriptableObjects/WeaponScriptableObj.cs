using UnityEngine;

[CreateAssetMenu(fileName = "WeaponScriptableObject", menuName = "Scriptable Objects/Roguelite/WeaponScriptableObject")]
public class WeaponScriptableObj : ResourceScriptableObj
{
    [Header("Weapon Stats")]
    public WeaponAnimationType animationType;
    [Range(0, 100)]
    public int damage = 10;
    [Range(0, 100)]
    public float poiseDamage = 10f; // Poise damage dealt by this weapon
    [Range(0, 3), Tooltip("Attack speed animator multiplier")]
    public float attackSpeed = 1f;
    
    [Header("Elemental Properties")]
    [Tooltip("The elemental type of this weapon. NONE means physical damage only.")]
    public AttackElement weaponElement = AttackElement.NONE;
    
    [Tooltip("Additional elemental damage bonus (added to base damage)")]
    [Range(0, 50)]
    public int elementalDamageBonus = 0;
    
    [Tooltip("Whether this weapon deals pure elemental damage (ignores physical resistances)")]
    public bool isPureElemental = false;
    
    [Tooltip("Status effects this weapon can apply on hit")]
    public StatusEffectType[] possibleStatusEffects = new StatusEffectType[0];
    
    [Tooltip("Chance to apply status effects (0-1)")]
    [Range(0f, 1f)]
    public float statusEffectChance = 0.1f;
    
    [Tooltip("Duration of applied status effects (in seconds)")]
    public float statusEffectDuration = 5f;
}
