using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Interface that allows AttackBase to work with any character type.
/// Implemented by both EnemyBase and HumanCharacterController (via CharacterBase).
/// 
/// This enables the same attack components to be used by:
/// - Enemies (zombies, drones, bosses)
/// - NPCs (settlers with weapons)
/// - Player-controlled characters
/// 
/// USAGE:
/// Instead of attacks requiring a specific EnemyBase reference,
/// they can work with any IAttackOwner, making attacks reusable across all character types.
/// </summary>
public interface IAttackOwner
{
    /// <summary>
    /// The transform of the attack owner (for positioning, rotation, etc.)
    /// </summary>
    Transform transform { get; }
    
    /// <summary>
    /// The GameObject of the attack owner
    /// </summary>
    GameObject gameObject { get; }
    
    /// <summary>
    /// The animator component for triggering attack animations
    /// </summary>
    Animator OwnerAnimator { get; }
    
    /// <summary>
    /// The current target for attacks (can be null if no target)
    /// </summary>
    Transform AttackTarget { get; }
    
    /// <summary>
    /// The NavMeshAgent for movement (can be null for player-controlled)
    /// </summary>
    NavMeshAgent OwnerNavMeshAgent { get; }
    
    /// <summary>
    /// Rotation speed for turning towards targets
    /// </summary>
    float OwnerRotationSpeed { get; }
    
    /// <summary>
    /// Current health of the owner
    /// </summary>
    float Health { get; }
    
    /// <summary>
    /// Whether the owner is currently performing an attack
    /// </summary>
    bool IsAttacking { get; set; }
    
    /// <summary>
    /// Whether this character uses root motion for movement
    /// </summary>
    bool UseRootMotion { get; }
    
    /// <summary>
    /// The allegiance of this owner (FRIENDLY, HOSTILE, NEUTRAL)
    /// Used for damage targeting - attacks only hit opposing allegiances
    /// </summary>
    Allegiance GetAllegiance();
    
    /// <summary>
    /// Check if the owner has line of sight to a target position
    /// </summary>
    /// <param name="targetPosition">The position to check visibility to</param>
    /// <returns>True if there's clear line of sight</returns>
    bool HasLineOfSight(Vector3 targetPosition);
    
    /// <summary>
    /// Called when an attack warning phase begins (for visual/audio feedback)
    /// </summary>
    void AttackWarning();
    
    /// <summary>
    /// Called when the actual attack damage frame occurs
    /// This is the standardized animation event method for all characters
    /// </summary>
    void Attack();
    
    /// <summary>
    /// Called when the attack animation ends
    /// This is the standardized animation event method for all characters
    /// </summary>
    void AttackEnd();
}

/// <summary>
/// Extended interface for owners that support advanced combat features.
/// Optional - not all attack owners need to implement this.
/// </summary>
public interface IAdvancedAttackOwner : IAttackOwner
{
    /// <summary>
    /// The character type for VFX/audio lookups
    /// </summary>
    CharacterType CharacterType { get; }
    
    /// <summary>
    /// Whether to show debug collision information
    /// </summary>
    bool ShowCollisionDebug { get; }
    
    /// <summary>
    /// Request attack permission from coordination systems (like EnemyManager)
    /// Returns true if attack is allowed, false if should wait
    /// </summary>
    bool RequestAttackPermission();
    
    /// <summary>
    /// Notify coordination systems that attack is complete
    /// </summary>
    void NotifyAttackComplete();
}

