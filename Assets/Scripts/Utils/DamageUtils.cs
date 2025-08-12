using Managers;
using UnityEngine;

/// <summary>
/// Utility class for handling damage-related calculations and animations
/// Reduces code duplication between different character types
/// Uses 2D blend trees for natural and flexible animation blending
/// </summary>
public static class DamageUtils
{
    /// <summary>
    /// Calculates the hit direction as a Vector2 for 2D blend tree animation
    /// Returns a normalized 2D vector where:
    /// X = left/right component (-1 = left, 1 = right)
    /// Y = forward/backward component (-1 = back, 1 = front)
    /// This allows for smooth blending between any hit direction
    /// </summary>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="damageSource">Transform of the damage source, null if unknown</param>
    /// <returns>Vector2 for 2D blend tree control</returns>
    public static Vector2 CalculateHitDirection(Transform characterTransform, Transform damageSource)
    {
        if (damageSource == null)
        {
            // Default to front hit if source is unknown
            return Vector2.up; // (0, 1) = front
        }

        // Calculate direction from damage source to this character
        Vector3 damageDirection = (characterTransform.position - damageSource.position).normalized;
        
        // Get the character's forward and right directions
        Vector3 characterForward = characterTransform.forward;
        Vector3 characterRight = characterTransform.right;
        
        // Calculate dot products for both forward and right components
        float forwardComponent = Vector3.Dot(damageDirection, characterForward);  // Y component
        float rightComponent = Vector3.Dot(damageDirection, characterRight);      // X component
        
        // Return as Vector2 for 2D blend tree
        // X = right/left (-1 to 1), Y = forward/back (-1 to 1)
        return new Vector2(rightComponent, forwardComponent);
    }

    /// <summary>
    /// Triggers the damaged animation with 2D direction parameters for blend tree control
    /// </summary>
    /// <param name="animator">The character's animator component</param>
    /// <param name="hitDirection">2D direction vector from CalculateHitDirection</param>
    public static void TriggerDamagedAnimation(Animator animator, Vector2 hitDirection)
    {
        if (animator == null) return;

        // Set the 2D hit direction parameters for the blend tree
        animator.SetFloat("HitDirectionX", hitDirection.x);  // Left/Right component
        animator.SetFloat("HitDirectionY", hitDirection.y);  // Forward/Back component
        
        // Trigger the damaged animation
        animator.SetTrigger("Damaged");
    }

    /// <summary>
    /// Calculates hit point and normal for VFX effects
    /// </summary>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="damageSource">Transform of the damage source</param>
    /// <param name="hitHeightOffset">Height offset for the hit point (default: 1.5f)</param>
    /// <returns>Tuple containing hit point and hit normal</returns>
    public static (Vector3 hitPoint, Vector3 hitNormal) CalculateHitPointAndNormal(
        Transform characterTransform, 
        Transform damageSource, 
        float hitHeightOffset = 1.5f)
    {
        Vector3 hitPoint = characterTransform.position + Vector3.up * hitHeightOffset;
        
        Vector3 hitNormal = damageSource != null 
            ? (characterTransform.position - damageSource.position).normalized 
            : Vector3.up; // Use upward direction as fallback
            
        return (hitPoint, hitNormal);
    }

    /// <summary>
    /// Applies damage to a character with automatic 2D direction detection
    /// </summary>
    /// <param name="character">The character taking damage</param>
    /// <param name="amount">Amount of damage to take</param>
    /// <param name="damageSource">Transform of the damage source</param>
    /// <param name="animator">The character's animator component</param>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="onDamageTaken">Callback for when damage is taken</param>
    /// <param name="onDeath">Callback for when character dies</param>
    /// <param name="playHitEffect">Whether to play hit VFX (default: true)</param>
    /// <returns>The calculated 2D hit direction vector</returns>
    public static Vector2 ApplyDamage(
        IDamageable character,
        float amount,
        Transform damageSource,
        Animator animator,
        Transform characterTransform,
        System.Action<float, float> onDamageTaken = null,
        System.Action onDeath = null,
        bool playHitEffect = true)
    {
        // Calculate 2D hit direction
        Vector2 hitDirection = CalculateHitDirection(characterTransform, damageSource);
        
        // Trigger animation with 2D direction parameters
        TriggerDamagedAnimation(animator, hitDirection);
        
        // Play hit VFX if requested
        if (playHitEffect && damageSource != null)
        {
            var (hitPoint, hitNormal) = CalculateHitPointAndNormal(characterTransform, damageSource);
            EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, character);
        }
        
        return hitDirection;
    }

    /// <summary>
    /// Applies damage to a character with explicit 2D direction specification
    /// </summary>
    /// <param name="character">The character taking damage</param>
    /// <param name="amount">Amount of damage to take</param>
    /// <param name="hitDirection">Explicit 2D hit direction vector</param>
    /// <param name="damageSource">Transform of the damage source (optional, for VFX)</param>
    /// <param name="animator">The character's animator component</param>
    /// <param name="characterTransform">Transform of the character taking damage</param>
    /// <param name="onDamageTaken">Callback for when damage is taken</param>
    /// <param name="onDeath">Callback for when character dies</param>
    /// <param name="playHitEffect">Whether to play hit VFX (default: true)</param>
    public static void ApplyDamageWithDirection(
        IDamageable character,
        float amount,
        Vector2 hitDirection,
        Transform damageSource,
        Animator animator,
        Transform characterTransform,
        System.Action<float, float> onDamageTaken = null,
        System.Action onDeath = null,
        bool playHitEffect = true)
    {
        // Use the explicitly specified 2D hit direction
        TriggerDamagedAnimation(animator, hitDirection);
        
        // Play hit VFX if requested
        if (playHitEffect && damageSource != null)
        {
            var (hitPoint, hitNormal) = CalculateHitPointAndNormal(characterTransform, damageSource);
            EffectManager.Instance.PlayHitEffect(hitPoint, hitNormal, character);
        }
    }
}
