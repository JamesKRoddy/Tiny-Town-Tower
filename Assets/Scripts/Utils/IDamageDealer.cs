using UnityEngine;

/// <summary>
/// Interface for objects that can deal damage
/// Provides a common contract for damage dealing across different systems
/// 
/// DAMAGE TARGETING RULES (handled automatically by DamageUtils.IsValidTarget):
/// ┌────────────────────────┬─────────────────┬─────────────────────────────────────┐
/// │ Dealer Allegiance      │ Can Damage      │ Examples                            │
/// ├────────────────────────┼─────────────────┼─────────────────────────────────────┤
/// │ HOSTILE (Enemies)      │ FRIENDLY only   │ Zombies hit players, NPCs, turrets  │
/// │                        │                 │ Enemy vomit pools hurt players      │
/// ├────────────────────────┼─────────────────┼─────────────────────────────────────┤
/// │ FRIENDLY (Players)     │ HOSTILE only    │ Swords hit enemies                  │
/// │                        │                 │ Turrets shoot enemies               │
/// │                        │                 │ Player grenades hurt enemies        │
/// ├────────────────────────┼─────────────────┼─────────────────────────────────────┤
/// │ NEUTRAL (Environment)  │ ALL except      │ Lava pits hurt everyone             │
/// │                        │ NEUTRAL         │ Spike traps damage all              │
/// ├────────────────────────┼─────────────────┼─────────────────────────────────────┤
/// │ ANY                    │ NEVER NEUTRAL   │ Quest NPCs always protected         │
/// └────────────────────────┴─────────────────┴─────────────────────────────────────┘
/// 
/// ALLEGIANCE VALUES:
/// - FRIENDLY: Players, NPCs, Turrets, Structures
/// - HOSTILE:  Enemies (Zombies, Bosses)
/// - NEUTRAL:  Protected entities (quest NPCs, invulnerable objects)
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
    /// The allegiance of this damage dealer
    /// Determines who this dealer can damage:
    /// - HOSTILE dealers damage FRIENDLY targets (enemies attack players/NPCs/turrets)
    /// - FRIENDLY dealers damage HOSTILE targets (players/turrets attack enemies)
    /// - NEUTRAL dealers damage ALL targets except NEUTRAL (environmental hazards hurt everyone)
    /// </summary>
    Allegiance DealerAllegiance { get; }
    
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

