using UnityEngine;

/// <summary>
/// Interface for objects that can deal damage
/// Provides a common contract for damage dealing across different systems
/// </summary>
public interface IDamageDealer
{
    /// <summary>
    /// The base damage this dealer can deal
    /// </summary>
    float BaseDamage { get; }
    
    /// <summary>
    /// The poise damage this dealer can deal
    /// </summary>
    float PoiseDamage { get; }
    
    /// <summary>
    /// The elemental type of damage this dealer deals
    /// </summary>
    AttackElement ElementType { get; }
    
    /// <summary>
    /// Additional elemental damage bonus
    /// </summary>
    int ElementalDamageBonus { get; }
    
    /// <summary>
    /// The transform that represents the source of this damage
    /// </summary>
    Transform DamageSource { get; }
    
    /// <summary>
    /// Deal damage to a single target
    /// </summary>
    /// <param name="target">The target to damage</param>
    void DealDamage(IDamageable target);
    
    /// <summary>
    /// Deal damage to a single target with custom damage amounts
    /// </summary>
    /// <param name="target">The target to damage</param>
    /// <param name="damageAmount">Custom damage amount</param>
    /// <param name="poiseAmount">Custom poise damage amount</param>
    void DealDamage(IDamageable target, float damageAmount, float poiseAmount);
}

